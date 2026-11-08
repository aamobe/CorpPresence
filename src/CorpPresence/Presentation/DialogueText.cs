namespace CorpPresence;

internal static class DialogueText
{
    internal static string WithAction(string action, string line, bool showAction = true)
    {
        // Never hide a standalone action: it may be the entire dialogue beat.
        if (!showAction && !string.IsNullOrWhiteSpace(line)) return line;
        return L.En ? "<i>(" + action + ")</i> " + line : "<i>（" + action + "）</i>" + line;
    }
}
