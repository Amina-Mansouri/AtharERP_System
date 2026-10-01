using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class ExpenseCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم التصنيف مطلوب")]
        [StringLength(100)]
        [Display(Name = "اسم التصنيف")]
        public string NameAr { get; set; } = string.Empty;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "نوع التصنيف")]
        public ExpenseCategoryScope Scope { get; set; } = ExpenseCategoryScope.Project;

        public virtual ICollection<ProjectExpense> Expenses { get; set; } = new List<ProjectExpense>();
    }
}