// Models/Entities/SafetyResult.cs
using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum SafetyResult
    {
        [Display(Name = "آمن")] Safe = 1,
        [Display(Name = "تحذير")] Warning = 2,
        [Display(Name = "خطير")] Danger = 3
    }
}