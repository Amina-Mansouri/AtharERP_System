using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum ClosureFinalStatus
    {
        [Display(Name = "مغلق بالكامل")]
        Closed = 1,

        [Display(Name = "مغلق مع ملاحظات")]
        ClosedWithNotes = 2,

        [Display(Name = "بانتظار اعتماد نهائي")]
        PendingFinalApproval = 3,

        [Display(Name = "بانتظار اعتماد العميل")]
        PendingClientApproval = 4,

        [Display(Name = "موقوف مؤقتًا")]
        TemporarilyHalted = 5
    }
}