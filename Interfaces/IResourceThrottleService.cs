using Microsoft.AspNetCore.Mvc;

public interface IResourceThrottleService
{
    Task DequeueAndProcess();
    JobInfo? GetJob(Guid jobId);
    Task<ThrottleResult> TryProcessOrQueueAsync(Func<Task<string>> work);

}

public record ThrottleResult(JobStatus Status, Guid JobId);

public enum JobStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}

public class JobInfo
{
    public JobStatus Status { get; set; }
    public required Func<Task<string>> Work { get; set; }
    public string? ResultPath { get; set; }
    public string? ErrorMessage { get; set; }
}