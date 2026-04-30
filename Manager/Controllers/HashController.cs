using Manager.Models;
using Manager.Models.Api;
using Manager.Services;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = Manager.Models.TaskStatus;

namespace Manager.Controllers;

[ApiController]
[Route("api/hash")]
public class HashController : ControllerBase
{
    private readonly IRequestTracker _tracker;
    private readonly ITaskQueueService _queueService;
    private readonly ILogger<HashController> _logger;

    public HashController(
        IRequestTracker tracker,
        ITaskQueueService queueService,
        ILogger<HashController> logger)
    {
        _tracker = tracker;
        _queueService = queueService;
        _logger = logger;
    }

    [HttpPost("crack")]
    public async Task<ActionResult<CrackResponse>> CrackHash([FromBody] CrackRequest request)
    {
        var hash = request.Hash.ToLowerInvariant();
        var hashKey = $"{hash}:{request.MaxLength}";
        RequestState existingState;

        if (_tracker.TryGetCachedResult(hash, request.MaxLength, out var cachedResult))
        {
            _logger.LogInformation("Cache hit for hash {HashKey}", hashKey);
            
            existingState = _tracker.GetByHashKey(hashKey)!;
            return Ok(new CrackResponse { RequestId = existingState.RequestId });
        }

        var state = new RequestState
        {
            RequestId = Guid.NewGuid().ToString(),
            Hash = hash,
            MaxLength = request.MaxLength,
            Status = TaskStatus.Queued
        };
        
        _tracker.Add(state);
        await _queueService.EnqueueAsync(state);
        
        _logger.LogInformation(" New crack request: {RequestId} for hash {HashKey}", 
            state.RequestId, hashKey);
        
        return Ok(new CrackResponse { RequestId = state.RequestId });
    }

    [HttpGet("status")]
    public ActionResult<StatusResponse> GetStatus([FromQuery] string requestId)
    {
        var state = _tracker.Get(requestId);
        if (state == null) 
            return NotFound(new StatusResponse { Status = "NOT_FOUND" });

        return Ok(new StatusResponse
        {
            Status = state.Status.ToString().ToUpper(),
            Progress = state.Progress,
            EstimatedTimeRemaining = state.EstimatedTimeRemaining,
            Data = state.Results
        });
    }
}