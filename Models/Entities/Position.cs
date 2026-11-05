using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class Position
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "اسم المنصب")]
        public string NameAr { get; set; } = string.Empty;

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "نسبة الزيادة %")]
        public decimal Percentage { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;
    }
}