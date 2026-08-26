// Models/Entities/ContractorStatus.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ContractorStatus
    {
        [Display(Name = "نشط")] Active = 1,
        [Display(Name = "منتهي")] Completed = 2,
        [Display(Name = "ملغى")] Cancelled = 3
    }
}