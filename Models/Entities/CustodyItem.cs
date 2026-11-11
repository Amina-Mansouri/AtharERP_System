using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // كتالوج مواد العهد — تُضاف من الإدارة بالكمية الإجمالية، وتُخصم الكمية المتاحة عند كل تسليم وتُرد عند الاستلام
    public class CustodyItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الرمز مطلوب")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المادة مطلوب")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public CustodyCategory? Category { get; set; }

        public int TotalQuantity { get; set; }
        public int AvailableQuantity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}