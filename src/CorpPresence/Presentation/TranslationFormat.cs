using System.Collections.Generic;

namespace CorpPresence;

internal static class TranslationFormat
{
    internal static bool Compatible(string source, string translated)
    {
        if (!TryIndices(translated, out var actual)) return false;
        return TryIndices(source, out var expected) ? expected.SetEquals(actual) : actual.Count == 0;
    }
    private static bool TryIndices(string text, out HashSet<int> indices)
    {
        indices = new HashSet<int>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '}') { if (i + 1 < text.Length && text[i + 1] == '}') { i++; continue; } return false; }
            if (text[i] != '{') continue;
            if (i + 1 < text.Length && text[i + 1] == '{') { i++; continue; }
            i++;
            if (!Number(text, ref i, out int index)) return false;
            indices.Add(index);
            Spaces(text, ref i);
            if (i < text.Length && text[i] == ',')
            {
                i++; Spaces(text, ref i);
                if (i < text.Length && text[i] == '-') i++;
                if (!Number(text, ref i, out _)) return false;
                Spaces(text, ref i);
            }
            if (i < text.Length && text[i] == ':')
            {
                i++;
                while (i < text.Length && text[i] != '}') { if (text[i] == '{') return false; i++; }
            }
            if (i >= text.Length || text[i] != '}') return false;
        }
        return true;
    }
    private static bool Number(string text, ref int index, out int value)
    {
        value = 0; int start = index;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            value = value * 10 + text[index++] - '0';
            if (value > 1024) return false;
        }
        return index != start;
    }
    private static void Spaces(string text, ref int index) { while (index < text.Length && text[index] == ' ') index++; }
}
