using System.ComponentModel.DataAnnotations.Schema;

namespace AtharERP_System.Models.Entities
{
    public class ResignationTaskClearance
    {
        public int Id { get; set; }

        public int ResignationRequestId { get; set; }
        [ForeignKey("ResignationRequestId")]
        public ResignationRequest? ResignationRequest { get; set; }

        public int ProjectAssignmentId { get; set; }
        [ForeignKey("ProjectAssignmentId")]
        public ProjectAssignment? ProjectAssignment { get; set; }

        public DateTime EndDate { get; set; }
    }
}