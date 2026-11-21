// Twee talen voor Jaaps Nederlandse versie: Nederlands is de eerste taal, Engels blijft beschikbaar
// (menu Opties, Taal). Elke tekst staat als paar in de code: L.T("Nederlands", "English").
using System;
using System.IO;
using System.Text;

namespace ElevenLabsMusicGenerator
{
    internal static class L
    {
        public static bool Engels { get; private set; }

        /// <summary>Alleen voor de controle (--controle): taal wisselen zonder te bewaren.</summary>
        internal static void ZetVoorControle(bool engels) { Engels = engels; }

        public static string T(string nl, string en)
        {
            return Engels ? en : nl;
        }

        private static string Pad { get { return Path.Combine(AppPaths.UserFolder, "Taal.txt"); } }

        /// <summary>Leest de gekozen taal. Zonder keuze: Nederlands.</summary>
        public static void Laad()
        {
            try { Engels = File.Exists(Pad) && File.ReadAllText(Pad, Encoding.UTF8).Trim().Equals("en", StringComparison.OrdinalIgnoreCase); }
            catch (Exception) { Engels = false; }
        }

        public static void Bewaar(bool engels)
        {
            Directory.CreateDirectory(AppPaths.UserFolder);
            File.WriteAllText(Pad, engels ? "en" : "nl", new UTF8Encoding(false));
        }
    }
}
