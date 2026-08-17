using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [Display(Name = "الاسم الكامل")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "الرقم الوظيفي")]
        [StringLength(50)]
        public string? JobNumber { get; set; }

        [Display(Name = "الرقم الشخصي")]
        [StringLength(50)]
        public string? PersonalId { get; set; }

        [Display(Name = "الصورة الشخصية")]
        public string? ProfilePhotoPath { get; set; }

        [Display(Name = "مسار المستندات")]
        public string? DocumentsPath { get; set; }

        [Display(Name = "القسم")]
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [Display(Name = "المسؤوليات")]
        public string? Responsibilities { get; set; }

        [Display(Name = "الرتبة الوظيفية")]
        public JobRank Rank { get; set; } = JobRank.E0_TraineeEngineer;

        [Display(Name = "المسار الوظيفي")]
        public CareerTrack CareerTrack { get; set; } = CareerTrack.Engineering;

        [Display(Name = "التعهد")]
        public string? Pledge { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الراتب التعاقدي")]
        public decimal ContractSalary { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ بداية العقد")]
        public DateTime? ContractStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ نهاية العقد")]
        public DateTime? ContractEndDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التقييم الشهري")]
        public DateTime? MonthlyEvaluationDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التقييم السنوي")]
        public DateTime? YearlyEvaluationDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ إنهاء العقد")]
        public DateTime? ContractTerminationDate { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "آخر دخول")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "رقم الهاتف")]
        [Phone]
        public override string? PhoneNumber { get; set; }

        // ========== الموقع الجغرافي للحضور ==========

        [Display(Name = "الموقع المتوقع للحضور")]
        [StringLength(250)]
        public string? ExpectedLocationName { get; set; }

        [Display(Name = "خط العرض")]
        public double? ExpectedLatitude { get; set; }

        [Display(Name = "خط الطول")]
        public double? ExpectedLongitude { get; set; }

        [Display(Name = "نصف القطر المسموح (متر)")]
        public int AllowedRadiusMeters { get; set; } = 100;

        // ========== المناصب المتعددة ==========
        public virtual ICollection<EmployeePosition> EmployeePositions { get; set; } = new List<EmployeePosition>();

        // ========== الصلاحيات الإضافية الممنوحة يدوياً ==========
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}