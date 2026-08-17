using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class EmployeePosition
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [Display(Name = "القسم")]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;

        [Display(Name = "الرتبة الوظيفية")]
        public JobRank Rank { get; set; }

        [Display(Name = "المسار الوظيفي")]
        public CareerTrack Track { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البداية")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ النهاية")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "المنصب الأساسي")]
        public bool IsPrimary { get; set; } = true;
    }
}