using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public LeaveType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [NotMapped]
        public int DaysCount => (EndDate.Date - StartDate.Date).Days + 1;

        public string? Reason { get; set; }

        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
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