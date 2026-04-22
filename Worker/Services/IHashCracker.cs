using Worker.Models;

namespace Worker.Services;

public interface IHashCracker
{
    Task<List<string>> CrackAsync(WorkerTask task);
}