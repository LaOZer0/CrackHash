using System.Threading.Channels;
using Manager.Models;
using TaskStatus = Manager.Models.TaskStatus;

namespace Manager.Services;

public class TaskQueueService : IHostedService, ITaskQueueService, IDisposable
{
    private readonly Channel<RequestState> _queue = Channel.CreateUnbounded<RequestState>();
    private readonly IWorkerHealthService _healthService;
    private readonly ITaskDistributor _distributor;
    private readonly IRequestTracker _tracker;
    private readonly IStatePersistence _persistence;
    private readonly ILogger<TaskQueueService> _logger;
    
    private CancellationTokenSource _stoppingCts = new();
    private Task _executingTask;

    public TaskQueueService(
        IWorkerHealthService healthService,
        ITaskDistributor distributor,
        IRequestTracker tracker,
        IStatePersistence persistence,
        ILogger<TaskQueueService> logger)
    {
        _healthService = healthService;
        _distributor = distributor;
        _tracker = tracker;
        _persistence = persistence;
        _logger = logger;
    }

    // Метод интерфейса ITaskQueueService
    public async Task EnqueueAsync(RequestState state)
    {
        await _queue.Writer.WriteAsync(state);
        _logger.LogInformation("Task {RequestId} added to queue.", state.RequestId);
    }

    // Методы интерфейса IHostedService
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TaskQueueService is starting.");
        // Связываем токен остановки с токеном приложения
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Запускаем фоновый цикл
        _executingTask = ExecuteAsync(_stoppingCts.Token);
        
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TaskQueueService is stopping.");
        
        if (_executingTask == null) return;

        try
        {
            // Сигнализируем о необходимости остановки
            _stoppingCts.Cancel();
        }
        finally
        {
            // Ждем завершения текущей задачи
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    // Основной цикл обработки
    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("Processing task {Id}...", task.RequestId);

                // 1. Обновляем статус
                _tracker.Update(task.RequestId, s => 
                {
                    s.Status = TaskStatus.InProgress;
                    s.StartedAt = DateTime.UtcNow;
                });
                await _persistence.SaveStateAsync(_tracker.GetAllStates());

                // 2. Проверяем воркеров
                var aliveWorkers = await _healthService.GetAliveWorkersAsync();
                if (!aliveWorkers.Any())
                    throw new InvalidOperationException("No alive workers available");

                // 3. Распределяем задачи
                var alphabet = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
                _tracker.Update(task.RequestId, s => s.AssignedWorkerCount = aliveWorkers.Count);
                
                await _distributor.DistributeTasksAsync(task, aliveWorkers, alphabet);
                
                // 4. Ждем завершения всех частей
                while (_tracker.Get(task.RequestId)?.CompletedParts.Count < aliveWorkers.Count 
                       && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                    // Периодически сохраняем прогресс (опционально, можно реже)
                    // await _persistence.SaveStateAsync(_tracker.GetAllStates()); 
                }

                // 5. Финализация
                _tracker.Update(task.RequestId, s => s.Status = TaskStatus.Ready);
                await _persistence.SaveStateAsync(_tracker.GetAllStates());
                _logger.LogInformation("Task {Id} completed successfully.", task.RequestId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Task processing cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {Id} failed.", task.RequestId);
                _tracker.Update(task.RequestId, s => 
                {
                    s.Status = TaskStatus.Error;
                    s.ErrorMessage = ex.Message;
                });
                await _persistence.SaveStateAsync(_tracker.GetAllStates());
            }
        }
    }

    public void Dispose()
    {
        _stoppingCts?.Dispose();
    }
}