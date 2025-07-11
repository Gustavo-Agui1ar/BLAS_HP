using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace BLAS_HP.DTO
{
    public class ComputeImageRequest
    {
        [Required(ErrorMessage = "id é nescessario")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "tipo é nescessario")]
        public int Matrix { get; set; }
        
        [Required(ErrorMessage = "sinal é nescessario")]
        public int Signal { get; set; }
        
        [Required(ErrorMessage = "sinal_v é nescessario")]
        public double[]? SignalData { get; set; }

        [Required(ErrorMessage = "algoritmo é nescessario")]
        public int Algorithm { get; set; }
    }
}
