using System;
using System.Collections.Generic;

namespace CorpPresence
{
    // The resource manifest is immutable for the lifetime of this assembly.
    // Keep names only: caching PNG byte arrays would duplicate decoded textures in RAM.
    internal static class PortraitResources
    {
        private static readonly HashSet<string> Names = BuildIndex();

        public static bool Exists(string name)
        {
            return name != null && Names.Contains(name);
        }

        private static HashSet<string> BuildIndex()
        {
            const string prefix = "CorpPresence.portraits.";
            const string suffix = ".png";
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (string resource in typeof(PortraitResources).Assembly.GetManifestResourceNames())
            {
                if (resource.StartsWith(prefix, StringComparison.Ordinal) && resource.EndsWith(suffix, StringComparison.Ordinal))
                    names.Add(resource.Substring(prefix.Length, resource.Length - prefix.Length - suffix.Length));
            }
            return names;
        }
    }
}
