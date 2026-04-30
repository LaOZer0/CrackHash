using System.Collections.Concurrent;
using Manager.Models;

namespace Manager.Services;

public class RequestTracker : IRequestTracker
{
    private readonly ConcurrentDictionary<string, RequestState> _states = new();
    private readonly ConcurrentDictionary<string, string> _hashKeyToRequestId = new();

    public void Add(RequestState state)
    {
        _states[state.RequestId] = state;
        _hashKeyToRequestId[state.HashKey] = state.RequestId;
    }

    public RequestState? Get(string requestId)
    {
        _states.TryGetValue(requestId, out var state);
        return state;
    }

    public RequestState? GetByHashKey(string hashKey)
    {
        if (_hashKeyToRequestId.TryGetValue(hashKey, out var requestId))
        {
            return Get(requestId);
        }
        return null;
    }

    public bool TryGetCachedResult(string hash, int maxLength, out List<string> result)
    {
        result = new();
        var state = GetByHashKey($"{hash.ToLowerInvariant()}:{maxLength}");
            
        if (state?.HasResult == true)
        {
            result = new List<string>(state.Results); 
            return true;
        }
        return false;
    }

    public IEnumerable<RequestState> GetAllStates() => _states.Values.ToList();

    public void Update(string requestId, Action<RequestState> update)
    {
        if (_states.TryGetValue(requestId, out var state))
        {
            update(state);
        }
    }
    
    public void IncrementFailedParts(string requestId, int count = 1)
    {
        if (_states.TryGetValue(requestId, out var state))
        {
            state.FailedParts += count;
        }
    }
}