using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class PermitAttachment
    {
        public int Id { get; set; }

        public int PermitRequestId { get; set; }
        [ForeignKey("PermitRequestId")]
        public PermitRequest? PermitRequest { get; set; }

        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}