using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class TaskTodo
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual ProjectTask Task { get; set; } = null!;

        [Required(ErrorMessage = "نص البند مطلوب")]
        [StringLength(500)]
        [Display(Name = "البند")]
        public string Item { get; set; } = string.Empty;

        [Display(Name = "مكتمل")]
        public bool IsCompleted { get; set; }

        [Display(Name = "تاريخ الإكمال")]
        public DateTime? CompletedAt { get; set; }
    }
}