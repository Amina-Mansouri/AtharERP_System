using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum NotificationEventType
    {
        [Display(Name = "مهمة مُكلَّفة")]
        TaskAssigned = 0,

        [Display(Name = "تغيّر حالة مهمة")]
        TaskStatusChanged = 1,

        [Display(Name = "مرحلة مكتملة")]
        StageCompleted = 2,

        [Display(Name = "مهمة متأخرة")]
        TaskDelayed = 3,

        [Display(Name = "تسليم يقترب")]
        DeliveryApproaching = 4,

        [Display(Name = "تكلفة مكتملة")]
        CostCompleted = 5,

        [Display(Name = "طلب توريد جديد")]
        SupplyRequestCreated = 6,

        [Display(Name = "تقرير يومي مضاف")]
        DailyReportAdded = 7,

        [Display(Name = "فحص جودة غير مطابق")]
        QualityCheckFailed = 8,

        [Display(Name = "فحص سلامة خطير")]
        SafetyCheckDanger = 9,

        [Display(Name = "مرحلة موقع متأخرة")]
        SitePhaseDelayed = 10,

        [Display(Name = "تغيير صلاحية")]
        PermissionChanged = 11
    }
}