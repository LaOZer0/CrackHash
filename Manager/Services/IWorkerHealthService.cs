namespace Manager.Services;

public interface IWorkerHealthService
{
    Task<List<string>> GetAliveWorkersAsync();
}