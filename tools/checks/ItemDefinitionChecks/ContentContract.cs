internal sealed record ContentRow(string Key, string Source, string[] TextKeys, string[] Resources, string Name = "name", bool IndependentShell = false);
internal sealed record FactoryRow(string Id, string Goods, string Company, string Sprite = null, string LegacyId = null);

internal static class ContentContract
{
    internal static string[] RequiredResources(string mode, string company, string art, string fallback, bool power)
    {
        if (mode == "NativeSprite" || mode == "TintedNativeSprite")
        {
            if (string.IsNullOrWhiteSpace(fallback) || power)
                throw new ArgumentException("Native art needs a named fallback and cannot declare embedded battery states");
            return Array.Empty<string>();
        }
        if (mode != "Embedded" || string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(art))
            throw new ArgumentException("Embedded art needs a company and art name; unknown art modes are rejected");
        var names = new List<string> { CorpPresence.UI.ArtResource.Name(company, art) };
        if (power)
            foreach (var suffix in new[] { "_half", "_empty" }) names.Add(CorpPresence.UI.ArtResource.Name(company, art + suffix));
        return names.ToArray();
    }
    internal static List<string> Validate(IReadOnlyList<ContentRow> definitions, IReadOnlyList<FactoryRow> identities,
        IReadOnlySet<string> texts, IReadOnlySet<string> resources)
    {
        var errors = new List<string>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var factoryKeys = identities.Where(i => i.Goods != null).Select(i => i.Goods).ToHashSet(StringComparer.Ordinal);
        foreach (var row in definitions)
        {
            if (string.IsNullOrWhiteSpace(row.Name)) errors.Add($"missing name: {row.Source}/{row.Key}");
            if (row.TextKeys.Length == 0) errors.Add($"missing text name binding: {row.Source}/{row.Key}");
            if (!keys.Add(row.Key)) errors.Add($"duplicate definition: {row.Source}/{row.Key}");
            if (!factoryKeys.Contains(row.Key)) errors.Add($"missing identity: {row.Source}/{row.Key}");
            if (!identities.Any(i => i.Id == "corp_presence_" + row.Key && i.Goods == row.Key && i.Company == null))
                errors.Add($"unreachable by Goods.Make: {row.Source}/{row.Key}");
            foreach (var text in row.TextKeys)
                if (string.IsNullOrWhiteSpace(text) || !texts.Contains(text)) errors.Add($"missing text: {row.Source}/{row.Key}: {text}");
            foreach (var resource in row.Resources)
                if (string.IsNullOrWhiteSpace(resource) || !resources.Contains(resource)) errors.Add($"missing resource: {row.Source}/{row.Key}: {resource}");
        }
        foreach (var identity in identities)
        {
            if (identity.Goods != null && !keys.Contains(identity.Goods)) errors.Add($"missing definition: {identity.Id}/{identity.Goods}");
            var definition = definitions.FirstOrDefault(d => d.Key == identity.Goods);
            if (definition != null && !definition.IndependentShell && string.IsNullOrWhiteSpace(identity.LegacyId))
                errors.Add($"missing shell base: {definition.Source}/{definition.Key}/{identity.Id}");
        }
        return errors;
    }
    internal static List<string> ValidateSpritePaths(IReadOnlyList<FactoryRow> identities, Func<FactoryRow, bool> resolves) =>
        identities.Where(i => string.IsNullOrWhiteSpace(i.Sprite) || !resolves(i))
            .Select(i => $"unresolved identity sprite: {i.Id}/{i.Sprite}").ToList();
}
