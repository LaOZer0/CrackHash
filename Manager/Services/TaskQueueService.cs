using System.Collections.Concurrent;
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
    
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _taskCompletions = new();
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
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        _executingTask = ExecuteAsync(_stoppingCts.Token);
        
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TaskQueueService is stopping.");
        
        if (_executingTask == null) return;

        try
        {
            _stoppingCts.Cancel();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Processing task {Id}...", task.RequestId);
                
                _tracker.Update(task.RequestId, s => { s.Status = TaskStatus.InProgress; s.StartedAt = DateTime.UtcNow; });
                _ = _persistence.SaveStateAsync(_tracker.GetAllStates());

                var aliveWorkers = await _healthService.GetAliveWorkersAsync();
                if (!aliveWorkers.Any()) throw new InvalidOperationException("No alive workers available");

                var alphabet = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
                _tracker.Update(task.RequestId, s => s.AssignedWorkerCount = aliveWorkers.Count);
                _ = _persistence.SaveStateAsync(_tracker.GetAllStates());
                
                await _distributor.DistributeTasksAsync(task, aliveWorkers, alphabet);
                
                var tcs = new TaskCompletionSource();
                _taskCompletions[task.RequestId] = tcs;
                
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(10));
                
                try
                {
                    await tcs.Task.WaitAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    _tracker.Update(task.RequestId, s =>
                    {
                        var missing = s.AssignedWorkerCount - s.CompletedParts.Count - s.FailedParts;
                        if (missing > 0) s.FailedParts += missing;
                        
                        if (s.Results.Count > 0) s.Status = TaskStatus.PartialReady;
                        else s.Status = TaskStatus.Error;
                    });
                    _logger.LogWarning("Task {Id} timed out. Marked missing parts as failed.", task.RequestId);
                }
                
                _ = _persistence.SaveStateAsync(_tracker.GetAllStates());
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {Id} failed.", task.RequestId);
                _tracker.Update(task.RequestId, s => { s.Status = TaskStatus.Error; s.ErrorMessage = ex.Message; });
                _ = _persistence.SaveStateAsync(_tracker.GetAllStates());
            }
            finally { _taskCompletions.TryRemove(task.RequestId, out _); }
        }
    }
    
    public void MarkTaskCompleted(string requestId)
    {
        if (_taskCompletions.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(); 
        }
    }

    public void Dispose()
    {
        _stoppingCts?.Dispose();
    }
}