using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ProjectTimeline
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [Required(ErrorMessage = "عنوان الحدث مطلوب")]
        [StringLength(255)]
        [Display(Name = "العنوان")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "تاريخ البدء")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "تاريخ الانتهاء")]
        public DateTime EndDate { get; set; }

        [StringLength(20)]
        [Display(Name = "اللون")]
        public string Color { get; set; } = "#3788d8";

        [Display(Name = "النوع")]
        public TimelineType Type { get; set; } = TimelineType.Milestone;
    }
}