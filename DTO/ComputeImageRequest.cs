using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace BLAS_HP.DTO
{
    public class ComputeImageRequest
    {
        [Required(ErrorMessage = "id é nescessario")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "tipo é nescessario")]
        public int TypeMatrix { get; set; }
        
        [Required(ErrorMessage = "sinal é nescessario")]
        public int TypeSignal { get; set; }
        
        [Required(ErrorMessage = "sinal_v é nescessario")]
        public double[]? SignalV { get; set; }

        [Required(ErrorMessage = "algoritmo é nescessario")]
        public int Algorithm { get; set; }
    }
}
