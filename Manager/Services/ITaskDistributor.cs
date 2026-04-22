using Manager.Models;

namespace Manager.Services;

public interface ITaskDistributor
{
    Task DistributeTasksAsync(RequestState task, List<string> workerUrls, char[] alphabet);
}