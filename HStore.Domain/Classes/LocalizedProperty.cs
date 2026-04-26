using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HStore.Domain.Classes;

public class LocalizedProperty : Dictionary<string, string>
{
    public LocalizedProperty(IDictionary<string, string> dictionary) : base(dictionary)
    {
    }

    public LocalizedProperty() : base()
    {
    }
    public string GetByLocale(string? Locale)
    {
        if (string.IsNullOrWhiteSpace(Locale) || Locale.Length > 3)
            Locale = "ar";

        return this.GetValueOrDefault(Locale, "");
    }

    public override string ToString()
    {
        return $"{{{string.Join(", ", this.Select(x => $"{x.Key}:{x.Value}"))}}}";
    }

    public bool Contains(string search, string? lang = null)
    {
        if (lang != null)
        {
            return TryGetValue(lang, out var v) && v.Contains(search, StringComparison.CurrentCultureIgnoreCase);
        }
        foreach (var key in Keys)
        {
            if (this[key].Contains(search, StringComparison.CurrentCultureIgnoreCase)) return true;
        }
        return false;
    }

    public List<Dictionary<string, string>> GetTranslations()
    {
        var translations = new List<Dictionary<string, string>>();

        foreach (var prop in this)
        {
            translations.Add(new() { { "lang", prop.Key }, { "value", prop.Value } });
        }
        return translations;
    }
}