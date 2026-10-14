using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public enum CorrespondencePriority
    {
        [Display(Name = "عادية")]
        Normal = 1,

        [Display(Name = "عالية")]
        High = 2,

        [Display(Name = "عاجلة")]
        Urgent = 3
    }

    public enum CorrespondenceStatus
    {
        [Display(Name = "قيد الانتظار")]
        Pending = 1,

        [Display(Name = "تم الرد")]
        Resolved = 2
    }

    // مراسلة رسمية واحدة بين الشركة والمقاول (طلب أو رد عليه) — كل مراسلة تُولَّد لها PDF بنموذج موحّد،
    // وتترابط عبر ParentCorrespondenceId لتكوين محادثة مزدوجة الاتجاه (نفس آلية مراجعة التكليفات)
    public class SiteCorrespondence
    {
        public int Id { get; set; }

        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        [ValidateNever]
        public virtual Site Site { get; set; } = null!;

        [Display(Name = "رقم الطلب")]
        public int RequestNumber { get; set; }

        [Display(Name = "الأولوية")]
        public CorrespondencePriority Priority { get; set; } = CorrespondencePriority.Normal;

        [Display(Name = "تاريخ الرد المطلوب")]
        public DateTime? RequiredResponseDate { get; set; }

        [Required(ErrorMessage = "تفاصيل الاستفسار مطلوبة")]
        [Display(Name = "تفاصيل الاستفسار")]
        public string DetailedQuery { get; set; } = string.Empty;

        [Display(Name = "من المقاول")]
        public bool IsFromContractor { get; set; }

        [Display(Name = "اسم مقدم الطلب")]
        public string SubmittedByName { get; set; } = string.Empty;

        [Display(Name = "صفة مقدم الطلب")]
        public string? SubmittedByPosition { get; set; }

        [Display(Name = "توقيع مقدم الطلب")]
        public string? SubmittedBySignaturePath { get; set; }

        // تُملأ فقط عندما تكون المراسلة من طرف الموظفين — تمثّل اعتماد المدير المباشر
        [Display(Name = "اسم المعتمِد")]
        public string? ApproverName { get; set; }

        [Display(Name = "صفة المعتمِد")]
        public string? ApproverPosition { get; set; }

        [Display(Name = "توقيع المعتمِد")]
        public string? ApproverSignaturePath { get; set; }

        [ValidateNever]
        public string? SentById { get; set; }

        [ForeignKey("SentById")]
        [ValidateNever]
        public virtual ApplicationUser? SentBy { get; set; }

        public int? SentByContractorId { get; set; }

        [ForeignKey("SentByContractorId")]
        [ValidateNever]
        public virtual Contractor? SentByContractor { get; set; }

        [Display(Name = "الحالة")]
        public CorrespondenceStatus Status { get; set; } = CorrespondenceStatus.Pending;

        [Display(Name = "مقروء")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "رد على")]
        public int? ParentCorrespondenceId { get; set; }

        [ForeignKey("ParentCorrespondenceId")]
        [ValidateNever]
        public virtual SiteCorrespondence? ParentCorrespondence { get; set; }

        [Display(Name = "مسار ملف PDF")]
        public string? PdfFilePath { get; set; }

        [Display(Name = "تاريخ الإرسال")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SiteCorrespondenceAttachment> Attachments { get; set; } = new List<SiteCorrespondenceAttachment>();
    }
}