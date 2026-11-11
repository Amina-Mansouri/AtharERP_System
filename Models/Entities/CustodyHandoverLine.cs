using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class CustodyHandoverLine
    {
        public int Id { get; set; }

        public int CustodyHandoverId { get; set; }
        [ForeignKey("CustodyHandoverId")]
        public CustodyHandover? Handover { get; set; }

        public int CustodyItemId { get; set; }
        [ForeignKey("CustodyItemId")]
        public CustodyItem? Item { get; set; }

        public int QuantityHanded { get; set; }
        public int QuantityReturned { get; set; } = 0;

        public CustodyLineStatus Status { get; set; } = CustodyLineStatus.WithEmployee;
    }
}