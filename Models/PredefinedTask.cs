using System.ComponentModel.DataAnnotations;
using Balance.Models.Enums;

namespace Balance.Models
{
    public class PredefinedTask
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Title { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Points")]
        public int PointsPerClick { get; set; }

        [Required]
        public Frequency Frequency { get; set; }

        [Required]
        [Display(Name = "Repetitions")]
        public int HowManyTimes { get; set; } = 1;
    }
}