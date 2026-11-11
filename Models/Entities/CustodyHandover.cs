using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    // عملية تسليم عهدة لموظف (قد تحمل أكثر من مادة) — توقيع الشؤون الإدارية هو مُجهِّز العملية (CreatedBy)، وتوقيع المستلم يُسجَّل عند تأكيده
    public class CustodyHandover
    {
        public int Id { get; set; }

        public string HolderId { get; set; } = string.Empty;
        [ForeignKey("HolderId")]
        public ApplicationUser? Holder { get; set; }

        public DateTime HandedDate { get; set; } = DateTime.UtcNow;

        public string CreatedById { get; set; } = string.Empty;
        [ForeignKey("CreatedById")]
        public ApplicationUser? CreatedBy { get; set; }

        public bool HolderConfirmed { get; set; } = false;
        public DateTime? HolderConfirmedAt { get; set; }

        public string? PdfFilePath { get; set; }

        public List<CustodyHandoverLine> Lines { get; set; } = new();
    }
}