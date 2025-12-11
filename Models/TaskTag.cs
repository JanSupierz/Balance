using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Balance.Models
{
    public class TaskTag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        public string Color { get; set; } = "#6c757d";

        [Required]
        public string UserId { get; set; } = null!;

        [JsonIgnore]
        public virtual ICollection<UserTask> Tasks { get; set; } = new List<UserTask>();
    }
}