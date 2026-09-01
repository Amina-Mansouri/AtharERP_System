using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Models.Entities
{
    public enum MeasurementUnit
    {
        [Display(Name = "طن")]
        Ton = 1,

        [Display(Name = "كيلوغرام")]
        Kilogram = 2,

        [Display(Name = "متر مربع")]
        SquareMeter = 3,

        [Display(Name = "متر مكعب")]
        CubicMeter = 4,

        [Display(Name = "متر طولي")]
        LinearMeter = 5,

        [Display(Name = "لتر")]
        Liter = 6,

        [Display(Name = "وحدة")]
        Unit = 7,

        [Display(Name = "كيس")]
        Bag = 8,

        [Display(Name = "لفة")]
        Roll = 9
    }
}