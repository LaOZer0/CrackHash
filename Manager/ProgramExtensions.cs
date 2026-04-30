using Manager.Options;
using Manager.Services;

namespace Manager;

public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        
        builder.Services.Configure<ManagerOptions>(
            builder.Configuration.GetSection("ManagerOptions"));
        
        builder.Services.AddSingleton<IRequestTracker, RequestTracker>();
        builder.Services.AddSingleton<IStatePersistence, StatePersistenceService>();
        builder.Services.AddSingleton<IWorkerHealthService, WorkerHealthService>();
        builder.Services.AddSingleton<ITaskDistributor, TaskDistributor>();
        builder.Services.AddSingleton<IWorkerClient, WorkerClient>();
        
        builder.Services.AddSingleton<TaskQueueService>();
        builder.Services.AddSingleton<ITaskQueueService>(sp => sp.GetRequiredService<TaskQueueService>());
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TaskQueueService>());
        
        return builder;
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
        
        _ = RestoreStateAsync(app.Services); // Fire-and-forget восстановление
        
        return app;
    }

    private static async Task RestoreStateAsync(IServiceProvider services)
    {
        var persistence = services.GetRequiredService<IStatePersistence>();
        var tracker = services.GetRequiredService<IRequestTracker>();
        var queue = services.GetRequiredService<ITaskQueueService>();
        
        var saved = await persistence.LoadStateAsync();
        foreach (var state in saved)
        {
            if (state.Status == Models.TaskStatus.InProgress) state.Status = Models.TaskStatus.InProgress;
            tracker.Add(state);
            if (state.Status == Models.TaskStatus.InProgress) await queue.EnqueueAsync(state);
        }
    }
}