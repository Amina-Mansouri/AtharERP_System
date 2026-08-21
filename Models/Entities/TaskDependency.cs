using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class TaskDependency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        [ValidateNever]
        public virtual ProjectTask Task { get; set; } = null!;

        [Required]
        public int DependsOnTaskId { get; set; }

        [ForeignKey("DependsOnTaskId")]
        [ValidateNever]
        public virtual ProjectTask DependsOnTask { get; set; } = null!;

        [Display(Name = "نوع التبعية")]
        public DependencyType Type { get; set; } = DependencyType.FinishToStart;
    }
}