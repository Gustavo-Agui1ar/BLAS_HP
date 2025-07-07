using BLAS_HP.DTO;
using BLAS_HP.Service.Python;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IResourceThrottleService _throttleService;

    public ImageController(IResourceThrottleService throttleService)
    {
        _throttleService = throttleService;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessImage([FromBody] ComputeImageRequest request)
    {
        try
        {
            Func<Task<string>> work = async () => { return await Task.Run(() => PythonService.ResolveImage(request)); };

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
        public async Task<IActionResult> GetJobStatus(Guid jobId)
        {
            var job = _throttleService.GetJob(jobId);

            if (job == null)
            {
                return NotFound(new { message = "Job não encontrado." });
        }

        switch (job.Status) {
            
            case JobStatus.Processing:
                return Ok(new { status = "Processing" });
            
            case JobStatus.Queued:
                return Ok(new { status = "Queued" });
            
            case JobStatus.Completed:
                if (string.IsNullOrEmpty(job.ResultPath) || !System.IO.File.Exists(job.ResultPath))
                    return NotFound(new { message = "Arquivo da imagem não encontrado no servidor." });
               
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(job.ResultPath);
                Response.Headers.Append("X-Execution-Time", $"{job.TimeExecuted.TotalSeconds:F3}");
                return File(imageBytes, "image/png");
            
            case JobStatus.Failed:
                _throttleService.RemoveJob(jobId);
                return StatusCode(500, new
                {
                    status = "Failed",
                    error = job.ErrorMessage ?? "Erro desconhecido.",
                    duration = $"{job.TimeExecuted.TotalSeconds:F2} segundos"
                });
            
            default:
                return BadRequest(new { message = "Status de job desconhecido." });
        }

    }
}