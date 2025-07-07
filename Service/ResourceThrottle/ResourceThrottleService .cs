using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

public class ResourceThrottleService : IResourceThrottleService, IDisposable
{
    private readonly ILogger<ResourceThrottleService> _logger;
    private readonly ConcurrentDictionary<Guid, JobInfo> _jobStore = new();
    private readonly ConcurrentQueue<Guid> _workQueue = new();
    private readonly SemaphoreSlim _semaphore;

    public readonly int _maxConcurrency;
    private readonly double _cpuThreshold;
    private readonly double _memoryThreshold;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;

    public ResourceThrottleService(ILogger<ResourceThrottleService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _maxConcurrency = configuration.GetValue<int>("Processing:MaxConcurrency");
        _cpuThreshold = configuration.GetValue<double>("Processing:CpuThreshold");
        _memoryThreshold = configuration.GetValue<double>("Processing:MemoryThreshold");

        _semaphore = new SemaphoreSlim(_maxConcurrency);

        if (OperatingSystem.IsWindows())
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
        }
    }

    public async Task<ThrottleResult> TryProcessOrQueueAsync(Func<Task<string>> work)
    {
        var jobId = Guid.NewGuid();

        if (IsSystemHealthy() && await _semaphore.WaitAsync(0))
        {
            _logger.LogInformation("Recursos disponíveis. Processando job {JobId} imediatamente.", jobId);
            var jobInfo = new JobInfo { Status = JobStatus.Processing, Work = work };
            _jobStore.TryAdd(jobId, jobInfo);
            _ = ProcessWorkAsync(jobId);

            return new ThrottleResult(JobStatus.Processing, jobId);
        }
        else
        {
            _logger.LogInformation("Recursos ocupados. Colocando job {JobId} na fila.", jobId);
            var jobInfo = new JobInfo { Status = JobStatus.Queued, Work = work };
            _jobStore.TryAdd(jobId, jobInfo);
            _workQueue.Enqueue(jobId);

            return new ThrottleResult(JobStatus.Queued, jobId);
        }
    }

    public async Task DequeueAndProcess()
    {
        if (_workQueue.TryPeek(out var jobId))
        {
            if (!IsSystemHealthy())
            {
                _logger.LogInformation("Worker pausado devido à alta carga do sistema.");
                await Task.Delay(2000); 
                return;
            }

            await _semaphore.WaitAsync();

            if (_workQueue.TryDequeue(out _))
            {
                _logger.LogInformation("Worker pegou o job {JobId} da fila para processamento.", jobId);
                await ProcessWorkAsync(jobId);
            }
            else
            {
                _semaphore.Release();
            }
        }
    }

    private async Task ProcessWorkAsync(Guid jobId)
    {
        if (!_jobStore.TryGetValue(jobId, out var jobInfo))
        {
            _logger.LogWarning("Job {JobId} não encontrado para processamento.", jobId);
            _semaphore.Release();
            return;
        }

        try
        {
            jobInfo.Status = JobStatus.Processing;
            jobInfo.StartedAt = DateTime.Now;

            jobInfo.ResultPath = await jobInfo.Work();
            
            jobInfo.Status = JobStatus.Completed;
            jobInfo.CompletedAt = DateTime.Now;
            jobInfo.TimeExecuted = (jobInfo.CompletedAt - jobInfo.StartedAt);
            _logger.LogInformation("Job {JobId} concluído com sucesso.", jobId);
        }
        catch (Exception ex)
        {
            jobInfo.Status = JobStatus.Failed;
            jobInfo.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Job {JobId} falhou.", jobId);
        }
        finally
        {
            _jobStore.TryUpdate(jobId, jobInfo, jobInfo);
            _semaphore.Release();
            _logger.LogInformation("Slot de processamento liberado pelo job {JobId}.", jobId);
        }
    }

    public JobInfo? GetJob(Guid jobId)
    {
        _jobStore.TryGetValue(jobId, out var jobInfo);
        return jobInfo;
    }

    public void RemoveJob (Guid jobId)
    {
        _jobStore.Remove(jobId, out var _);
    }

    private bool IsSystemHealthy()
    {
        if (!OperatingSystem.IsWindows()) return true;

        _cpuCounter?.NextValue();
        Thread.Sleep(100);

        var currentCpuUsage = _cpuCounter?.NextValue() ?? 0.0;
        var currentMemoryUsage = _memoryCounter?.NextValue() ?? 0.0;

        _logger.LogInformation($"Uso atual - CPU: {currentCpuUsage:F2}%, Memória: {currentMemoryUsage:F2}%");

        return currentCpuUsage < _cpuThreshold && currentMemoryUsage < _memoryThreshold;
    }

    public void Dispose() {
        _workQueue.Clear();
        _jobStore.Clear();
        _semaphore.Dispose();
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        _logger.LogInformation("ResourceThrottleService disposed and resources cleared.");
    }
}