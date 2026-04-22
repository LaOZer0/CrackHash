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

    public CallbackController(
        IRequestTracker tracker,
        IStatePersistence persistence,
        ILogger<CallbackController> logger)
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
        {
            response = serializer.Deserialize(stringReader) as CrackHashWorkerResponse;
        }
        
        if (response == null)
            return BadRequest("Invalid XML");
        
        _logger.LogInformation("Received response for request {RequestId}, part {PartNumber}, {Count} answers",
            response.RequestId, response.PartNumber, response.Answers.Words.Count);
        
        _tracker.Update(response.RequestId, state =>
        {
            lock (state.Results)
            {
                state.Results.AddRange(response.Answers.Words);
                state.CompletedParts.Add(response.PartNumber);
                
                if (state.CompletedParts.Count == state.AssignedWorkerCount)
                {
                    state.Status = TaskStatus.Ready;
                }
            }
        });
        
        await _persistence.SaveStateAsync(_tracker.GetAllStates());
        
        return Ok();
    }
}