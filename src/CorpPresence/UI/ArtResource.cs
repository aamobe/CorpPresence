namespace CorpPresence.UI;

// Shared with offline content checks; no Unity objects or texture cache access.
internal static class ArtResource
{
    internal static string Name(string company, string art) => "CorpPresence.art." + company + "." + art + ".png";
}
