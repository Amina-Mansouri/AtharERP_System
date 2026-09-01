using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum SupplierSpecialty
    {
        [Display(Name = "حديد ومواد بناء")]
        SteelAndBuildingMaterials = 1,

        [Display(Name = "أسمنت وخرسانة")]
        CementAndConcrete = 2,

        [Display(Name = "كهربائيات")]
        Electrical = 3,

        [Display(Name = "صحية وسباكة")]
        PlumbingAndSanitary = 4,

        [Display(Name = "تكييف وتهوية")]
        HVAC = 5,

        [Display(Name = "أخشاب ونجارة")]
        WoodAndCarpentry = 6,

        [Display(Name = "أرضيات وتشطيبات")]
        FlooringAndFinishing = 7,

        [Display(Name = "زجاج وألمنيوم")]
        GlassAndAluminum = 8
    }
}