using System;
using System.Collections.Generic;
using System.Reflection;

namespace RootAccess.Core
{
    /// <summary>
    /// Collects every bilingual text exposed as a static string property under a set of classes (such as
    /// <see cref="Strings"/>), read once in Romanian and once in English. The content generators use the result to bake
    /// the English next to each Romanian text they write into assets and scenes; the tests use it to check that nothing
    /// was left untranslated.
    /// </summary>
    public static class TextCatalog
    {
        public readonly struct Entry
        {
            public readonly string Path;
            public readonly string Romanian;
            public readonly string English;

            public Entry(string path, string romanian, string english)
            {
                Path = path;
                Romanian = romanian;
                English = english;
            }
        }

        /// <summary>Every static string property of <paramref name="roots"/> and their nested classes, in both languages.</summary>
        public static List<Entry> Collect(params Type[] roots)
        {
            var entries = new List<Entry>();
            foreach (Type root in roots)
                if (root != null) Walk(root, root.Name, entries);
            return entries;
        }

        /// <summary>
        /// Romanian text → English text. Two properties with the same Romanian text must agree on the English one,
        /// otherwise a baked label could not know which translation it needs.
        /// </summary>
        public static Dictionary<string, string> EnglishByRomanian(IEnumerable<Entry> entries)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Entry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Romanian)) continue;
                if (map.TryGetValue(entry.Romanian, out string known))
                {
                    if (known != entry.English)
                        throw new InvalidOperationException(
                            $"[TextCatalog] '{entry.Romanian}' is translated as '{known}' ({owners[entry.Romanian]}) and as '{entry.English}' ({entry.Path}).");
                    continue;
                }

                map.Add(entry.Romanian, entry.English);
                owners.Add(entry.Romanian, entry.Path);
            }

            return map;
        }

        private static void Walk(Type type, string path, List<Entry> entries)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                if (property.PropertyType != typeof(string) || property.GetIndexParameters().Length > 0) continue;
                MethodInfo getter = property.GetGetMethod();
                if (getter == null) continue;
                string ro = Loc.In(Language.Romanian, () => (string)getter.Invoke(null, null));
                string en = Loc.In(Language.English, () => (string)getter.Invoke(null, null));
                entries.Add(new Entry($"{path}.{property.Name}", ro, en));
            }

            foreach (Type nested in type.GetNestedTypes(BindingFlags.Public))
                Walk(nested, $"{path}.{nested.Name}", entries);
        }
    }
}
