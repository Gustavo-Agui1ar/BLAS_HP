using BLAS_HP.DTO;
using BLAS_HP.Service.Python;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IResourceThrottleService _throttleService;
    private readonly IServiceProvider _serviceProvider;

    public ImageController(IResourceThrottleService throttleService, IServiceProvider serviceProvider)
    {
        _throttleService = throttleService;
        _serviceProvider = serviceProvider;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessImage([FromBody] ComputeImageRequest request)
    {
        try
        {
            Func<Task<string>> work = async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                return await Task.Run(() => PythonService.ResolveImage(request));
            };

            var result = await _throttleService.TryProcessOrQueueAsync(work);

            if (result.Status == JobStatus.Processing)
            {
                return Accepted(new { message = "Sua requisição foi recebida e está sendo processada.", status = "Processing", result.JobId });
            }
            else
            {
                return Accepted(new
                {
                    message = "Sua requisição foi recebida e colocada na fila de processamento.",
                    jobId = result.JobId,
                    status = "Queued"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Ocorreu um erro inesperado ao submeter sua requisição.", error = ex.Message });
        }
    }

    [HttpGet("status/{jobId}")]
    public IActionResult GetJobStatus(Guid jobId)
    {
        
        var jobStatus = _throttleService.GetJob(jobId);

        if (jobStatus == null)
        {
            return NotFound(new { message = "Job não encontrado." });
        }

        if(jobStatus.Status == JobStatus.Completed)
        {

        }

        return Ok(jobStatus); // Retorna um objeto como { status: "Completed", imageUrl: "/images/resultado_xyz.png" }
    }
}