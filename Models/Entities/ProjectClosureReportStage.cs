using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class ProjectClosureReportStage
    {
        public int Id { get; set; }

        [Required]
        public int ProjectClosureReportId { get; set; }

        [ForeignKey("ProjectClosureReportId")]
        [ValidateNever]
        public virtual ProjectClosureReport Report { get; set; } = null!;

        [Required]
        public int ProjectStageId { get; set; }

        [ForeignKey("ProjectStageId")]
        [ValidateNever]
        public virtual ProjectStage Stage { get; set; } = null!;
    }
}