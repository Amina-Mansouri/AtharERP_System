using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

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
        PermissionChanged = 11,

        [Display(Name = "اقتراب انتهاء عقد")]
        ContractExpiring = 12,

        [Display(Name = "انتهاء عقد")]
        ContractExpired = 13,

        [Display(Name = "طلب فني جديد")]
        TechnicalRequestRaised = 14,

        [Display(Name = "طلب صيانة جديد")]
        MaintenanceRequested = 15,

        [Display(Name = "مقترح تصميمي مُسلَّم")]
        ProposalSubmitted = 16,

        [Display(Name = "مطالبة مالية معتمدة")]
        ClaimApproved = 17,

        [Display(Name = "مراسلة موقع جديدة")]
        SiteCorrespondenceReceived = 18,

        [Display(Name = "دفعة مسجَّلة على مطالبة")]
        ClaimPaymentRecorded = 19,

        [Display(Name = "تحصيل مطالبة بالكامل")]
        ClaimFullySettled = 20,

        [Display(Name = "صرف مكافأة مهندسة")]
        EngineerRewardPaid = 21,

        [Display(Name = "إلغاء صرف مكافأة")]
        EngineerRewardCancelled = 22,

        [Display(Name = "حذف إيصال دفعة")]
        ClaimPaymentDeleted = 23,

        [Display(Name = "طلب إجازة جديد")]
        LeaveRequestCreated = 24,

        [Display(Name = "قرار بشأن طلب إجازة")]
        LeaveRequestDecided = 25,

        [Display(Name = "طلب استقالة جديد")]
        ResignationRequestCreated = 26,

        [Display(Name = "قرار بشأن طلب استقالة")]
        ResignationRequestDecided = 27,

        [Display(Name = "تفعيل استقالة")]
        ResignationEffective = 28,

        [Display(Name = "طلب سلفة جديد")]
        SalaryAdvanceRequestCreated = 29,

        [Display(Name = "قرار بشأن طلب سلفة")]
        SalaryAdvanceRequestDecided = 30,

        [Display(Name = "تسليم عهدة جديدة")]
        CustodyHandoverCreated = 31,

        [Display(Name = "تأكيد استلام عهدة")]
        CustodyHandoverConfirmed = 32,

        [Display(Name = "طلب إذن جديد")]
        PermitRequestCreated = 33,

        [Display(Name = "قرار بشأن طلب إذن")]
        PermitRequestDecided = 34,

        [Display(Name = "شكوى جديدة")]
        ComplaintCreated = 35,

        [Display(Name = "رد على شكوى")]
        ComplaintResolved = 36
    }
}