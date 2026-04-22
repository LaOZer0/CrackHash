using Manager.Services;
using TaskStatus = Manager.Models.TaskStatus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IRequestTracker, RequestTracker>();
builder.Services.AddSingleton<IStatePersistence, StatePersistenceService>();
builder.Services.AddSingleton<IWorkerHealthService, WorkerHealthService>();
builder.Services.AddSingleton<ITaskDistributor, TaskDistributor>();
builder.Services.AddSingleton<IWorkerClient, WorkerClient>();

builder.Services.AddSingleton<TaskQueueService>();

builder.Services.AddSingleton<ITaskQueueService>(sp => sp.GetRequiredService<TaskQueueService>());

builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TaskQueueService>());

var app = builder.Build();

// Восстановление состояния (State Persistence)
var persistence = app.Services.GetRequiredService<IStatePersistence>();
var tracker = app.Services.GetRequiredService<IRequestTracker>();
var queue = app.Services.GetRequiredService<TaskQueueService>();

// Восстанавливаем задачи при старте
var savedStates = await persistence.LoadStateAsync();
foreach (var state in savedStates)
{
    // Если задача была в процессе, сбрасываем в очередь (т.к. сервер перезагружался)
    if (state.Status == TaskStatus.InProgress) 
        state.Status = TaskStatus.Queued;
    
    tracker.Add(state);
    
    // Если задача в очереди, добавляем в канал обработки
    if (state.Status == TaskStatus.Queued)
        await queue.EnqueueAsync(state);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

Console.WriteLine("Manager started...");
app.Run();