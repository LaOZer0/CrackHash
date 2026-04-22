using Manager.Models;

namespace Manager.Services;

public class TaskDistributor : ITaskDistributor
{
    private readonly IWorkerClient _workerClient;
    private readonly ILogger<TaskDistributor> _logger;

    public TaskDistributor(
        IWorkerClient workerClient,
        ILogger<TaskDistributor> logger)
    {
        _workerClient = workerClient;
        _logger = logger;
    }

    public async Task DistributeTasksAsync(
        RequestState task, 
        List<string> workerUrls, 
        char[] alphabet)
    {
        var tasks = new List<Task>();
        var workerCount = workerUrls.Count;

        for (int i = 0; i < workerCount; i++)
        {
            var workerUrl = workerUrls[i];
            var partNumber = i + 1;

            var request = new Models.Xml.CrackHashManagerRequest
            {
                RequestId = task.RequestId,
                PartNumber = partNumber,
                PartCount = workerCount,
                Hash = task.Hash,
                MaxLength = task.MaxLength,
                Alphabet = new Models.Xml.Alphabet
                {
                    Symbols = alphabet.Select(c => c.ToString()).ToList()
                }
            };

            tasks.Add(_workerClient.SendTaskAsync(workerUrl, request));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Distributed {Count} tasks for request {RequestId}", 
            workerCount, task.RequestId);
    }
}