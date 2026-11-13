using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ComplaintRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public ComplaintType Type { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
        public DateTime RequestedAt { get; set; }

        public string? ResolvedById { get; set; }
        [ForeignKey("ResolvedById")]
        public ApplicationUser? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }

        public string? PdfFilePath { get; set; }
    }
}