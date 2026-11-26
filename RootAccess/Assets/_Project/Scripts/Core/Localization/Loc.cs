using System;

namespace RootAccess.Core
{
    /// <summary>Languages the game ships with. Romanian is the source language and the default.</summary>
    public enum Language
    {
        Romanian = 0,
        English = 1
    }

    /// <summary>
    /// The current game language. Every player-facing text is written in both languages next to each other
    /// (<see cref="Strings"/>, the generated content assets, the scene labels) and <see cref="T"/> picks the right one
    /// when the text is drawn, so switching the language from the menu takes effect immediately.
    /// </summary>
    public static class Loc
    {
        private static Language _current = Language.Romanian;

        public static Language Current => _current;
        public static bool IsEnglish => _current == Language.English;

        /// <summary>Romanian or English text for the current language; a missing translation falls back to Romanian.</summary>
        public static string T(string romanian, string english) =>
            _current == Language.English && !string.IsNullOrEmpty(english) ? english : romanian;

        /// <summary>Switches the language and tells every listener (labels, HUD, menus) to redraw.</summary>
        public static void Set(Language language)
        {
            if (!Enum.IsDefined(typeof(Language), language)) language = Language.Romanian;
            if (language == _current) return;
            _current = language;
            EventBus<LanguageChanged>.Publish(new LanguageChanged(language));
        }

        /// <summary>
        /// Reads <paramref name="read"/> as if <paramref name="language"/> were active, without notifying anyone. The
        /// content generators and tests use it to collect both versions of a text.
        /// </summary>
        public static T1 In<T1>(Language language, Func<T1> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            Language previous = _current;
            _current = language;
            try
            {
                return read();
            }
            finally
            {
                _current = previous;
            }
        }

        /// <summary>Short name shown on the language switch ("Română" / "English").</summary>
        public static string NativeName(Language language) => language == Language.English ? "English" : "Română";

        /// <summary>The other language (the main menu switch toggles between the two).</summary>
        public static Language Next(Language language) => language == Language.English ? Language.Romanian : Language.English;
    }
}
