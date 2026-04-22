using System.Collections.Concurrent;
using Manager.Models;

namespace Manager.Services;

public class RequestTracker : IRequestTracker
{
    private readonly ConcurrentDictionary<string, RequestState> _states = new();

    public void Add(RequestState state)
    {
        _states[state.RequestId] = state;
    }

    public RequestState? Get(string requestId)
    {
        _states.TryGetValue(requestId, out var state);
        return state;
    }

    public IEnumerable<RequestState> GetAllStates()
    {
        return _states.Values.ToList();
    }

    public void Update(string requestId, Action<RequestState> update)
    {
        if (_states.TryGetValue(requestId, out var state))
        {
            update(state);
        }
    }
}