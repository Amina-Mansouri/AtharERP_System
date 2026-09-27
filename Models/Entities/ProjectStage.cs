using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AtharERP_System.Models.Entities
{
    public class ProjectStage
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        [ValidateNever]
        public virtual Project Project { get; set; } = null!;

        [Required(ErrorMessage = "اسم المرحلة مطلوب")]
        [StringLength(255)]
        [Display(Name = "اسم المرحلة")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "الترتيب")]
        public int Sequence { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "الوزن (% من المشروع)")]
        public decimal Weight { get; set; }

        [Display(Name = "التخصص")]
        public DocumentClassification? Discipline { get; set; }

        // وزن مستقل تماماً عن Weight أعلاه — يُدخَل يدوياً لكل مرحلة، يختلف حسب نوع المشروع، يُستخدم لاحقاً في حساب KPI
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "وزن التخصص (KPI)")]
        public decimal? KpiWeight { get; set; }

        [Display(Name = "الحالة")]
        public StageStatus Status { get; set; } = StageStatus.New;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "التكلفة الفعلية")]
        public decimal ActualCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "قيمة البيع")]
        public decimal SaleValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المساحة (م²)")]
        public decimal? Area { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "النسبة اليدوية الأولى")]
        public decimal? SaleMarkupPercent1 { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "النسبة اليدوية الثانية")]
        public decimal? SaleMarkupPercent2 { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الإنجاز")]
        public decimal CompletionPercentage { get; set; }

        [Display(Name = "المهندس المسؤول")]
        public string? AssignedEngineerId { get; set; }

        [ForeignKey("AssignedEngineerId")]
        public virtual ApplicationUser? AssignedEngineer { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ البدء المخطط")]
        public DateTime? PlannedStartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الانتهاء المخطط")]
        public DateTime? PlannedEndDate { get; set; }

       

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التسليم الفعلي")]
        public DateTime? ActualDeliveryDate { get; set; }

        [Display(Name = "توثيق العمل الدوري")]
        public string? WorkDocumentation { get; set; }

      
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public virtual ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();
    }
}