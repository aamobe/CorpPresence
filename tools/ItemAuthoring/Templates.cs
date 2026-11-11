using System.Text.RegularExpressions;

namespace CorpPresence.ItemAuthoring;

internal static class Templates
{
    // Embedded developer templates only. Specs cannot supply code, templates or file paths here.
    internal static string Render(string name,IReadOnlyDictionary<string,string> values)
    {
        using var stream=typeof(Templates).Assembly.GetManifestResourceStream("ItemAuthoring."+name+".txt")
            ?? throw new AuthoringException("TEMPLATE_MISSING",name);
        using var reader=new StreamReader(stream);
        return Regex.Replace(reader.ReadToEnd(),@"\{\{([A-Za-z]+)\}\}",match=>
            values.TryGetValue(match.Groups[1].Value,out string? value)?value:
            throw new AuthoringException("TEMPLATE_FIELD_MISSING",match.Groups[1].Value));
    }
}
