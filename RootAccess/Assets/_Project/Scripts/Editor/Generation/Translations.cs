using System;
using System.Collections.Generic;
using RootAccess.Core;

namespace RootAccess.EditorTools
{
    /// <summary>
    /// English twin of every Romanian text the generators write (UI strings in <see cref="Strings"/> and the seed content
    /// in <see cref="SeedContent"/>). Content assets store both texts and labels baked into prefabs and scenes get a
    /// <c>LocalizedLabel</c>, so the language can be switched at runtime without regenerating anything.
    /// </summary>
    public static class Translations
    {
        private static Dictionary<string, string> _english;

        private static Dictionary<string, string> Map =>
            _english ??= TextCatalog.EnglishByRomanian(TextCatalog.Collect(typeof(Strings), typeof(SeedContent)));

        /// <summary>English text for a Romanian text the generators know; false for anything else (numbers, names, ids).</summary>
        public static bool TryEnglish(string romanian, out string english)
        {
            english = null;
            return !string.IsNullOrEmpty(romanian) && Map.TryGetValue(romanian, out english);
        }

        /// <summary>English text for content written into an asset; fails the generation when it was never translated.</summary>
        public static string English(string romanian)
        {
            if (string.IsNullOrEmpty(romanian)) return romanian;
            if (TryEnglish(romanian, out string english)) return english;
            throw new InvalidOperationException($"[Translations] No English text for '{romanian}'. Add it next to the Romanian one in Strings or SeedContent.");
        }

        /// <summary>The generators bake Romanian (the source language) whatever language the Editor last used.</summary>
        public static void UseSourceLanguage() => Loc.Set(Language.Romanian);
    }
}
