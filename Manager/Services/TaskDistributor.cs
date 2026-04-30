using Manager.Models;
using Manager.Models.Xml;

namespace Manager.Services;

public class TaskDistributor : ITaskDistributor
{
    private readonly IWorkerClient _workerClient;
    private readonly IRequestTracker _tracker;
    private readonly ILogger<TaskDistributor> _logger;

    public TaskDistributor(IWorkerClient workerClient, IRequestTracker tracker, ILogger<TaskDistributor> logger)
    {
        _workerClient = workerClient;
        _tracker = tracker;
        _logger = logger;
    }

    public async Task DistributeTasksAsync(RequestState task, List<string> workerUrls, char[] alphabet)
    {
        var distributionTasks = new List<Task>();
        var workerCount = workerUrls.Count;

        for (int i = 0; i < workerCount; i++)
        {
            var workerUrl = workerUrls[i];
            var partNumber = i + 1;

            var request = new CrackHashManagerRequest
            {
                RequestId = task.RequestId,
                PartNumber = partNumber,
                PartCount = workerCount,
                Hash = task.Hash,
                MaxLength = task.MaxLength,
                Alphabet = new Alphabet { Symbols = alphabet.Select(c => c.ToString()).ToList() }
            };

            distributionTasks.Add(SendWithFailureTracking(workerUrl, request, task.RequestId));
        }

        await Task.WhenAll(distributionTasks);
        _logger.LogInformation("Distributed {Count} tasks for request {RequestId}", workerCount, task.RequestId);
    }

    private async Task SendWithFailureTracking(string workerUrl, CrackHashManagerRequest request, string requestId)
    {
        try
        {
            await _workerClient.SendTaskAsync(workerUrl, request);
            _logger.LogDebug("Task sent to {WorkerUrl} for request {RequestId}", workerUrl, requestId);
        }
        catch (Exception ex) when (
            ex is TaskCanceledException or HttpRequestException or IOException)
        {
            // 🔹 Воркер недоступен или таймаут → считаем часть упавшей
            _logger.LogWarning("Could not reach {WorkerUrl}. Marking part {PartNumber} as failed.", 
                workerUrl, request.PartNumber);
        
            _tracker.IncrementFailedParts(requestId);
        }
    }
}