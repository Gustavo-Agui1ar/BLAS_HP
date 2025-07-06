public class QueuedProcessingService : BackgroundService
{
    private readonly ILogger<QueuedProcessingService> _logger;
    private readonly ResourceThrottleService _throttleService;

    public QueuedProcessingService(ILogger<QueuedProcessingService> logger, IResourceThrottleService throttleService)
    {
        _logger = logger;
        _throttleService = (ResourceThrottleService)throttleService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de processamento em fila foi iniciado (modo paralelo).");

        var tasksList = new List<Task>();
        var maxConcurrency = _throttleService._maxConcurrency;

        while (!stoppingToken.IsCancellationRequested)
        {
            tasksList.RemoveAll(t => t.IsCompleted);

            var availableSlots = maxConcurrency - tasksList.Count;

            if (availableSlots > 0)
            {
                for (int i = 0; i < availableSlots; i++)
                {
                    var consumerTask = _throttleService.DequeueAndProcess();
                    tasksList.Add(consumerTask);
                }
            }

            await Task.Delay(500, stoppingToken);
        }

        await Task.WhenAll(tasksList);
        _logger.LogInformation("Serviço de processamento em fila foi parado.");
    }
}