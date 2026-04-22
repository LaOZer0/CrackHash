using Manager.Models;

namespace Manager.Services;

public interface ITaskQueueService
{
    Task EnqueueAsync(RequestState state);
}