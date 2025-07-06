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
        _logger.LogInformation("Serviço de processamento em fila foi iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _throttleService.DequeueAndProcess();

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no worker de processamento em fila.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Serviço de processamento em fila foi parado.");
    }
}