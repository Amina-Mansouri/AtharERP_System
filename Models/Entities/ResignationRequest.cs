using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ResignationRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime RequestedLastWorkingDay { get; set; }
        public string? Reason { get; set; }

        public ResignationStatus Status { get; set; } = ResignationStatus.Pending;
        public DateTime RequestedAt { get; set; }

        public string? DecidedById { get; set; }
        [ForeignKey("DecidedById")]
        public ApplicationUser? DecidedBy { get; set; }
        public DateTime? DecidedAt { get; set; }

        public string? GmDecidedById { get; set; }
        [ForeignKey("GmDecidedById")]
        public ApplicationUser? GmDecidedBy { get; set; }
        public DateTime? GmDecidedAt { get; set; }

        public string? PdfFilePath { get; set; }
    }
}