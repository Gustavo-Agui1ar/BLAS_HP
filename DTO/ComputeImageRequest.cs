using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace BLAS_HP.DTO
{
    public class ComputeImageRequest
    {
        [Required(ErrorMessage = "tipo é nescessario")]
        public int typeMatrix { get; set; }
        [Required(ErrorMessage = "sinal é nescessario")]
        public int typeSignal { get; set; }
        [Required(ErrorMessage = "sinal_v é nescessario")]
        public double[]? signalV { get; set; }
        [Required(ErrorMessage = "algoritmo é nescessario")]
        public int algorithm { get; set; }
    }
}
