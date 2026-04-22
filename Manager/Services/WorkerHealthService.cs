namespace Manager.Services;

public class WorkerHealthService : IWorkerHealthService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WorkerHealthService> _logger;

    public WorkerHealthService(
        IConfiguration config, 
        IHttpClientFactory httpClientFactory,
        ILogger<WorkerHealthService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<string>> GetAliveWorkersAsync()
    {
        var workerUrls = (_config["WorkerUrls"] ?? "http://worker:8080").Split(',');
        var alive = new List<string>();

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(2);

        var tasks = workerUrls.Select(async url =>
        {
            try
            {
                var trimmedUrl = url.TrimEnd('/');
                var response = await client.GetAsync($"{trimmedUrl}/health");
                if (response.IsSuccessStatusCode)
                    return trimmedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Worker {Url} is down: {Message}", url, ex.Message);
            }
            return (string?)null;
        });

        var results = await Task.WhenAll(tasks);
        alive.AddRange(results.Where(u => u != null)!);
            
        _logger.LogInformation("Alive workers: {Count}/{Total}", 
            alive.Count, workerUrls.Length);
            
        return alive;
    }
}