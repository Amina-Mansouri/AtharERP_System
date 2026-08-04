using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [Display(Name = "الاسم الكامل")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "المسمى الوظيفي")]
        [StringLength(100)]
        public string? JobTitle { get; set; }

        [Display(Name = "القسم")]
        [StringLength(100)]
        public string? Department { get; set; }

        [Display(Name = "درجة المهندس")]
        public EngineerRank EngineerRank { get; set; } = EngineerRank.None;

        [Display(Name = "الصورة الشخصية")]
        public string? ProfileImage { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الانضمام")]
        [DataType(DataType.Date)]
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "آخر دخول")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "رقم الهاتف")]
        [Phone]
        public override string? PhoneNumber { get; set; }

        // ========== الموقع الجغرافي للحضور ==========

        [Display(Name = "الموقع المتوقع للحضور")]
        [StringLength(250)]
        public string? ExpectedLocationName { get; set; }  // ← مثال: "مقر الشركة - طرابلس"

        [Display(Name = "خط العرض")]
        public double? ExpectedLatitude { get; set; }

        [Display(Name = "خط الطول")]
        public double? ExpectedLongitude { get; set; }

        [Display(Name = "نصف القطر المسموح (متر)")]
        public double? AllowedRadiusMeters { get; set; } = 100;

     
    }
}