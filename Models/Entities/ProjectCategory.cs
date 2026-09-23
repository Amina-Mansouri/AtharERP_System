using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // تصنيف نوع المشروع حسب إدارة الشركة (مثال: سكني/تجاري) مع فئته (VIP/عادي) ووزنها — يُستخدم لاحقاً في معادلة KPI
    public class ProjectCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "التصنيف مطلوب")]
        [StringLength(100)]
        [Display(Name = "التصنيف")]
        public string Classification { get; set; } = string.Empty;

        [Required(ErrorMessage = "الفئة مطلوبة")]
        [StringLength(50)]
        [Display(Name = "الفئة")]
        public string Tier { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "الوزن")]
        public decimal Weight { get; set; }

        [Display(Name = "نشطة")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        [Display(Name = "الاسم الكامل")]
        public string DisplayName => $"{Classification} - {Tier}";

        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}