using System.ComponentModel.DataAnnotations;
using Balance.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Balance.Models
{
    public class UserTask
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Title { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int PointsPerClick { get; set; }

        [BindNever]
        public string? UserId { get; set; } = null!;

        [Required]
        public Frequency Frequency { get; set; } // Daily, Weekly, or OneTime

        [Required]
        public int HowManyTimes { get; set; }

        public int CompletedCount { get; set; } = 0;

        // Tracks when the user last clicked +1 or modified the task
        public DateTime LastModified { get; set; } = DateTime.Now;

        // Tracks exact moment of full completion
        public DateTime? CompletedAt { get; set; }

        // Necessary for "OneTime" tasks to know their specific date.
        // For Daily/Weekly, the Controller calculates this automatically (e.g. End of Day).
        [Required]
        public DateTime Deadline { get; set; }

        public bool IsCompleted => CompletedCount >= HowManyTimes;

        public virtual ICollection<TaskTag> Tags { get; set; } = new List<TaskTag>();
    }
}