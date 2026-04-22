using Manager.Models;

namespace Manager.Services;

public interface IStatePersistence
{
    Task SaveStateAsync(IEnumerable<RequestState> states);
    Task<List<RequestState>> LoadStateAsync();
}