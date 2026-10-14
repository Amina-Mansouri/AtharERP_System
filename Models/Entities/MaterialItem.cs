using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class MaterialItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الصنف مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم الصنف")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "التصنيف")]
        public int MaterialCategoryId { get; set; }

        [ForeignKey("MaterialCategoryId")]
        [ValidateNever]
        public virtual MaterialCategory MaterialCategory { get; set; } = null!;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإضافة")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}