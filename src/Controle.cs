// Controle op toegankelijkheid (--controle <rapport.txt>): bouwt elk venster in het Nederlands en het Engels
// en kijkt of elk bedienbaar onderdeel een naam heeft en of Alt-letters niet dubbel zijn.
// Draait in de bouwronde op GitHub (Windows) en lokaal met Mono. Geeft exitcode 1 bij problemen.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ElevenLabsMusicGenerator
{
    internal static class Controle
    {
        public static bool IsControle(string[] args)
        {
            return args.Length > 0 && args[0].Equals("--controle", StringComparison.OrdinalIgnoreCase);
        }

        public static int Voer(string[] args)
        {
            var rapport = new StringBuilder();
            var fouten = 0;
            foreach (var engels in new[] { false, true })
            {
                L.ZetVoorControle(engels);
                rapport.AppendLine("== " + (engels ? "English" : "Nederlands"));
                var settings = new AppSettings();
                using (var hoofd = new MainForm(null))
                {
                    fouten += Menu(hoofd.MainMenuStrip, rapport, VormLetters(hoofd, c => true));
                    fouten += Groep("Hoofdvenster", hoofd, c => true, rapport);
                }
                using (var spraak = new SpraakVenster(settings, 0))
                {
                    for (var i = 0; i < spraak.Panelen.Count; i++)
                    {
                        var paneel = spraak.Panelen[i];
                        fouten += Groep("Spraak en stemmen, " + SpraakVenster.FunctieNamen[i], spraak, c => c == spraak.Houder ? false : true, rapport, paneel);
                    }
                }
                fouten += Probeer("Voorkeuren", rapport, delegate
                {
                    var f = 0;
                    using (var voorkeuren = new PreferencesForm(settings, 0))
                    {
                        var tabs = Alle(voorkeuren).OfType<TabControl>().First();
                        foreach (TabPage pagina in tabs.TabPages)
                        {
                            var p = pagina;
                            f += Groep("Voorkeuren, " + p.Text, voorkeuren, c => !(c is TabPage) || c == p, rapport);
                        }
                    }
                    return f;
                });
                fouten += Probeer("Compositieplan", rapport, delegate
                {
                    using (var plan = new PlanEditorForm(null, "test", 30, "music_v2_5", Path.GetTempPath(), string.Empty))
                        return Groep("Compositieplan", plan, c => true, rapport);
                });
                fouten += Probeer("Muziek bij een video", rapport, delegate
                {
                    using (var video = new VideoSoundtrackForm(settings))
                        return Groep("Muziek bij een video", video, c => true, rapport);
                });
            }
            rapport.AppendLine(fouten == 0 ? "Geen problemen gevonden." : fouten + " problemen gevonden.");
            var pad = args.Length > 1 ? args[1] : Path.Combine(AppPaths.AppFolder, "controle.txt");
            File.WriteAllText(pad, rapport.ToString(), new UTF8Encoding(true));
            Console.WriteLine(rapport.ToString());
            return fouten == 0 ? 0 : 1;
        }

        private static int Probeer(string naam, StringBuilder rapport, Func<int> werk)
        {
            try { return werk(); }
            catch (DllNotFoundException) { rapport.AppendLine(naam + ": overgeslagen, dit kan alleen op Windows."); return 0; }
        }

        private static IEnumerable<Control> Alle(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var kind in Alle(c)) yield return kind;
            }
        }

        private static IEnumerable<Control> Zichtbaar(Control root, Func<Control, bool> mee)
        {
            foreach (Control c in root.Controls)
            {
                if (!mee(c)) continue;
                yield return c;
                foreach (var kind in Zichtbaar(c, mee)) yield return kind;
            }
        }

        private static char? Letter(string tekst)
        {
            if (string.IsNullOrEmpty(tekst)) return null;
            for (var i = 0; i < tekst.Length - 1; i++)
            {
                if (tekst[i] != '&') continue;
                if (tekst[i + 1] == '&') { i++; continue; }
                return char.ToUpperInvariant(tekst[i + 1]);
            }
            return null;
        }

        private static bool HeeftLetter(Control c)
        {
            var label = c as Label;
            if (label != null) return label.UseMnemonic;
            return c is ButtonBase;
        }

        private static Dictionary<char, string> VormLetters(Control root, Func<Control, bool> mee)
        {
            var letters = new Dictionary<char, string>();
            foreach (var c in Zichtbaar(root, mee))
            {
                if (!HeeftLetter(c)) continue;
                var l = Letter(c.Text);
                if (l.HasValue && !letters.ContainsKey(l.Value)) letters[l.Value] = c.Text;
            }
            return letters;
        }

        private static int Groep(string naam, Form form, Func<Control, bool> mee, StringBuilder rapport, Control extra = null)
        {
            var fouten = 0;
            var controls = Zichtbaar(form, mee).ToList();
            if (extra != null) { controls.Add(extra); controls.AddRange(Alle(extra)); }
            var letters = new Dictionary<char, string>();
            foreach (var c in controls)
            {
                if (HeeftLetter(c))
                {
                    var l = Letter(c.Text);
                    if (l.HasValue)
                    {
                        string ander;
                        if (letters.TryGetValue(l.Value, out ander)) { rapport.AppendLine(naam + ": Alt+" + l.Value + " dubbel: \"" + ander + "\" en \"" + c.Text + "\""); fouten++; }
                        else letters[l.Value] = c.Text;
                    }
                }
                var bedienbaar = c is TextBoxBase || c is ComboBox || c is ListBox || c is NumericUpDown || c is ButtonBase || c is TrackBar || c is ListView || c is TreeView;
                if (bedienbaar && string.IsNullOrWhiteSpace(c.AccessibleName) && string.IsNullOrWhiteSpace(c.Text.Replace("&", string.Empty)))
                {
                    rapport.AppendLine(naam + ": onderdeel zonder naam: " + c.GetType().Name + " (" + c.Name + ")");
                    fouten++;
                }
            }
            return fouten;
        }

        private static int Menu(MenuStrip menu, StringBuilder rapport, Dictionary<char, string> vormLetters)
        {
            var fouten = 0;
            var boven = new Dictionary<char, string>();
            foreach (ToolStripItem item in menu.Items)
            {
                var l = Letter(item.Text);
                if (!l.HasValue) { rapport.AppendLine("Menu: geen Alt-letter bij " + item.Text); fouten++; continue; }
                string ander;
                if (boven.TryGetValue(l.Value, out ander) || vormLetters.TryGetValue(l.Value, out ander)) { rapport.AppendLine("Menu: Alt+" + l.Value + " dubbel: \"" + ander + "\" en \"" + item.Text + "\""); fouten++; }
                boven[l.Value] = item.Text;
                fouten += Uitklap(item as ToolStripMenuItem, rapport);
            }
            return fouten;
        }

        private static int Uitklap(ToolStripMenuItem menu, StringBuilder rapport)
        {
            if (menu == null || menu.DropDownItems.Count == 0) return 0;
            var fouten = 0;
            var letters = new Dictionary<char, string>();
            foreach (ToolStripItem item in menu.DropDownItems)
            {
                if (item is ToolStripSeparator) continue;
                var l = Letter(item.Text);
                if (l.HasValue)
                {
                    string ander;
                    if (letters.TryGetValue(l.Value, out ander)) { rapport.AppendLine("Menu " + menu.Text + ": letter " + l.Value + " dubbel: \"" + ander + "\" en \"" + item.Text + "\""); fouten++; }
                    else letters[l.Value] = item.Text;
                }
                fouten += Uitklap(item as ToolStripMenuItem, rapport);
            }
            return fouten;
        }
    }
}
