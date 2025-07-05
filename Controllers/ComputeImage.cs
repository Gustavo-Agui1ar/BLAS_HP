
using BLAS_HP.DTO;
using Microsoft.AspNetCore.Mvc;
using BLAS_HP.Service.Python;

[ApiController]
[Route("api/[controller]")]
public class ImageController : Controller
{
    [HttpPost]
    public IActionResult Compute([FromBody] ComputeImageRequest request)
    {
        try
        {
            return PythonService.ResolveImage(request);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "An error occurred while processing your request.", error = ex.Message });
        }
    }
}