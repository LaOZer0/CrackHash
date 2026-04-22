using Manager.Models;

namespace Manager.Services;

public interface IRequestTracker
{
    void Add(RequestState state);
    RequestState? Get(string requestId);
    IEnumerable<RequestState> GetAllStates();
    void Update(string requestId, Action<RequestState> update);
}