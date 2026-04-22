using Worker.Models;
using Worker.Utils;

namespace Worker.Services;

public class HashCracker : IHashCracker
{
    private readonly ILogger<HashCracker> _logger;

    public HashCracker(ILogger<HashCracker> logger)
    {
        _logger = logger;
    }

    public Task<List<string>> CrackAsync(WorkerTask task)
    {
        var results = new List<string>();
        var alphabet = task.Alphabet;
        var totalCombinations = CombinationIterator.CountTotal(alphabet, task.MaxLength);
            
        var partSize = totalCombinations / task.PartCount;
        var remainder = totalCombinations % task.PartCount;
            
        var startIndex = (task.PartNumber - 1) * partSize + Math.Min(task.PartNumber - 1, remainder);
        var endIndex = startIndex + partSize + (task.PartNumber <= remainder ? 1 : 0) - 1;
            
        _logger.LogInformation("Worker {PartNumber}: processing indices {Start}-{End} of {Total}",
            task.PartNumber, startIndex, endIndex, totalCombinations);
            
        for (long index = startIndex; index <= endIndex; index++)
        {
            var candidate = CombinationIterator.GetByIndex(alphabet, task.MaxLength, index);
            var hash = Md5Helper.ComputeMd5(candidate);
                
            if (hash.Equals(task.Hash, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(candidate);
                _logger.LogInformation("Found match: {Candidate} -> {Hash}", candidate, hash);
            }
        }
            
        return Task.FromResult(results);
    }
}