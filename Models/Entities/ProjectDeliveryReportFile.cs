using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class ProjectDeliveryReportFile
    {
        public int Id { get; set; }

        [Required]
        public int ProjectDeliveryReportId { get; set; }

        [ForeignKey("ProjectDeliveryReportId")]
        [ValidateNever]
        public virtual ProjectDeliveryReport Report { get; set; } = null!;

        [Required]
        public int DocumentRevisionId { get; set; }

        [ForeignKey("DocumentRevisionId")]
        [ValidateNever]
        public virtual DocumentRevision DocumentRevision { get; set; } = null!;
    }
}