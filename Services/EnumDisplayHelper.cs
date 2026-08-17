using System.ComponentModel.DataAnnotations;

namespace AtharERP_System.Services
{
    public static class EnumDisplayHelper
    {
        public static List<(string Value, string Label)> GetDisplayList<TEnum>() where TEnum : struct, Enum
        {
            var result = new List<(string, string)>();
            foreach (var value in Enum.GetValues<TEnum>())
            {
                var member = typeof(TEnum).GetMember(value.ToString())[0];
                var display = member.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
                result.Add((value.ToString(), display?.Name ?? value.ToString()));
            }
            return result;
        }
    }
}