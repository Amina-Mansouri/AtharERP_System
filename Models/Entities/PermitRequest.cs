using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class PermitRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public PermitType Type { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        [NotMapped]
        public decimal DurationHours => (decimal)(EndTime - StartTime).TotalHours;

        [NotMapped]
        public string TimeRangeDisplay => $"{(int)StartTime.TotalHours:00}:{StartTime.Minutes:00} - {(int)EndTime.TotalHours:00}:{EndTime.Minutes:00}";

        public string? Reason { get; set; }

        public PermitStatus Status { get; set; } = PermitStatus.Pending;
        public DateTime RequestedAt { get; set; }

        public string? DecidedById { get; set; }
        [ForeignKey("DecidedById")]
        public ApplicationUser? DecidedBy { get; set; }
        public DateTime? DecidedAt { get; set; }

        public string? GmDecidedById { get; set; }
        [ForeignKey("GmDecidedById")]
        public ApplicationUser? GmDecidedBy { get; set; }
        public DateTime? GmDecidedAt { get; set; }
    }
}