using Microsoft.AspNetCore.Mvc;
using System.Xml.Serialization;
using System.IO;
using Manager.Models.Xml;
using Manager.Services;
using Manager.Models;
using TaskStatus = Manager.Models.TaskStatus;

namespace Manager.Controllers;

[ApiController]
[Route("internal/api/manager/hash/crack")]
public class CallbackController : ControllerBase
{
    private readonly IRequestTracker _tracker;
    private readonly IStatePersistence _persistence;
    private readonly ILogger<CallbackController> _logger;

    public CallbackController(IRequestTracker tracker, IStatePersistence persistence, ILogger<CallbackController> logger)
    {
        _tracker = tracker;
        _persistence = persistence;
        _logger = logger;
    }

    [HttpPatch("request")]
    public async Task<IActionResult> ReceiveWorkerResponse()
    {
        using var reader = new StreamReader(Request.Body);
        var xml = await reader.ReadToEndAsync();
        
        var serializer = new XmlSerializer(typeof(CrackHashWorkerResponse), 
            "http://ccfit.nsu.ru/schema/crack-hash-response");
        
        CrackHashWorkerResponse? response;
        using (var stringReader = new StringReader(xml))
            response = serializer.Deserialize(stringReader) as CrackHashWorkerResponse;
        
        if (response == null) return BadRequest("Invalid XML");
        
        bool isFinished = false;
        
        _tracker.Update(response.RequestId, state =>
        {
            lock (state.Results)
            {
                foreach (var word in response.Answers.Words)
                    if (!state.Results.Contains(word)) state.Results.Add(word);
                
                state.CompletedParts.Add(response.PartNumber);
                
                bool allProcessed = state.CompletedParts.Count + state.FailedParts >= state.AssignedWorkerCount;
                
                if (state.Results.Count > 0)
                {
                    state.Status = allProcessed ? TaskStatus.Ready : TaskStatus.PartialReady;
                }
                else if (allProcessed)
                {
                    state.Status = TaskStatus.Error;
                }
                
                isFinished = allProcessed;
            }
        });

        _ = _persistence.SaveStateAsync(_tracker.GetAllStates());
        
        if (isFinished)
        {
            _logger.LogInformation("Task {RequestId} finished with status {Status}", 
                response.RequestId, _tracker.Get(response.RequestId)?.Status);
            
            if (HttpContext.RequestServices.GetRequiredService<ITaskQueueService>() is TaskQueueService tqs)
                tqs.MarkTaskCompleted(response.RequestId);
        }
        
        return Ok();
    }
}