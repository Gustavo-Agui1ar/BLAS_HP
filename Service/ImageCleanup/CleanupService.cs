public class ImageCleanupService : BackgroundService
{
    private readonly ILogger<ImageCleanupService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    private readonly int _cleanupInterval;
    private readonly int _fileMaxAge;

    public ImageCleanupService(ILogger<ImageCleanupService> logger, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;

        _cleanupInterval = _configuration.GetValue<int>("CleanupService:CleanupIntervalMinutes", 5);
        _fileMaxAge = _configuration.GetValue<int>("CleanupService:FileMaxAgeMinutes", 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de limpeza de imagens iniciado.");
        _logger.LogInformation("Intervalo de verificação: {Intervalo} minutos. Idade máxima dos arquivos: {Idade} minutos.", _cleanupInterval, _fileMaxAge);


        await Task.Delay(TimeSpan.FromMinutes(_cleanupInterval), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Executando limpeza de imagens antigas...");

                var resultsPath = Path.Combine(_environment.ContentRootPath, "Results");

                if (!Directory.Exists(resultsPath))
                {
                    _logger.LogWarning("Diretório de resultados '{Path}' não encontrado. Pulando limpeza.", resultsPath);
                }
                else
                {
                    var filesDeleted = 0;
                    var directoryInfo = new DirectoryInfo(resultsPath);

                    foreach (var file in directoryInfo.GetFiles("*.png"))
                    {
                        // Verifica se o arquivo é mais antigo que o tempo máximo permitido
                        if (file.CreationTimeUtc < DateTime.UtcNow.AddMinutes(-_fileMaxAge))
                        {
                            _logger.LogInformation("Deletando arquivo antigo: {FileName}", file.Name);
                            file.Delete();
                            filesDeleted++;
                        }
                    }

                    if (filesDeleted > 0)
                    {
                        _logger.LogInformation("{Count} imagens antigas foram deletadas.", filesDeleted);
                    }
                    else
                    {
                        _logger.LogInformation("Nenhuma imagem antiga encontrada para deletar.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro durante a execução do serviço de limpeza de imagens.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_cleanupInterval), stoppingToken);
        }

        _logger.LogInformation("Serviço de limpeza de imagens foi parado.");
    }
}