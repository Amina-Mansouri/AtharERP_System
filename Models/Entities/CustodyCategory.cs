using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public class CustodyCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم التصنيف مطلوب")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}