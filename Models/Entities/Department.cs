using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم القسم")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "القسم الرئيسي")]
        public int? ParentDepartmentId { get; set; }

        [ForeignKey("ParentDepartmentId")]
        public virtual Department? ParentDepartment { get; set; }

        public virtual ICollection<Department> ChildDepartments { get; set; } = new List<Department>();

        [StringLength(1000)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<EmployeePosition> EmployeePositions { get; set; } = new List<EmployeePosition>();
    }
}