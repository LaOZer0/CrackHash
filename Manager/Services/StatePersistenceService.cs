using System.Text.Json;
using Manager.Models;
using TaskStatus = Manager.Models.TaskStatus;

namespace Manager.Services;

public class StatePersistenceService : IStatePersistence
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _options = new() 
    { 
        WriteIndented = true, 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    };

    public StatePersistenceService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "data", "state.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public async Task SaveStateAsync(IEnumerable<RequestState> states)
    {
        await _lock.WaitAsync();
        try
        {
            var deduplicated = states
                .GroupBy(s => s.HashKey)
                .Select(group =>
                {
                    var priorityOrder = new[] { TaskStatus.Ready, TaskStatus.Error, TaskStatus.InProgress, TaskStatus.Queued };
                    
                    return group.OrderByDescending(s => Array.IndexOf(priorityOrder, s.Status)).First();
                })
                .ToList();

            var json = JsonSerializer.Serialize(deduplicated, _options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally 
        { 
            _lock.Release(); 
        }
    }

    public async Task<List<RequestState>> LoadStateAsync()
    {
        if (!File.Exists(_filePath)) return new();
        
        await _lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var states = JsonSerializer.Deserialize<List<RequestState>>(json, _options) ?? new();
            
            return states
                .GroupBy(s => s.HashKey)
                .Select(group =>
                {
                    var priorityOrder = new[] { TaskStatus.Ready, TaskStatus.Error, TaskStatus.InProgress, TaskStatus.Queued };
                    return group.OrderByDescending(s => Array.IndexOf(priorityOrder, s.Status)).First();
                })
                .ToList();
        }
        finally 
        { 
            _lock.Release(); 
        }
    }
}