
using System.ComponentModel.DataAnnotations.Schema;

namespace HStore.Domain.Classes;

[ComplexType]
public class LocalizedProperty(string? ar = null, string? en = null)
{
    public string? Ar { get; set; } = ar;
    public string? En { get; set; } = en;
     
    public LocalizedProperty() : this(null, null) { }
    public string this[string i]
    {
        get
        {
            var res = i.ToLower() switch
            {
                "ar" => Ar,
                _ => En
            };
            return res ?? Ar ?? En ?? "";
        }
        set
        {
            switch (i.ToLower())
            {
                case "ar":
                    Ar = value;
                    break;
                case "en":
                    En = value;
                    break;
                default:
                    throw new ArgumentException("Invalid key");
            }
        }
    }

    public string GetByLocale(string locale)
    {
        return this[locale];
    }

}