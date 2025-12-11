using System.ComponentModel.DataAnnotations;

namespace Balance.Models
{
    public class Prize
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Title { get; set; } = null!;

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Cost must be at least 1 point.")]
        public int Cost { get; set; }

        [Required]
        public string UserId { get; set; } = null!;
    }
}