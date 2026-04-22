using Manager.Models.Xml;

namespace Manager.Services;

public interface IWorkerClient
{
    Task SendTaskAsync(string workerUrl, CrackHashManagerRequest request);
}