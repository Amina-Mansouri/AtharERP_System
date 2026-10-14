using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class SiteCorrespondenceAttachment
    {
        public int Id { get; set; }

        [Required]
        public int SiteCorrespondenceId { get; set; }

        [ForeignKey("SiteCorrespondenceId")]
        [ValidateNever]
        public virtual SiteCorrespondence SiteCorrespondence { get; set; } = null!;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;
    }
}