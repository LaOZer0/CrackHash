using Manager.Models;

namespace Manager.Services;

public interface IRequestTracker
{
    void Add(RequestState state);
    RequestState? Get(string requestId);
    RequestState? GetByHashKey(string hashKey);
    IEnumerable<RequestState> GetAllStates();
    void Update(string requestId, Action<RequestState> update);
    bool TryGetCachedResult(string hash, int maxLength, out List<string> result);
    void IncrementFailedParts(string requestId, int count = 1); 
}