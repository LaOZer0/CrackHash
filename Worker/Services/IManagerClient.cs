namespace Worker.Services;

public interface IManagerClient
{
    Task SendResultAsync(string managerUrl, string requestId, int partNumber, List<string> answers);
}