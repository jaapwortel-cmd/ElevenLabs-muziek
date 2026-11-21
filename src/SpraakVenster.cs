// Venster L.T("Spraak en stemmen", "Speech and voices"): de functies van "ElevenLabs Media Suite" (https://github.com/audioses/elevenlabs-media-suite,
// MIT-licentie, Copyright (c) 2026 ElevenLabs Media Suite contributors), opnieuw gemaakt in WinForms voor Jaap Wortel (nov 2026).
// Idee uit de Media Suite: een keuzelijst L.T("Functie", "Function") (Alt+F) wisselt het paneel; de focus springt nooit vanzelf weg;
// lange klussen lopen op de achtergrond, een voor een.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EL = ElevenLabsMusicGenerator.MediaSuite;

namespace ElevenLabsMusicGenerator
{
    internal sealed class SpraakVenster : Form
    {
        public static string[] FunctieNamen { get { return functieNamen; } }
        private static readonly string[] functieNamen =
        {
            L.T("Tekst naar spraak", "Text to speech"), L.T("Gesprek met meerdere stemmen", "Dialogue with multiple voices"), L.T("Stemmen zoeken", "Voice search"), L.T("Stem ontwerpen", "Voice design"), L.T("Stem klonen", "Voice cloning"),
            L.T("Stem omzetten", "Voice changer"), L.T("Achtergrondgeluid weghalen", "Remove background noise"), L.T("Spraak naar tekst", "Speech to text"), L.T("Nasynchroniseren", "Dubbing"), L.T("Tekst uitlijnen op een opname", "Align text to a recording"),
            L.T("Uitspraaklijsten", "Pronunciation dictionaries"), L.T("Geschiedenis", "History")
        };

        private static readonly string GeluidFilter = L.T("Geluid en video|*.mp3;*.wav;*.m4a;*.aac;*.ogg;*.opus;*.flac;*.wma;*.webm;*.mp4;*.mov;*.mkv;*.avi|Alle bestanden|*.*", "Audio and video|*.mp3;*.wav;*.m4a;*.aac;*.ogg;*.opus;*.flac;*.wma;*.webm;*.mp4;*.mov;*.mkv;*.avi|All files|*.*");

        private readonly AppSettings settings;
        private readonly Playback playback = new Playback();
        private EL.JobQueue jobs;
        private readonly ComboBox functie;
        private readonly Panel houder;
        private readonly AccessibleStatusTextBox meldingen;
        private readonly List<TableLayoutPanel> panelen = new List<TableLayoutPanel>();
        internal IList<TableLayoutPanel> Panelen { get { return panelen; } }
        internal Panel Houder { get { return houder; } }
        private readonly Dictionary<TableLayoutPanel, Action> hoofdActie = new Dictionary<TableLayoutPanel, Action>();
        private readonly List<ComboBox> stemKeuzes = new List<ComboBox>();
        private readonly List<EL.VoiceInfo> stemmen = new List<EL.VoiceInfo>();
        private readonly List<Dictionary<string, object>> uitspraakLijsten = new List<Dictionary<string, object>>();
        private int bezig;

        public SpraakVenster(AppSettings settings, int startFunctie)
        {
            this.settings = settings;
            playback.Failed += ex => Meld(L.T("Afspelen lukt niet: ", "Could not play: ") + ex.Message);
            Disposed += delegate { playback.Dispose(); if (jobs != null) jobs.Dispose(); };
            Text = L.T("Spraak en stemmen - ", "Speech and voices - ") + Program.DisplayName;
            AccessibleName = L.T("Spraak en stemmen", "Speech and voices");
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 600);
            Size = new Size(940, 760);
            KeyPreview = true;
            ShowInTaskbar = true;

            stemmen.Add(new EL.VoiceInfo { VoiceId = EL.MediaSuiteModels.DefaultVoiceId, Name = "Rachel", Category = L.T("standaardstem", "default voice") });

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var kop = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, TabIndex = 0 };
            kop.Controls.Add(new Label { Text = L.T("&Functie:", "&Function:"), AutoSize = true, UseMnemonic = true, Margin = new Padding(3, 7, 3, 3), TabIndex = 0 });
            functie = new ShortcutComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320, AccessibleName = L.T("Functie", "Function"), ShortcutText = "Alt+F", TabIndex = 1,
                AccessibleDescription = L.T("Kies wat je wilt doen. Het deel eronder past zich aan. Ctrl+PageDown en Ctrl+PageUp wisselen ook.", "Choose what you want to do. The part below changes to match. Ctrl+PageDown and Ctrl+PageUp also switch.") };
            functie.Items.AddRange(FunctieNamen);
            kop.Controls.Add(functie);
            root.Controls.Add(kop, 0, 0);

            houder = new Panel { Dock = DockStyle.Fill, TabIndex = 1 };
            root.Controls.Add(houder, 0, 1);

            var meldLabel = new Label { Text = L.T("Me&ldingen:", "Message &log:"), AutoSize = true, UseMnemonic = true, TabIndex = 2 };
            meldingen = new AccessibleStatusTextBox { Dock = DockStyle.Top, ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 90,
                TabIndex = 3, Text = L.T("Klaar.", "Ready."), AccessibleName = L.T("Meldingen", "Messages"), ShortcutText = "Alt+L" };
            root.Controls.Add(meldLabel, 0, 2);
            root.Controls.Add(meldingen, 0, 3);
            Controls.Add(root);

            BouwTekstNaarSpraak();
            BouwGesprek();
            BouwStemmenZoeken();
            BouwStemOntwerpen();
            BouwStemKlonen();
            BouwStemOmzetten();
            BouwAchtergrondWeg();
            BouwSpraakNaarTekst();
            BouwNasynchroniseren();
            BouwUitlijnen();
            BouwUitspraaklijsten();
            BouwGeschiedenis();
            foreach (var paneel in panelen) { paneel.Visible = false; houder.Controls.Add(paneel); }
            VulStemKeuzes(null);

            functie.SelectedIndexChanged += delegate { ToonPaneel(functie.SelectedIndex); };
            functie.SelectedIndex = Math.Max(0, Math.Min(FunctieNamen.Length - 1, startFunctie));

            Load += delegate
            {
                jobs = new EL.JobQueue();
                if (!string.IsNullOrEmpty(Sleutel())) { HaalStemmenOp(false); HaalUitspraaklijstenOp(false); }
                else Meld(L.T("Er is nog geen API-sleutel. Zet hem eerst in het hoofdvenster: Opties, Voorkeuren.", "There is no API key yet. Enter it first in the main window: Options, Preferences."));
            };
            Shown += delegate { functie.Focus(); };
            FormClosing += delegate { playback.Stop(); if (jobs != null) jobs.CancelAll(); };
        }

        /// <summary>Wisselt naar een functie (ook vanuit het menu van het hoofdvenster).</summary>
        public void KiesFunctie(int index)
        {
            if (index >= 0 && index < FunctieNamen.Length) functie.SelectedIndex = index;
            functie.Focus();
        }

        private void ToonPaneel(int index)
        {
            for (var i = 0; i < panelen.Count; i++) panelen[i].Visible = i == index;
        }

        private TableLayoutPanel Huidig { get { var i = functie.SelectedIndex; return i >= 0 && i < panelen.Count ? panelen[i] : null; } }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Enter))
            {
                Action actie;
                if (Huidig != null && hoofdActie.TryGetValue(Huidig, out actie)) actie();
                return true;
            }
            if (keyData == (Keys.Control | Keys.PageDown)) { functie.SelectedIndex = (functie.SelectedIndex + 1) % FunctieNamen.Length; functie.Focus(); return true; }
            if (keyData == (Keys.Control | Keys.PageUp)) { functie.SelectedIndex = (functie.SelectedIndex + FunctieNamen.Length - 1) % FunctieNamen.Length; functie.Focus(); return true; }
            if (keyData == Keys.F1) { ContextHelp.Show(this, OpenHandleiding); return true; }
            if (keyData == Keys.Escape)
            {
                if (playback.IsPlaying) { playback.Stop(); Meld(L.T("Afspelen gestopt.", "Playback stopped.")); }
                else if (bezig > 0) { jobs.CancelAll(); Meld(L.T("Gestopt. Wat al bij ElevenLabs bezig was, kan nog even doorlopen.", "Stopped. Work already running at ElevenLabs may continue for a moment.")); }
                else Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ------------------------------------------------------------------ Bouwstenen voor de panelen

        private TableLayoutPanel NieuwPaneel(Action hoofd)
        {
            var paneel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(0, 8, 0, 0) };
            paneel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            paneel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panelen.Add(paneel);
            if (hoofd != null) hoofdActie[paneel] = hoofd;
            return paneel;
        }

        private static string Schoon(string label)
        {
            return label.Replace("&&", "\u0001").Replace("&", string.Empty).Replace("\u0001", "&").TrimEnd(':', ' ');
        }

        private static T Veld<T>(TableLayoutPanel paneel, string label, T control) where T : Control
        {
            var rij = paneel.RowCount++;
            paneel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var tekst = new Label { Text = label, AutoSize = true, UseMnemonic = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 8, 3), TabIndex = rij * 2 };
            if (control.AccessibleName == null) control.AccessibleName = Schoon(label);
            control.TabIndex = rij * 2 + 1;
            if (control.Dock == DockStyle.None) control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            paneel.Controls.Add(tekst, 0, rij);
            paneel.Controls.Add(control, 1, rij);
            return control;
        }

        private static T Breed<T>(TableLayoutPanel paneel, T control) where T : Control
        {
            var rij = paneel.RowCount++;
            paneel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.TabIndex = rij * 2;
            paneel.Controls.Add(control, 0, rij);
            paneel.SetColumnSpan(control, 2);
            return control;
        }

        private static CheckBox Vakje(TableLayoutPanel paneel, string tekst, bool aan)
        {
            return Breed(paneel, new CheckBox { Text = tekst, AutoSize = true, Checked = aan, Margin = new Padding(3, 6, 3, 3) });
        }

        private static FlowLayoutPanel Knoppen(TableLayoutPanel paneel, params Button[] knoppen)
        {
            var rij = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 6) };
            for (var i = 0; i < knoppen.Length; i++) { knoppen[i].TabIndex = i; rij.Controls.Add(knoppen[i]); }
            return Breed(paneel, rij);
        }

        private static Button Knop(string tekst, Action klik, string sneltoets = null)
        {
            var knop = new ShortcutButton { Text = tekst, AutoSize = true, ShortcutText = sneltoets };
            knop.Click += delegate { klik(); };
            return knop;
        }

        private static TextBox Tekstvak(bool meerRegels, int hoogte = 0)
        {
            var vak = new ShortcutTextBox { Multiline = meerRegels, AcceptsReturn = meerRegels, ScrollBars = meerRegels ? ScrollBars.Vertical : ScrollBars.None, WordWrap = true };
            if (meerRegels) vak.Height = hoogte > 0 ? hoogte : 110;
            return vak;
        }

        private static TextBox Uitkomstvak(int hoogte)
        {
            return new ShortcutTextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, Height = hoogte };
        }

        private static ComboBox Keuze(params object[] items)
        {
            var keuze = new ShortcutComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            keuze.Items.AddRange(items);
            if (keuze.Items.Count > 0) keuze.SelectedIndex = 0;
            return keuze;
        }

        private static NumericUpDown Getal(decimal min, decimal max, decimal waarde, int decimalen)
        {
            var getal = new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = decimalen, Increment = decimalen > 0 ? 0.05m : 1, Value = waarde, Width = 90, Anchor = AnchorStyles.Left };
            NumericFieldBehavior.SelectCurrentValueOnFocus(getal);
            return getal;
        }

        private ComboBox StemKeuze()
        {
            var keuze = new ShortcutComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleDescription = L.T("Kies een stem. Meer stemmen vind je bij de functie Stemmen zoeken.", "Choose a voice. Find more voices with the Voice search function.") };
            stemKeuzes.Add(keuze);
            return keuze;
        }

        private TableLayoutPanel BestandRij(TableLayoutPanel paneel, string label, out TextBox pad, string filter)
        {
            var rij = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0) };
            rij.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rij.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var vak = new ShortcutTextBox { Dock = DockStyle.Fill, AccessibleName = Schoon(label), TabIndex = 0 };
            var bladeren = new ShortcutButton { Text = L.T("&Bladeren...", "&Browse..."), AutoSize = true, TabIndex = 1, AccessibleDescription = L.T("Kies een bestand voor ", "Choose a file for ") + Schoon(label).ToLowerInvariant() };
            bladeren.Click += delegate { var gekozen = KiesBestand(filter); if (gekozen != null) vak.Text = gekozen; vak.Focus(); };
            rij.Controls.Add(vak, 0, 0);
            rij.Controls.Add(bladeren, 1, 0);
            pad = vak;
            rij.AccessibleName = Schoon(label);
            return Veld(paneel, label, rij);
        }

        // ------------------------------------------------------------------ 1. Tekst naar spraak

        private TextBox ttsTekst, ttsTaal;
        private ComboBox ttsStem, ttsModel, ttsFormaat, ttsUitspraak;
        private CheckBox ttsEigen;
        private NumericUpDown ttsStabiel, ttsGelijk, ttsStijl, ttsSnelheid;
        private string ttsLaatste;

        private void BouwTekstNaarSpraak()
        {
            var p = NieuwPaneel(MaakSpraak);
            ttsTekst = Veld(p, L.T("&Tekst:", "&Text:"), Tekstvak(true, 150));
            ttsTekst.AccessibleDescription = L.T("De tekst die voorgelezen moet worden. Met het model eleven_v3 kun je ook aanwijzingen tussen haken zetten, zoals [lacht] of [fluistert].", "The text to be spoken. With the eleven_v3 model you can add directions in brackets, such as [laughs] or [whispers].");
            ttsStem = Veld(p, L.T("&Stem:", "&Voice:"), StemKeuze());
            ttsModel = Veld(p, L.T("M&odel:", "M&odel:"), Keuze(ModelKeuzes("tts")));
            ttsFormaat = Veld(p, L.T("Fo&rmaat:", "Fo&rmat:"), FormaatKeuze());
            ttsTaal = Veld(p, L.T("Taal&code (leeg = automatisch, bijvoorbeeld nl):", "Language &code (blank = automatic, for example en):"), Tekstvak(false));
            ttsUitspraak = Veld(p, L.T("&Uitspraaklijst:", "Pron&unciation dictionary:"), Keuze(L.T("(geen)", "(none)")));
            ttsEigen = Vakje(p, L.T("&Eigen stem-instellingen gebruiken", "Use own voice s&ettings"), false);
            ttsStabiel = Veld(p, L.T("Sta&biliteit (0 tot 1):", "Sta&bility (0 to 1):"), Getal(0, 1, 0.5m, 2));
            ttsGelijk = Veld(p, L.T("&Gelijkenis (0 tot 1):", "Similarit&y (0 to 1):"), Getal(0, 1, 0.75m, 2));
            ttsStijl = Veld(p, L.T("St&ijl (0 tot 1):", "Style exa&ggeration (0 to 1):"), Getal(0, 1, 0, 2));
            ttsSnelheid = Veld(p, L.T("S&nelheid (0,7 tot 1,2):", "Spee&d (0.7 to 1.2):"), Getal(0.7m, 1.2m, 1, 2));
            ttsEigen.CheckedChanged += delegate { ZetInstellingenAan(); };
            ZetInstellingenAan();
            Knoppen(p, Knop(L.T("&Maken", "&Make"), MaakSpraak, "Ctrl+Enter"), Knop(L.T("Afs&pelen", "&Play"), delegate { Speel(ttsLaatste); }), Knop(L.T("Opslaan &als...", "Save &as..."), delegate { BewaarAls(ttsLaatste); }),
                Knop(L.T("Stemmen &vernieuwen", "Refres&h voices"), delegate { HaalStemmenOp(true); }));
        }

        private void ZetInstellingenAan()
        {
            ttsStabiel.Enabled = ttsGelijk.Enabled = ttsStijl.Enabled = ttsSnelheid.Enabled = ttsEigen.Checked;
        }

        private void MaakSpraak()
        {
            var verzoek = new EL.TextToSpeechRequest
            {
                Text = ttsTekst.Text,
                VoiceId = GekozenStem(ttsStem),
                ModelId = ModelId(ttsModel),
                OutputFormat = Formaat(ttsFormaat),
                LanguageCode = Leeg(ttsTaal.Text)
            };
            if (ttsEigen.Checked)
                verzoek.Settings = new EL.VoiceSettings { Stability = (double)ttsStabiel.Value, SimilarityBoost = (double)ttsGelijk.Value, Style = (double)ttsStijl.Value, Speed = (double)ttsSnelheid.Value };
            var lijst = ttsUitspraak.SelectedItem as UitspraakItem;
            if (lijst != null) verzoek.Dictionaries.Add(new EL.DictionaryLocator { Id = lijst.Id, VersionId = lijst.VersieId });
            if (string.IsNullOrWhiteSpace(verzoek.Text)) { Meld(L.T("Typ eerst een tekst.", "Type a text first.")); ttsTekst.Focus(); return; }
            var tekst = verzoek.Text;
            var meta = Meta("tekst naar spraak", tekst, verzoek.ModelId, verzoek.VoiceId, verzoek.OutputFormat);
            Doe(L.T("Spraak maken", "Making speech"), c => BewaarAudio(Client().TextToSpeech(verzoek, c), "Spraak", tekst, verzoek.OutputFormat, meta),
                pad => { ttsLaatste = pad; Klaar(L.T("De spraak is klaar: ", "The speech is ready: ") + Path.GetFileName(pad) + L.T(". Druk op Afspelen (Alt+P).", ". Press Play (Alt+P).")); if (settings.AutoPlayGenerations) Speel(pad); });
        }

        // ------------------------------------------------------------------ 2. Gesprek

        private ListBox gesprekRegels;
        private ComboBox gesprekStem, gesprekFormaat;
        private TextBox gesprekTekst, gesprekTaal;
        private string gesprekLaatste;

        private sealed class GesprekRegel
        {
            public string StemId, StemNaam, Tekst;
            public override string ToString() { return StemNaam + ": " + Tekst; }
        }

        private void BouwGesprek()
        {
            var p = NieuwPaneel(MaakGesprek);
            gesprekRegels = Veld(p, L.T("Re&gels van het gesprek:", "Dialogue l&ines:"), new ListBox { Height = 150, IntegralHeight = false });
            gesprekStem = Veld(p, L.T("&Stem voor de nieuwe regel:", "&Voice for the new line:"), StemKeuze());
            gesprekTekst = Veld(p, L.T("&Tekst van de nieuwe regel:", "&Text of the new line:"), Tekstvak(false));
            gesprekTekst.AccessibleDescription = L.T("Typ wat deze stem zegt en druk op Enter om de regel toe te voegen.", "Type what this voice says and press Enter to add the line.");
            gesprekTekst.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None) { VoegRegelToe(); e.Handled = e.SuppressKeyPress = true; } };
            gesprekRegels.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { HaalRegelWeg(); e.Handled = true; } };
            Knoppen(p, Knop(L.T("Regel toe&voegen", "A&dd line"), VoegRegelToe), Knop(L.T("Regel &weghalen", "D&elete line"), HaalRegelWeg, "Delete"),
                Knop(L.T("Alles wiss&en", "Clear everythin&g"), delegate { gesprekRegels.Items.Clear(); Meld(L.T("Alle regels zijn weg.", "All lines have been removed.")); }));
            gesprekTaal = Veld(p, L.T("Taal&code (leeg = automatisch):", "Language &code (blank = automatic):"), Tekstvak(false));
            gesprekFormaat = Veld(p, L.T("Fo&rmaat:", "Fo&rmat:"), FormaatKeuze());
            Knoppen(p, Knop(L.T("&Maken", "&Make"), MaakGesprek, "Ctrl+Enter"), Knop(L.T("Afs&pelen", "&Play"), delegate { Speel(gesprekLaatste); }), Knop(L.T("Opslaan &als...", "Save &as..."), delegate { BewaarAls(gesprekLaatste); }));
        }

        private void VoegRegelToe()
        {
            if (string.IsNullOrWhiteSpace(gesprekTekst.Text)) { Meld(L.T("Typ eerst de tekst van de regel.", "Type the text of the line first.")); gesprekTekst.Focus(); return; }
            var stem = gesprekStem.SelectedItem as EL.VoiceInfo;
            if (stem == null) { Meld(L.T("Kies eerst een stem.", "Choose a voice first.")); return; }
            gesprekRegels.Items.Add(new GesprekRegel { StemId = stem.VoiceId, StemNaam = stem.Name, Tekst = gesprekTekst.Text.Trim() });
            gesprekTekst.Clear();
            Meld(L.T("Regel ", "Line ") + gesprekRegels.Items.Count + L.T(" toegevoegd.", " added."));
        }

        private void HaalRegelWeg()
        {
            var i = gesprekRegels.SelectedIndex;
            if (i < 0) { Meld(L.T("Kies eerst een regel in de lijst.", "Choose a line in the list first.")); return; }
            gesprekRegels.Items.RemoveAt(i);
            if (gesprekRegels.Items.Count > 0) gesprekRegels.SelectedIndex = Math.Min(i, gesprekRegels.Items.Count - 1);
            Meld(L.T("Regel weggehaald.", "Line removed."));
        }

        private void MaakGesprek()
        {
            var verzoek = new EL.DialogueRequest { OutputFormat = Formaat(gesprekFormaat), LanguageCode = Leeg(gesprekTaal.Text) };
            foreach (GesprekRegel regel in gesprekRegels.Items) verzoek.Lines.Add(new EL.DialogueLine(regel.Tekst, regel.StemId));
            if (verzoek.Lines.Count == 0) { Meld(L.T("Voeg eerst regels toe aan het gesprek.", "Add lines to the dialogue first.")); gesprekTekst.Focus(); return; }
            var tekst = string.Join(" ", verzoek.Lines.Select(l => l.Text));
            var meta = Meta("gesprek", string.Join(Environment.NewLine, gesprekRegels.Items.Cast<object>().Select(o => o.ToString())), "eleven_v3", null, verzoek.OutputFormat);
            Doe(L.T("Gesprek maken", "Making dialogue"), c => BewaarAudio(Client().TextToDialogue(verzoek, c), "Gesprekken", tekst, verzoek.OutputFormat, meta),
                pad => { gesprekLaatste = pad; Klaar(L.T("Het gesprek is klaar: ", "The dialogue is ready: ") + Path.GetFileName(pad) + L.T(". Druk op Afspelen (Alt+P).", ". Press Play (Alt+P).")); if (settings.AutoPlayGenerations) Speel(pad); });
        }

        // ------------------------------------------------------------------ 3. Stemmen zoeken

        private ComboBox zoekWaar, zoekGeslacht;
        private TextBox zoekWoord, zoekTaal, zoekDetails;
        private ListBox zoekUitkomst;
        private EL.SharedVoiceQuery zoekVraag;

        private void BouwStemmenZoeken()
        {
            var p = NieuwPaneel(delegate { ZoekStemmen(false); });
            zoekWaar = Veld(p, L.T("&Waar zoeken:", "&Where to search:"), Keuze(L.T("Mijn stemmen", "My voices"), L.T("Bibliotheek van ElevenLabs", "ElevenLabs voice library")));
            zoekWoord = Veld(p, L.T("&Zoekwoord:", "&Keyword:"), Tekstvak(false));
            zoekWoord.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None) { ZoekStemmen(false); e.Handled = e.SuppressKeyPress = true; } };
            zoekGeslacht = Veld(p, L.T("&Geslacht:", "&Gender:"), Keuze(L.T("Alle", "All"), L.T("Man", "Male"), L.T("Vrouw", "Female"), L.T("Neutraal", "Neutral")));
            zoekTaal = Veld(p, L.T("Taal&code (leeg = alle talen, bijvoorbeeld nl):", "Language &code (blank = all languages, for example en):"), Tekstvak(false));
            Knoppen(p, Knop(L.T("Zoe&ken", "&Search"), delegate { ZoekStemmen(false); }, "Ctrl+Enter"), Knop(L.T("Meer &resultaten", "Mo&re results"), delegate { ZoekStemmen(true); }));
            zoekUitkomst = Veld(p, L.T("&Uitkomsten:", "Res&ults:"), new ListBox { Height = 160, IntegralHeight = false });
            zoekDetails = Veld(p, L.T("&Details:", "&Details:"), Uitkomstvak(90));
            zoekUitkomst.SelectedIndexChanged += delegate { ToonStemDetails(); };
            zoekUitkomst.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None) { BeluisterStem(); e.Handled = true; } };
            Knoppen(p, Knop(L.T("&Beluisteren", "Liste&n"), BeluisterStem, "Enter"), Knop(L.T("Gebruik deze &stem", "Use this &voice"), GebruikStem), Knop(L.T("Toevoegen aan &mijn stemmen", "Add to &my voices"), VoegStemToe),
                Knop(L.T("Stem-ID k&opiëren", "C&opy voice ID"), delegate { var v = zoekUitkomst.SelectedItem as EL.VoiceInfo; if (v != null) { Clipboard.SetText(v.VoiceId); Meld(L.T("Stem-ID gekopieerd: ", "Voice ID copied: ") + v.VoiceId); } }));
        }

        private void ZoekStemmen(bool meer)
        {
            var woord = zoekWoord.Text.Trim();
            if (zoekWaar.SelectedIndex == 0)
            {
                if (meer) { Meld(L.T("Bij je eigen stemmen staan alle stemmen al in de lijst.", "All your own voices are already in the list.")); return; }
                Doe(L.T("Stemmen zoeken", "Voice search"), c => Client().ListVoices(woord, c), lijst =>
                {
                    zoekUitkomst.Items.Clear();
                    foreach (var v in lijst) zoekUitkomst.Items.Add(v);
                    Klaar(lijst.Count == 0 ? L.T("Geen stemmen gevonden.", "No voices found.") : lijst.Count + L.T(" stemmen gevonden. Ze staan in de lijst Uitkomsten (Alt+U).", " voices found. They are in the Results list (Alt+U)."));
                });
                return;
            }
            if (meer)
            {
                if (zoekVraag == null) { Meld(L.T("Zoek eerst.", "Search first.")); return; }
                zoekVraag.Page++;
            }
            else
            {
                var geslachten = new[] { null, "male", "female", "neutral" };
                zoekVraag = new EL.SharedVoiceQuery { Page = 0, Search = Leeg(woord), Gender = geslachten[Math.Max(0, zoekGeslacht.SelectedIndex)], Language = Leeg(zoekTaal.Text) };
            }
            var vraag = zoekVraag;
            Doe(L.T("Stemmen zoeken", "Voice search"), c => Client().GetSharedVoices(vraag, c), pagina =>
            {
                if (!meer) zoekUitkomst.Items.Clear();
                foreach (var v in pagina.Voices) zoekUitkomst.Items.Add(v);
                Klaar(pagina.Voices.Count + L.T(" stemmen erbij, samen ", " more voices, ") + zoekUitkomst.Items.Count + (pagina.HasMore ? L.T(". Er zijn er nog meer: Meer resultaten (Alt+R).", " in total. There are more: More results (Alt+R).") : L.T(". Dat zijn ze allemaal.", " in total. That is all of them.")));
            });
        }

        private void ToonStemDetails()
        {
            var v = zoekUitkomst.SelectedItem as EL.VoiceInfo;
            if (v == null) { zoekDetails.Text = string.Empty; return; }
            var tekst = new StringBuilder();
            tekst.AppendLine(L.T("Naam: ", "Name: ") + v.Name);
            if (!string.IsNullOrEmpty(v.Category)) tekst.AppendLine(L.T("Soort: ", "Category: ") + v.Category);
            if (v.Labels != null) foreach (var paar in v.Labels) tekst.AppendLine(paar.Key + ": " + paar.Value);
            foreach (var sleutel in new[] { "gender", "age", "accent", "language", "use_case", "descriptive" })
            {
                var waarde = EL.MediaSuiteClient.Str(v.Raw, sleutel);
                if (!string.IsNullOrEmpty(waarde) && (v.Labels == null || !v.Labels.ContainsKey(sleutel))) tekst.AppendLine(sleutel + ": " + waarde);
            }
            if (!string.IsNullOrEmpty(v.Description)) tekst.AppendLine(L.T("Beschrijving: ", "Description: ") + v.Description);
            tekst.Append(L.T("Stem-ID: ", "Voice ID: ") + v.VoiceId);
            zoekDetails.Text = tekst.ToString();
        }

        private void BeluisterStem()
        {
            var v = zoekUitkomst.SelectedItem as EL.VoiceInfo;
            if (v == null) { Meld(L.T("Kies eerst een stem in de lijst Uitkomsten.", "Choose a voice in the Results list first.")); return; }
            if (string.IsNullOrEmpty(v.PreviewUrl)) { Meld(L.T("Deze stem heeft geen voorbeeld.", "This voice has no preview.")); return; }
            var url = v.PreviewUrl;
            Doe(L.T("Voorbeeld ophalen", "Fetching preview"), c =>
            {
                var pad = Path.Combine(Path.GetTempPath(), "ElevenLabs-voorbeeld-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".mp3");
                File.WriteAllBytes(pad, EL.MediaSuiteClient.FetchPreview(url));
                return pad;
            }, pad => { Meld(L.T("Voorbeeld van ", "Preview of ") + v.Name + "."); Speel(pad); });
        }

        private void GebruikStem()
        {
            var v = zoekUitkomst.SelectedItem as EL.VoiceInfo;
            if (v == null) { Meld(L.T("Kies eerst een stem in de lijst Uitkomsten.", "Choose a voice in the Results list first.")); return; }
            VulStemKeuzes(v);
            Meld(v.Name + L.T(" is nu gekozen bij Tekst naar spraak, Gesprek en Stem omzetten. Werkt een stem uit de bibliotheek niet? Kies dan eerst Toevoegen aan mijn stemmen.", " is now selected for Text to speech, Dialogue and Voice changer. If a library voice does not work, choose Add to my voices first."));
        }

        private void VoegStemToe()
        {
            var v = zoekUitkomst.SelectedItem as EL.VoiceInfo;
            if (v == null) { Meld(L.T("Kies eerst een stem in de lijst Uitkomsten.", "Choose a voice in the Results list first.")); return; }
            var eigenaar = EL.MediaSuiteClient.Str(v.Raw, "public_owner_id");
            if (string.IsNullOrEmpty(eigenaar)) { Meld(L.T("Deze stem staat al bij je eigen stemmen.", "This voice is already one of your own voices.")); return; }
            Doe(L.T("Stem toevoegen", "Adding voice"), c => Client().AddSharedVoice(eigenaar, v.VoiceId, v.Name, c), id =>
            {
                var nieuw = new EL.VoiceInfo { VoiceId = id, Name = v.Name, Category = L.T("mijn stem", "my voice"), PreviewUrl = v.PreviewUrl, Raw = v.Raw };
                VulStemKeuzes(nieuw);
                Klaar(v.Name + L.T(" staat nu bij je eigen stemmen en is gekozen.", " is now one of your own voices and is selected."));
            });
        }

        // ------------------------------------------------------------------ 4. Stem ontwerpen

        private TextBox ontwerpBeschrijving, ontwerpTekst, ontwerpNaam;
        private CheckBox ontwerpZelf;
        private ComboBox ontwerpModel;
        private ListBox ontwerpVoorbeelden;

        private sealed class Voorbeeld
        {
            public string GemaaktId, Pad; public double Seconden; public int Nummer;
            public override string ToString() { return L.T("Voorbeeld ", "Preview ") + Nummer + " (" + Seconden.ToString("0.0", CultureInfo.GetCultureInfo(L.T("nl-NL", "en-GB"))) + L.T(" seconden)", " seconds)"); }
        }

        private void BouwStemOntwerpen()
        {
            var p = NieuwPaneel(OntwerpStem);
            ontwerpBeschrijving = Veld(p, L.T("&Beschrijving van de stem:", "Voice &description:"), Tekstvak(true, 80));
            ontwerpBeschrijving.AccessibleDescription = L.T("Bijvoorbeeld: een warme, rustige mannenstem van rond de 60 met een zacht Nederlands accent. In het Engels werkt het vaak het best.", "For example: a warm, calm male voice of about 60 with a soft Dutch accent.");
            ontwerpTekst = Veld(p, L.T("&Voorbeeldtekst (100 tot 1000 tekens):", "Sample &text (100 to 1000 characters):"), Tekstvak(true, 80));
            ontwerpZelf = Vakje(p, L.T("&Tekst zelf laten maken door ElevenLabs", "Let ElevenLabs &write the text"), true);
            ontwerpZelf.CheckedChanged += delegate { ontwerpTekst.Enabled = !ontwerpZelf.Checked; };
            ontwerpTekst.Enabled = false;
            ontwerpModel = Veld(p, L.T("M&odel:", "M&odel:"), Keuze(ModelKeuzes("ttv")));
            Knoppen(p, Knop(L.T("&Maken", "&Make"), OntwerpStem, "Ctrl+Enter"));
            ontwerpVoorbeelden = Veld(p, L.T("Voo&rbeelden:", "P&reviews:"), new ListBox { Height = 90, IntegralHeight = false });
            ontwerpVoorbeelden.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None) { SpeelVoorbeeld(); e.Handled = true; } };
            ontwerpNaam = Veld(p, L.T("&Naam voor de nieuwe stem:", "&Name for the new voice:"), Tekstvak(false));
            Knoppen(p, Knop(L.T("Afs&pelen", "&Play"), SpeelVoorbeeld, "Enter"), Knop(L.T("Stem be&waren", "&Save voice"), BewaarOntwerp));
        }

        private void OntwerpStem()
        {
            var verzoek = new EL.VoiceDesignRequest { VoiceDescription = ontwerpBeschrijving.Text.Trim(), ModelId = ModelId(ontwerpModel) };
            if (ontwerpZelf.Checked) verzoek.AutoGenerateText = true; else verzoek.Text = ontwerpTekst.Text.Trim();
            if (verzoek.VoiceDescription.Length == 0) { Meld(L.T("Beschrijf eerst de stem.", "Describe the voice first.")); ontwerpBeschrijving.Focus(); return; }
            if (!ontwerpZelf.Checked && verzoek.Text.Length < 100) { Meld(L.T("De voorbeeldtekst moet minstens 100 tekens hebben. Of vink Tekst zelf laten maken aan.", "The sample text needs at least 100 characters. Or check Let ElevenLabs write the text.")); ontwerpTekst.Focus(); return; }
            var map = Map("Stemvoorbeelden");
            Doe(L.T("Stem ontwerpen", "Voice design"), c =>
            {
                string tekst;
                var lijst = Client().DesignVoice(verzoek, out tekst, c);
                var uit = new List<Voorbeeld>();
                for (var i = 0; i < lijst.Count; i++)
                {
                    var pad = Path.Combine(map, EL.AssetNaming.Slugify(verzoek.VoiceDescription) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + (i + 1) + ".mp3");
                    File.WriteAllBytes(pad, lijst[i].Audio);
                    uit.Add(new Voorbeeld { GemaaktId = lijst[i].GeneratedVoiceId, Pad = pad, Seconden = lijst[i].DurationSeconds, Nummer = i + 1 });
                }
                return uit;
            }, uit =>
            {
                ontwerpVoorbeelden.Items.Clear();
                foreach (var v in uit) ontwerpVoorbeelden.Items.Add(v);
                if (uit.Count > 0) ontwerpVoorbeelden.SelectedIndex = 0;
                Klaar(uit.Count + L.T(" voorbeelden gemaakt. Ze staan in de lijst Voorbeelden (Alt+R). Kies er een, geef een naam en druk op Stem bewaren.", " previews made. They are in the Previews list (Alt+R). Choose one, enter a name and press Save voice."));
            });
        }

        private void SpeelVoorbeeld()
        {
            var v = ontwerpVoorbeelden.SelectedItem as Voorbeeld;
            if (v == null) { Meld(L.T("Kies eerst een voorbeeld.", "Choose a preview first.")); return; }
            Speel(v.Pad);
        }

        private void BewaarOntwerp()
        {
            var v = ontwerpVoorbeelden.SelectedItem as Voorbeeld;
            if (v == null) { Meld(L.T("Kies eerst een voorbeeld.", "Choose a preview first.")); return; }
            var naam = ontwerpNaam.Text.Trim();
            if (naam.Length == 0) { Meld(L.T("Typ eerst een naam voor de stem.", "Type a name for the voice first.")); ontwerpNaam.Focus(); return; }
            var beschrijving = ontwerpBeschrijving.Text.Trim();
            Doe(L.T("Stem bewaren", "Saving voice"), c => Client().SaveDesignedVoice(naam, beschrijving, v.GemaaktId, c), id =>
            {
                VulStemKeuzes(new EL.VoiceInfo { VoiceId = id, Name = naam, Category = L.T("mijn stem", "my voice"), Description = beschrijving });
                Klaar(L.T("De stem ", "The voice ") + naam + L.T(" is bewaard bij je eigen stemmen en is gekozen.", " has been saved to your own voices and is selected."));
            });
        }

        // ------------------------------------------------------------------ 5. Stem klonen

        private TextBox kloonNaam, kloonBeschrijving;
        private ListBox kloonOpnames;
        private CheckBox kloonRuis, kloonToestemming;

        private void BouwStemKlonen()
        {
            var p = NieuwPaneel(KloonStem);
            kloonNaam = Veld(p, L.T("&Naam:", "&Name:"), Tekstvak(false));
            kloonBeschrijving = Veld(p, L.T("&Beschrijving:", "&Description:"), Tekstvak(false));
            kloonOpnames = Veld(p, L.T("&Opnames:", "&Recordings:"), new ListBox { Height = 110, IntegralHeight = false });
            kloonOpnames.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) { HaalOpnameWeg(); e.Handled = true; } };
            Knoppen(p, Knop(L.T("Opname &toevoegen...", "Add recordin&gs..."), delegate
            {
                using (var dialoog = new OpenFileDialog { Filter = GeluidFilter, Multiselect = true, Title = L.T("Opnames kiezen", "Choose recordings") })
                    if (dialoog.ShowDialog(this) == DialogResult.OK) { foreach (var pad in dialoog.FileNames) kloonOpnames.Items.Add(pad); Meld(kloonOpnames.Items.Count + L.T(" opnames in de lijst.", " recordings in the list.")); }
            }), Knop(L.T("Opname &weghalen", "Re&move recording"), HaalOpnameWeg, "Delete"));
            kloonRuis = Vakje(p, L.T("&Achtergrondgeluid weghalen uit de opnames", "Remove &background noise from the recordings"), false);
            kloonToestemming = Vakje(p, L.T("&Ik heb toestemming van de persoon van wie deze stem is", "&I have permission from the person whose voice this is"), false);
            Knoppen(p, Knop(L.T("&Klonen", "&Clone"), KloonStem, "Ctrl+Enter"));
        }

        private void HaalOpnameWeg()
        {
            var i = kloonOpnames.SelectedIndex;
            if (i < 0) { Meld(L.T("Kies eerst een opname in de lijst.", "Choose a recording in the list first.")); return; }
            kloonOpnames.Items.RemoveAt(i);
            if (kloonOpnames.Items.Count > 0) kloonOpnames.SelectedIndex = Math.Min(i, kloonOpnames.Items.Count - 1);
            Meld(L.T("Opname weggehaald.", "Recording removed."));
        }

        private void KloonStem()
        {
            if (!kloonToestemming.Checked) { Meld(L.T("Je mag alleen een stem klonen als je toestemming hebt. Vink dat eerst aan.", "You may only clone a voice if you have permission. Check that box first.")); kloonToestemming.Focus(); return; }
            var naam = kloonNaam.Text.Trim();
            var beschrijving = Leeg(kloonBeschrijving.Text);
            var opnames = kloonOpnames.Items.Cast<object>().Select(o => o.ToString()).ToList();
            bool? ruis = kloonRuis.Checked ? (bool?)true : null;
            Doe(L.T("Stem klonen", "Voice cloning"), c => Client().CloneVoice(naam, opnames, beschrijving, null, ruis, c), data =>
            {
                var id = EL.MediaSuiteClient.Str(data, "voice_id");
                if (!string.IsNullOrEmpty(id)) VulStemKeuzes(new EL.VoiceInfo { VoiceId = id, Name = naam, Category = L.T("gekloond", "cloned") });
                Klaar(L.T("De stem ", "The voice ") + naam + L.T(" is gekloond en gekozen.", " has been cloned and is selected."));
            });
        }

        // ------------------------------------------------------------------ 6. Stem omzetten

        private TextBox omzetOpname;
        private ComboBox omzetStem, omzetModel, omzetFormaat;
        private CheckBox omzetRuis;
        private string omzetLaatste;

        private void BouwStemOmzetten()
        {
            var p = NieuwPaneel(ZetStemOm);
            BestandRij(p, L.T("Op&name:", "Recordi&ng:"), out omzetOpname, GeluidFilter);
            omzetStem = Veld(p, L.T("&Stem:", "&Voice:"), StemKeuze());
            omzetModel = Veld(p, L.T("Mo&del:", "Mo&del:"), Keuze(ModelKeuzes("sts")));
            omzetFormaat = Veld(p, L.T("Fo&rmaat:", "Fo&rmat:"), FormaatKeuze());
            omzetRuis = Vakje(p, L.T("Achtergrondgeluid &weghalen", "Remove back&ground noise"), false);
            Knoppen(p, Knop(L.T("&Omzetten", "&Convert"), ZetStemOm, "Ctrl+Enter"), Knop(L.T("Afs&pelen", "&Play"), delegate { Speel(omzetLaatste); }), Knop(L.T("Opslaan &als...", "Save &as..."), delegate { BewaarAls(omzetLaatste); }));
        }

        private void ZetStemOm()
        {
            var verzoek = new EL.SpeechToSpeechRequest { AudioPath = omzetOpname.Text.Trim(), VoiceId = GekozenStem(omzetStem), ModelId = ModelId(omzetModel), OutputFormat = Formaat(omzetFormaat) };
            if (omzetRuis.Checked) verzoek.RemoveBackgroundNoise = true;
            var naam = Path.GetFileNameWithoutExtension(verzoek.AudioPath ?? string.Empty);
            var meta = Meta("stem omzetten", verzoek.AudioPath, verzoek.ModelId, verzoek.VoiceId, verzoek.OutputFormat);
            Doe(L.T("Stem omzetten", "Voice changer"), c => BewaarAudio(Client().SpeechToSpeech(verzoek, c), "Stem omzetten", naam, verzoek.OutputFormat, meta),
                pad => { omzetLaatste = pad; Klaar(L.T("Klaar: ", "Ready: ") + Path.GetFileName(pad) + L.T(". Druk op Afspelen (Alt+P).", ". Press Play (Alt+P).")); if (settings.AutoPlayGenerations) Speel(pad); });
        }

        // ------------------------------------------------------------------ 7. Achtergrondgeluid weghalen

        private TextBox schoonOpname;
        private string schoonLaatste;

        private void BouwAchtergrondWeg()
        {
            var p = NieuwPaneel(MaakSchoon);
            BestandRij(p, L.T("Op&name:", "Recordi&ng:"), out schoonOpname, GeluidFilter);
            Breed(p, new Label { Text = L.T("ElevenLabs haalt muziek, ruis en ander geluid weg. Alleen de stem blijft over.", "ElevenLabs removes music, noise and other sounds. Only the voice remains."), AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            Knoppen(p, Knop(L.T("Geluid &weghalen", "&Remove noise"), MaakSchoon, "Ctrl+Enter"), Knop(L.T("Afs&pelen", "&Play"), delegate { Speel(schoonLaatste); }), Knop(L.T("Opslaan &als...", "Save &as..."), delegate { BewaarAls(schoonLaatste); }));
        }

        private void MaakSchoon()
        {
            var pad = schoonOpname.Text.Trim();
            var meta = Meta("achtergrondgeluid weghalen", pad, null, null, "mp3");
            Doe(L.T("Achtergrondgeluid weghalen", "Remove background noise"), c => BewaarAudio(Client().IsolateVoice(pad, null, c), "Schoon geluid", Path.GetFileNameWithoutExtension(pad), null, meta),
                uit => { schoonLaatste = uit; Klaar(L.T("Klaar: ", "Ready: ") + Path.GetFileName(uit) + L.T(". Druk op Afspelen (Alt+P).", ". Press Play (Alt+P).")); if (settings.AutoPlayGenerations) Speel(uit); });
        }

        // ------------------------------------------------------------------ 8. Spraak naar tekst

        private TextBox sttOpname, sttTaal, sttResultaat;
        private CheckBox sttGeluiden, sttSprekers;
        private NumericUpDown sttAantal;
        private Dictionary<string, object> sttAntwoord;

        private void BouwSpraakNaarTekst()
        {
            var p = NieuwPaneel(SchrijfUit);
            BestandRij(p, L.T("Op&name of video:", "Recordi&ng or video:"), out sttOpname, GeluidFilter);
            sttTaal = Veld(p, L.T("Taal&code (leeg = automatisch, bijvoorbeeld nl):", "Language &code (blank = automatic, for example en):"), Tekstvak(false));
            sttGeluiden = Vakje(p, L.T("&Geluiden aangeven, zoals (lacht) of (applaus)", "Mark &sounds, such as (laughs) or (applause)"), false);
            sttSprekers = Vakje(p, L.T("Sprekers &herkennen", "Identify spea&kers"), true);
            sttAantal = Veld(p, L.T("&Aantal sprekers (0 = automatisch):", "Number of spe&akers (0 = automatic):"), Getal(0, 32, 0, 0));
            Knoppen(p, Knop(L.T("&Uitschrijven", "&Transcribe"), SchrijfUit, "Ctrl+Enter"));
            sttResultaat = Veld(p, L.T("&Resultaat:", "&Result:"), Uitkomstvak(180));
            Knoppen(p, Knop(L.T("Tekst &opslaan...", "Save te&xt..."), delegate { BewaarTekst(sttResultaat.Text, ".txt"); }),
                Knop(L.T("Onder&titels (SRT) opslaan...", "Save s&ubtitles (SRT)..."), delegate { if (sttAntwoord != null) BewaarTekst(EL.Subtitles.ToSrt(sttAntwoord), ".srt"); else Meld(L.T("Schrijf eerst iets uit.", "Transcribe something first.")); }),
                Knop(L.T("JSON op&slaan...", "Save &JSON..."), delegate { if (sttAntwoord != null) BewaarTekst(ReadableJsonText(sttAntwoord), ".json"); else Meld(L.T("Schrijf eerst iets uit.", "Transcribe something first.")); }));
        }

        private void SchrijfUit()
        {
            var verzoek = new EL.SpeechToTextRequest { AudioPath = sttOpname.Text.Trim(), LanguageCode = Leeg(sttTaal.Text), TagAudioEvents = sttGeluiden.Checked };
            if (sttSprekers.Checked) verzoek.Diarize = true;
            if (sttAantal.Value > 0) verzoek.NumSpeakers = (int)sttAantal.Value;
            Doe(L.T("Uitschrijven", "Transcribing"), c => Client().SpeechToText(verzoek, c), antwoord =>
            {
                sttAntwoord = antwoord;
                sttResultaat.Text = LeesbareTekst(antwoord);
                var map = Map("Teksten");
                try
                {
                    var pad = Path.Combine(map, EL.AssetNaming.Slugify(Path.GetFileNameWithoutExtension(verzoek.AudioPath)) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
                    File.WriteAllText(pad, sttResultaat.Text, new UTF8Encoding(true));
                    Klaar(L.T("Uitgeschreven. De tekst staat in het vak Resultaat (Alt+R) en is bewaard als ", "Transcribed. The text is in the Result box (Alt+R) and was saved as ") + Path.GetFileName(pad) + ".");
                }
                catch (Exception ex) { AppLog.WriteException("Tekst bewaren", ex); Klaar(L.T("Uitgeschreven. De tekst staat in het vak Resultaat (Alt+R).", "Transcribed. The text is in the Result box (Alt+R).")); }
            });
        }

        private static string LeesbareTekst(Dictionary<string, object> antwoord)
        {
            var woorden = EL.MediaSuiteClient.List(antwoord, "words");
            var heeftSprekers = woorden.Any(w => !string.IsNullOrEmpty(EL.MediaSuiteClient.Str(w, "speaker_id")));
            var sprekerIds = woorden.Select(w => EL.MediaSuiteClient.Str(w, "speaker_id")).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            if (!heeftSprekers || sprekerIds.Count < 2) return (EL.MediaSuiteClient.Str(antwoord, "text") ?? string.Empty).Trim();
            var uit = new StringBuilder();
            string huidige = null;
            foreach (var w in woorden)
            {
                var spreker = EL.MediaSuiteClient.Str(w, "speaker_id");
                var tekst = EL.MediaSuiteClient.Str(w, "text") ?? string.Empty;
                if (!string.IsNullOrEmpty(spreker) && spreker != huidige)
                {
                    huidige = spreker;
                    if (uit.Length > 0) uit.AppendLine().AppendLine();
                    uit.Append(L.T("Spreker ", "Speaker ") + (sprekerIds.IndexOf(spreker) + 1) + ": ");
                    tekst = tekst.TrimStart();
                }
                uit.Append(tekst);
            }
            return uit.ToString().Trim();
        }

        // ------------------------------------------------------------------ 9. Nasynchroniseren

        private TextBox dubBestand, dubUrl, dubId;
        private ComboBox dubDoel, dubBron;
        private NumericUpDown dubAantal;
        private CheckBox dubWatermerk;
        private System.Windows.Forms.Timer dubTimer;
        private string dubTaal;

        private static readonly string[][] Talen =
        {
            new[] { "nl", L.T("Nederlands", "Dutch") }, new[] { "en", L.T("Engels", "English") }, new[] { "de", L.T("Duits", "German") }, new[] { "fr", L.T("Frans", "French") }, new[] { "es", L.T("Spaans", "Spanish") },
            new[] { "it", L.T("Italiaans", "Italian") }, new[] { "pt", L.T("Portugees", "Portuguese") }, new[] { "pl", L.T("Pools", "Polish") }, new[] { "tr", L.T("Turks", "Turkish") }, new[] { "ar", L.T("Arabisch", "Arabic") },
            new[] { "zh", L.T("Chinees", "Chinese") }, new[] { "ja", L.T("Japans", "Japanese") }, new[] { "ko", L.T("Koreaans", "Korean") }, new[] { "hi", "Hindi" }, new[] { "ru", L.T("Russisch", "Russian") },
            new[] { "sv", L.T("Zweeds", "Swedish") }, new[] { "da", L.T("Deens", "Danish") }, new[] { "fi", L.T("Fins", "Finnish") }, new[] { "no", L.T("Noors", "Norwegian") }, new[] { "uk", L.T("Oekraïens", "Ukrainian") },
            new[] { "cs", L.T("Tsjechisch", "Czech") }, new[] { "el", L.T("Grieks", "Greek") }, new[] { "ro", L.T("Roemeens", "Romanian") }, new[] { "hu", L.T("Hongaars", "Hungarian") }, new[] { "id", L.T("Indonesisch", "Indonesian") }
        };

        private void BouwNasynchroniseren()
        {
            var p = NieuwPaneel(StartNasynchroniseren);
            BestandRij(p, L.T("Op&name of video:", "Recordi&ng or video:"), out dubBestand, GeluidFilter);
            dubUrl = Veld(p, L.T("Of een &webadres:", "Or a &web address:"), Tekstvak(false));
            dubDoel = Veld(p, L.T("&Doeltaal:", "&Target language:"), Keuze(Talen.Select(t => (object)(t[1] + " (" + t[0] + ")")).ToArray()));
            dubDoel.SelectedIndex = 1;
            dubBron = Veld(p, L.T("B&rontaal:", "&Source language:"), Keuze(new object[] { L.T("Automatisch", "Automatic") }.Concat(Talen.Select(t => (object)(t[1] + " (" + t[0] + ")"))).ToArray()));
            dubAantal = Veld(p, L.T("&Aantal sprekers (0 = automatisch):", "Number of spe&akers (0 = automatic):"), Getal(0, 32, 0, 0));
            dubWatermerk = Vakje(p, L.T("Water&merk in de video (nodig bij sommige abonnementen)", "Water&mark in the video (required on some plans)"), false);
            Knoppen(p, Knop(L.T("&Starten", "Sta&rt"), StartNasynchroniseren, "Ctrl+Enter"), Knop(L.T("Stat&us bekijken", "Check stat&us"), delegate { BekijkDubStatus(true); }), Knop(L.T("Downl&oaden", "D&ownload"), DownloadDub));
            dubId = Veld(p, L.T("Klus-&ID:", "Job &ID:"), Tekstvak(false));
            dubId.AccessibleDescription = L.T("Het nummer van de klus bij ElevenLabs. Dit wordt vanzelf ingevuld na Starten.", "The ElevenLabs job number. It is filled in automatically after Start.");
            dubTimer = new System.Windows.Forms.Timer { Interval = 20000 };
            dubTimer.Tick += delegate { BekijkDubStatus(false); };
            Disposed += delegate { dubTimer.Dispose(); };
        }

        private static string TaalCode(ComboBox keuze)
        {
            var tekst = Convert.ToString(keuze.SelectedItem);
            var open = tekst.LastIndexOf('(');
            return open < 0 ? "auto" : tekst.Substring(open + 1).TrimEnd(')');
        }

        private void StartNasynchroniseren()
        {
            var verzoek = new EL.DubbingRequest
            {
                FilePath = Leeg(dubBestand.Text),
                SourceUrl = Leeg(dubUrl.Text),
                TargetLanguage = TaalCode(dubDoel),
                SourceLanguage = TaalCode(dubBron),
                Name = Leeg(Path.GetFileNameWithoutExtension(dubBestand.Text.Trim()))
            };
            if (dubAantal.Value > 0) verzoek.NumSpeakers = (int)dubAantal.Value;
            if (dubWatermerk.Checked) verzoek.Watermark = true;
            if (verzoek.FilePath != null && verzoek.SourceUrl != null) { Meld(L.T("Kies een bestand of een webadres, niet allebei.", "Choose a file or a web address, not both.")); return; }
            var taal = verzoek.TargetLanguage;
            Doe(L.T("Nasynchroniseren starten", "Starting dubbing"), c => Client().StartDubbing(verzoek, c), data =>
            {
                dubId.Text = EL.MediaSuiteClient.Str(data, "dubbing_id") ?? string.Empty;
                dubTaal = taal;
                var seconden = EL.MediaSuiteClient.Num(data, "expected_duration_sec");
                dubTimer.Start();
                Klaar(L.T("Gestart. ElevenLabs is bezig", "Started. ElevenLabs is working") + (seconden > 0 ? L.T(", dat duurt ongeveer ", ", which takes about ") + Math.Ceiling(seconden / 60) + L.T(" minuten", " minutes") : string.Empty) +
                    L.T(". Ik kijk elke 20 seconden en zeg het als het klaar is.", ". I check every 20 seconds and will tell you when it is ready."));
            });
        }

        private void BekijkDubStatus(bool zelfGevraagd)
        {
            var id = dubId.Text.Trim();
            if (id.Length == 0) { dubTimer.Stop(); if (zelfGevraagd) Meld(L.T("Er is nog geen klus. Druk eerst op Starten, of typ een Klus-ID.", "There is no job yet. Press Start first, or type a Job ID.")); return; }
            if (!zelfGevraagd && bezig > 0) return;
            Doe(zelfGevraagd ? L.T("Status bekijken", "Checking status") : null, c => Client().GetDubbingStatus(id, c), status =>
            {
                if (status == "dubbed") { dubTimer.Stop(); Klaar(L.T("Het nasynchroniseren is klaar. Druk op Downloaden (Alt+O).", "Dubbing is ready. Press Download (Alt+O).")); }
                else if (status == "failed") { dubTimer.Stop(); Klaar(L.T("Het nasynchroniseren is mislukt bij ElevenLabs.", "Dubbing failed at ElevenLabs.")); }
                else if (zelfGevraagd) Meld(L.T("ElevenLabs is nog bezig.", "ElevenLabs is still working."));
            });
        }

        private void DownloadDub()
        {
            var id = dubId.Text.Trim();
            if (id.Length == 0) { Meld(L.T("Er is nog geen klus.", "There is no job yet.")); return; }
            var taal = dubTaal ?? TaalCode(dubDoel);
            var naam = Leeg(Path.GetFileNameWithoutExtension(dubBestand.Text.Trim())) ?? id;
            var meta = Meta("nasynchroniseren", naam, null, null, taal);
            Doe(L.T("Downloaden", "Downloading"), c => BewaarAudio(Client().GetDubbedFile(id, taal, c), "Nagesynchroniseerd", naam + " " + taal, null, meta),
                pad => Klaar(L.T("Gedownload: ", "Downloaded: ") + Path.GetFileName(pad) + L.T(". Het staat in de map Nagesynchroniseerd in je uitvoermap.", ". It is in the Nagesynchroniseerd folder in your output folder.")));
        }

        // ------------------------------------------------------------------ 10. Tekst uitlijnen

        private TextBox uitlijnOpname, uitlijnTekst, uitlijnResultaat;
        private Dictionary<string, object> uitlijnAntwoord;

        private void BouwUitlijnen()
        {
            var p = NieuwPaneel(LijnUit);
            BestandRij(p, L.T("Op&name:", "Recordi&ng:"), out uitlijnOpname, GeluidFilter);
            uitlijnTekst = Veld(p, L.T("&Tekst die in de opname gesproken wordt:", "&Text spoken in the recording:"), Tekstvak(true, 140));
            Knoppen(p, Knop(L.T("&Uitlijnen", "&Align"), LijnUit, "Ctrl+Enter"));
            uitlijnResultaat = Veld(p, L.T("&Resultaat:", "&Result:"), Uitkomstvak(150));
            Knoppen(p, Knop(L.T("&Ondertitels (SRT) opslaan...", "&Save subtitles (SRT)..."), delegate { if (uitlijnAntwoord != null) BewaarTekst(EL.Subtitles.ToSrt(uitlijnAntwoord), ".srt"); else Meld(L.T("Lijn eerst iets uit.", "Align something first.")); }));
        }

        private void LijnUit()
        {
            var pad = uitlijnOpname.Text.Trim();
            var tekst = uitlijnTekst.Text;
            Doe(L.T("Uitlijnen", "Aligning"), c => Client().ForcedAlignment(pad, tekst, c), antwoord =>
            {
                uitlijnAntwoord = antwoord;
                uitlijnResultaat.Text = EL.Subtitles.ToSrt(antwoord);
                Klaar(L.T("Uitgelijnd. De ondertitels staan in het vak Resultaat (Alt+R).", "Aligned. The subtitles are in the Result box (Alt+R)."));
            });
        }

        // ------------------------------------------------------------------ 11. Uitspraaklijsten

        private ListBox uitspraakLijst;
        private TextBox uitspraakNaam, uitspraakRegels;

        private sealed class UitspraakItem
        {
            public string Id, VersieId, Naam;
            public override string ToString() { return Naam; }
        }

        private void BouwUitspraaklijsten()
        {
            var p = NieuwPaneel(MaakUitspraaklijst);
            uitspraakLijst = Veld(p, L.T("&Uitspraaklijsten:", "Pron&unciation dictionaries:"), new ListBox { Height = 110, IntegralHeight = false });
            Knoppen(p, Knop(L.T("&Vernieuwen", "Refres&h"), delegate { HaalUitspraaklijstenOp(true); }));
            uitspraakNaam = Veld(p, L.T("&Naam voor een nieuwe lijst:", "&Name for a new dictionary:"), Tekstvak(false));
            uitspraakRegels = Veld(p, L.T("&Regels (één per regel: woord = zo uitspreken):", "&Rules (one per line: word = how to say it):"), Tekstvak(true, 120));
            uitspraakRegels.AccessibleDescription = L.T("Bijvoorbeeld: NVDA = en vee dee aa. Elke regel is een woord, een is-teken en hoe het uitgesproken moet worden.", "For example: NVDA = en vee dee ay. Each line is a word, an equals sign and how it should be pronounced.");
            Knoppen(p, Knop(L.T("Lijst &maken", "&Create dictionary"), MaakUitspraaklijst, "Ctrl+Enter"), Knop(L.T("&PLS-bestand importeren...", "Import &PLS file..."), ImporteerPls));
        }

        private void MaakUitspraaklijst()
        {
            var naam = uitspraakNaam.Text.Trim();
            var regels = new List<EL.PronunciationRule>();
            foreach (var regel in uitspraakRegels.Lines)
            {
                var deel = regel.Split(new[] { '=' }, 2);
                if (deel.Length == 2 && deel[0].Trim().Length > 0 && deel[1].Trim().Length > 0)
                    regels.Add(new EL.PronunciationRule { StringToReplace = deel[0].Trim(), Alias = deel[1].Trim() });
            }
            if (regels.Count == 0) { Meld(L.T("Typ eerst regels, zoals: NVDA = en vee dee aa.", "Type rules first, such as: NVDA = en vee dee ay.")); uitspraakRegels.Focus(); return; }
            Doe(L.T("Uitspraaklijst maken", "Creating dictionary"), c => Client().CreatePronunciationDictionary(naam, regels, null, c), data => { Klaar(L.T("De uitspraaklijst ", "The pronunciation dictionary ") + naam + L.T(" is gemaakt.", " has been created.")); HaalUitspraaklijstenOp(false); });
        }

        private void ImporteerPls()
        {
            var pad = KiesBestand(L.T("Uitspraakbestand (*.pls)|*.pls|Alle bestanden|*.*", "Pronunciation file (*.pls)|*.pls|All files|*.*"));
            if (pad == null) return;
            var naam = uitspraakNaam.Text.Trim();
            if (naam.Length == 0) naam = Path.GetFileNameWithoutExtension(pad);
            Doe(L.T("PLS importeren", "Importing PLS"), c => Client().CreatePronunciationDictionaryFromFile(naam, pad, null, c), data => { Klaar(L.T("De uitspraaklijst ", "The pronunciation dictionary ") + naam + L.T(" is geïmporteerd.", " has been imported.")); HaalUitspraaklijstenOp(false); });
        }

        private void HaalUitspraaklijstenOp(bool zelfGevraagd)
        {
            Doe(zelfGevraagd ? L.T("Uitspraaklijsten ophalen", "Fetching pronunciation dictionaries") : null, c => Client().ListPronunciationDictionaries(c), lijst =>
            {
                var items = lijst.Select(d => new UitspraakItem { Id = EL.MediaSuiteClient.Str(d, "id"), VersieId = EL.MediaSuiteClient.Str(d, "latest_version_id"), Naam = EL.MediaSuiteClient.Str(d, "name") ?? L.T("zonder naam", "untitled") }).ToList();
                uitspraakLijst.Items.Clear();
                foreach (var item in items) uitspraakLijst.Items.Add(item);
                var gekozen = ttsUitspraak.SelectedItem as UitspraakItem;
                ttsUitspraak.Items.Clear();
                ttsUitspraak.Items.Add(L.T("(geen)", "(none)"));
                foreach (var item in items) ttsUitspraak.Items.Add(item);
                ttsUitspraak.SelectedIndex = 0;
                if (gekozen != null) for (var i = 1; i < ttsUitspraak.Items.Count; i++) if (((UitspraakItem)ttsUitspraak.Items[i]).Id == gekozen.Id) ttsUitspraak.SelectedIndex = i;
                if (zelfGevraagd) Klaar(items.Count == 0 ? L.T("Je hebt nog geen uitspraaklijsten.", "You have no pronunciation dictionaries yet.") : items.Count + L.T(" uitspraaklijsten.", " pronunciation dictionaries."));
            }, stil: !zelfGevraagd);
        }

        // ------------------------------------------------------------------ 12. Geschiedenis

        private ListBox geschiedenis;
        private string geschiedenisVolgende;

        private sealed class GeschiedenisItem
        {
            public string Id, Tekst;
            public override string ToString() { return Tekst; }
        }

        private void BouwGeschiedenis()
        {
            var p = NieuwPaneel(delegate { HaalGeschiedenisOp(false); });
            geschiedenis = Veld(p, L.T("&Geschiedenis:", "&History:"), new ListBox { Height = 240, IntegralHeight = false });
            geschiedenis.AccessibleDescription = L.T("Alles wat je bij ElevenLabs met spraak hebt gemaakt, het nieuwste bovenaan.", "Everything you made with speech at ElevenLabs, newest first.");
            geschiedenis.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && e.Modifiers == Keys.None) { SpeelGeschiedenis(false); e.Handled = true; }
                if (e.KeyCode == Keys.Delete) { VerwijderGeschiedenis(); e.Handled = true; }
            };
            Knoppen(p, Knop(L.T("&Vernieuwen", "R&eload"), delegate { HaalGeschiedenisOp(false); }, "Ctrl+Enter"), Knop(L.T("&Meer laden", "Load &more"), delegate { HaalGeschiedenisOp(true); }),
                Knop(L.T("Afs&pelen", "&Play"), delegate { SpeelGeschiedenis(false); }, "Enter"), Knop(L.T("Opslaan &als...", "Save &as..."), delegate { SpeelGeschiedenis(true); }), Knop(L.T("Ve&rwijderen", "&Delete"), VerwijderGeschiedenis, "Delete"));
        }

        private void HaalGeschiedenisOp(bool meer)
        {
            if (meer && geschiedenisVolgende == null) { Meld(L.T("Er is niet meer geschiedenis.", "There is no more history.")); return; }
            var na = meer ? geschiedenisVolgende : null;
            Doe(L.T("Geschiedenis ophalen", "Fetching history"), c => Client().ListHistory(100, na, c), pagina =>
            {
                if (!meer) geschiedenis.Items.Clear();
                foreach (var item in pagina.Items)
                {
                    var datum = DateTimeOffset.FromUnixTimeSeconds((long)EL.MediaSuiteClient.Num(item, "date_unix")).LocalDateTime;
                    var tekst = (EL.MediaSuiteClient.Str(item, "text") ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                    if (tekst.Length > 90) tekst = tekst.Substring(0, 90) + "...";
                    geschiedenis.Items.Add(new GeschiedenisItem
                    {
                        Id = EL.MediaSuiteClient.Str(item, "history_item_id"),
                        Tekst = datum.ToString("d MMMM yyyy HH:mm", CultureInfo.GetCultureInfo(L.T("nl-NL", "en-GB"))) + ", " + (EL.MediaSuiteClient.Str(item, "voice_name") ?? L.T("onbekende stem", "unknown voice")) + ": " + tekst
                    });
                }
                geschiedenisVolgende = pagina.HasMore ? pagina.LastHistoryItemId : null;
                Klaar(geschiedenis.Items.Count + L.T(" items in de geschiedenis", " items in the history") + (pagina.HasMore ? L.T(". Er is nog meer: Meer laden (Alt+M).", ". There is more: Load more (Alt+M).") : "."));
            });
        }

        private void SpeelGeschiedenis(bool opslaan)
        {
            var item = geschiedenis.SelectedItem as GeschiedenisItem;
            if (item == null) { Meld(L.T("Kies eerst een item in de geschiedenis.", "Choose a history item first.")); return; }
            Doe(L.T("Ophalen", "Fetching"), c => BewaarAudio(Client().DownloadHistoryItem(item.Id, c), "Geschiedenis", item.Tekst, null, null), pad => { if (opslaan) BewaarAls(pad); else Speel(pad); });
        }

        private void VerwijderGeschiedenis()
        {
            var item = geschiedenis.SelectedItem as GeschiedenisItem;
            if (item == null) { Meld(L.T("Kies eerst een item in de geschiedenis.", "Choose a history item first.")); return; }
            if (MessageBox.Show(this, L.T("Dit item bij ElevenLabs verwijderen?", "Delete this item at ElevenLabs?") + Environment.NewLine + item.Tekst, Program.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            Doe(L.T("Verwijderen", "Deleting"), c => { Client().DeleteHistoryItem(item.Id, c); return true; }, ok =>
            {
                var i = geschiedenis.Items.IndexOf(item);
                geschiedenis.Items.Remove(item);
                if (geschiedenis.Items.Count > 0) geschiedenis.SelectedIndex = Math.Max(0, Math.Min(i, geschiedenis.Items.Count - 1));
                Klaar(L.T("Verwijderd.", "Deleted."));
            });
        }

        // ------------------------------------------------------------------ Stemmen

        private void HaalStemmenOp(bool zelfGevraagd)
        {
            Doe(zelfGevraagd ? L.T("Stemmen ophalen", "Fetching voices") : null, c => Client().ListVoices(null, c), lijst =>
            {
                stemmen.Clear();
                stemmen.AddRange(lijst);
                if (stemmen.Count == 0) stemmen.Add(new EL.VoiceInfo { VoiceId = EL.MediaSuiteModels.DefaultVoiceId, Name = "Rachel", Category = L.T("standaardstem", "default voice") });
                VulStemKeuzes(null);
                if (zelfGevraagd) Klaar(stemmen.Count + L.T(" stemmen opgehaald.", " voices fetched."));
            }, stil: !zelfGevraagd);
        }

        private void VulStemKeuzes(EL.VoiceInfo kies)
        {
            if (kies != null && !stemmen.Any(v => v.VoiceId == kies.VoiceId)) stemmen.Insert(0, kies);
            foreach (var keuze in stemKeuzes)
            {
                var oud = keuze.SelectedItem as EL.VoiceInfo;
                var oudId = kies != null ? kies.VoiceId : (oud != null ? oud.VoiceId : null);
                keuze.Items.Clear();
                foreach (var v in stemmen) keuze.Items.Add(v);
                keuze.SelectedIndex = 0;
                for (var i = 0; i < stemmen.Count; i++) if (stemmen[i].VoiceId == oudId) keuze.SelectedIndex = i;
            }
        }

        private static string GekozenStem(ComboBox keuze)
        {
            var v = keuze.SelectedItem as EL.VoiceInfo;
            return v == null ? null : v.VoiceId;
        }

        // ------------------------------------------------------------------ Klussen, meldingen en bestanden

        private string Sleutel()
        {
            try { return AppPaths.LoadApiKey(); }
            catch (Exception ex) { AppLog.WriteException("API-sleutel laden", ex); return string.Empty; }
        }

        private EL.MediaSuiteClient Client()
        {
            return new EL.MediaSuiteClient(Sleutel());
        }

        private void Doe<T>(string naam, Func<CancellationToken, T> werk, Action<T> klaar, bool stil = false)
        {
            if (jobs == null) return;
            if (naam != null && string.IsNullOrEmpty(Sleutel())) { Meld(L.T("Er is nog geen API-sleutel. Zet hem eerst in het hoofdvenster: Opties, Voorkeuren.", "There is no API key yet. Enter it first in the main window: Options, Preferences.")); return; }
            bezig++;
            if (naam != null) Meld(naam + L.T("... Even geduld. Met Escape kun je stoppen.", "... Please wait. Press Escape to stop."));
            jobs.Submit(naam ?? "achtergrond", werk, uitkomst =>
            {
                bezig--;
                if (IsDisposed) return;
                try { klaar(uitkomst); }
                catch (Exception ex) { AppLog.WriteException(naam ?? L.T("Spraak en stemmen", "Speech and voices"), ex); Meld(L.T("Er ging iets mis: ", "Something went wrong: ") + ex.Message); }
            }, fout =>
            {
                bezig--;
                if (IsDisposed) return;
                AppLog.WriteException(naam ?? L.T("Spraak en stemmen", "Speech and voices"), fout);
                if (stil) return;
                if (fout is OperationCanceledException) { Meld(L.T("Gestopt.", "Stopped.")); return; }
                Meld((naam ?? L.T("Het", "It")) + L.T(" is niet gelukt. ", " did not succeed. ") + fout.Message);
                SystemSounds.Hand.Play();
            });
        }

        private void Meld(string tekst)
        {
            if (IsDisposed) return;
            meldingen.Text = tekst;
            meldingen.NotifyValueChanged();
            try
            {
                AccessibilityObject.RaiseAutomationNotification(System.Windows.Forms.Automation.AutomationNotificationKind.Other,
                    System.Windows.Forms.Automation.AutomationNotificationProcessing.ImportantMostRecent, tekst);
            }
            catch (Exception) { }
        }

        private void Klaar(string tekst)
        {
            Meld(tekst);
            if (settings.CompletionSound) SystemSounds.Asterisk.Play();
        }

        private string Map(string soort)
        {
            var root = Environment.ExpandEnvironmentVariables((settings.DefaultOutputFolder ?? AppPaths.DefaultMusicFolder).Trim().Trim('"'));
            var map = Path.Combine(root, soort);
            Directory.CreateDirectory(map);
            return map;
        }

        private Dictionary<string, object> Meta(string soort, string tekst, string model, string stem, string formaat)
        {
            var meta = new Dictionary<string, object> { { "soort", soort }, { "gemaakt", DateTime.Now.ToString("s", CultureInfo.InvariantCulture) } };
            if (!string.IsNullOrEmpty(tekst)) meta["tekst"] = tekst;
            if (!string.IsNullOrEmpty(model)) meta["model"] = model;
            if (!string.IsNullOrEmpty(stem)) meta["stem"] = stem;
            if (!string.IsNullOrEmpty(formaat)) meta["formaat"] = formaat;
            return meta;
        }

        /// <summary>Bewaart het geluid in de uitvoermap (achtergronddraad). Ruwe PCM krijgt een WAV-kop.</summary>
        private string BewaarAudio(EL.AudioResult resultaat, string soort, string tekst, string formaat, Dictionary<string, object> meta)
        {
            var pcm = formaat != null && formaat.StartsWith("pcm_", StringComparison.Ordinal);
            var extensie = pcm ? ".wav" : resultaat.Extension;
            string metaPad;
            var root = Environment.ExpandEnvironmentVariables((settings.DefaultOutputFolder ?? AppPaths.DefaultMusicFolder).Trim().Trim('"'));
            var pad = EL.AssetNaming.NextAssetPath(root, soort, tekst, extensie, null, out metaPad);
            if (pcm) SchrijfWav(pad, resultaat.Audio, int.Parse(formaat.Substring(4), CultureInfo.InvariantCulture));
            else File.WriteAllBytes(pad, resultaat.Audio);
            if (meta != null) { if (!string.IsNullOrEmpty(resultaat.RequestId)) meta["request_id"] = resultaat.RequestId; EL.AssetNaming.WriteSidecar(metaPad, meta); }
            AppLog.Write("Bewaard: " + pad);
            return pad;
        }

        private static void SchrijfWav(string pad, byte[] pcm, int frequentie)
        {
            using (var uit = new BinaryWriter(File.Create(pad), Encoding.ASCII))
            {
                uit.Write(Encoding.ASCII.GetBytes("RIFF")); uit.Write(36 + pcm.Length); uit.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                uit.Write(16); uit.Write((short)1); uit.Write((short)1); uit.Write(frequentie); uit.Write(frequentie * 2); uit.Write((short)2); uit.Write((short)16);
                uit.Write(Encoding.ASCII.GetBytes("data")); uit.Write(pcm.Length); uit.Write(pcm);
            }
        }

        private void Speel(string pad)
        {
            if (string.IsNullOrEmpty(pad) || !File.Exists(pad)) { Meld(L.T("Er is nog niets om af te spelen. Maak eerst iets.", "There is nothing to play yet. Make something first.")); return; }
            playback.Play(pad, settings.PlaybackDevice);
        }

        private void BewaarAls(string pad)
        {
            if (string.IsNullOrEmpty(pad) || !File.Exists(pad)) { Meld(L.T("Er is nog niets om op te slaan. Maak eerst iets.", "There is nothing to save yet. Make something first.")); return; }
            var extensie = Path.GetExtension(pad);
            using (var dialoog = new SaveFileDialog { FileName = Path.GetFileName(pad), Filter = L.T("Geluid (*", "Audio (*") + extensie + ")|*" + extensie + L.T("|Alle bestanden|*.*", "|All files|*.*"), Title = L.T("Opslaan als", "Save as") })
            {
                if (dialoog.ShowDialog(this) != DialogResult.OK) return;
                try { File.Copy(pad, dialoog.FileName, true); Meld(L.T("Opgeslagen als ", "Saved as ") + dialoog.FileName + "."); }
                catch (Exception ex) { Meld(L.T("Opslaan lukt niet: ", "Could not save: ") + ex.Message); }
            }
        }

        private void BewaarTekst(string tekst, string extensie)
        {
            if (string.IsNullOrWhiteSpace(tekst)) { Meld(L.T("Er is nog geen tekst om op te slaan.", "There is no text to save yet.")); return; }
            var soort = extensie == ".srt" ? L.T("Ondertitels (*.srt)|*.srt", "Subtitles (*.srt)|*.srt") : extensie == ".json" ? "JSON (*.json)|*.json" : L.T("Tekst (*.txt)|*.txt", "Text (*.txt)|*.txt");
            using (var dialoog = new SaveFileDialog { Filter = soort + L.T("|Alle bestanden|*.*", "|All files|*.*"), DefaultExt = extensie, Title = L.T("Opslaan als", "Save as") })
            {
                if (dialoog.ShowDialog(this) != DialogResult.OK) return;
                try { File.WriteAllText(dialoog.FileName, tekst, new UTF8Encoding(true)); Meld(L.T("Opgeslagen als ", "Saved as ") + dialoog.FileName + "."); }
                catch (Exception ex) { Meld(L.T("Opslaan lukt niet: ", "Could not save: ") + ex.Message); }
            }
        }

        private string KiesBestand(string filter)
        {
            using (var dialoog = new OpenFileDialog { Filter = filter, Title = L.T("Bestand kiezen", "Choose a file") })
                return dialoog.ShowDialog(this) == DialogResult.OK ? dialoog.FileName : null;
        }

        private static string ReadableJsonText(Dictionary<string, object> data)
        {
            return ReadableJson.Format(EL.MediaSuiteClient.Json.Serialize(data));
        }

        private void OpenHandleiding()
        {
            if (!File.Exists(AppPaths.ManualPath)) { Meld(L.T("De handleiding staat niet naast het programma.", "The manual was not found beside the program.")); return; }
            Process.Start(new ProcessStartInfo { FileName = AppPaths.ManualPath, UseShellExecute = true });
        }

        private static string Leeg(string tekst)
        {
            return string.IsNullOrWhiteSpace(tekst) ? null : tekst.Trim();
        }

        private static object[] ModelKeuzes(string soort)
        {
            return EL.MediaSuiteModels.SelectorChoices(soort).Select(m => (object)(m == EL.MediaSuiteModels.LatestLabel ? L.T("Nieuwste (aanbevolen)", "Latest (recommended)") : m)).ToArray();
        }

        private static string ModelId(ComboBox keuze)
        {
            var tekst = Convert.ToString(keuze.SelectedItem);
            return keuze.SelectedIndex <= 0 ? null : tekst;
        }

        private sealed class FormaatItem
        {
            public string Id, Naam;
            public override string ToString() { return Naam; }
        }

        private static ComboBox FormaatKeuze()
        {
            var keuze = Keuze(
                new FormaatItem { Id = "mp3_44100_128", Naam = "MP3, 128 kbps" },
                new FormaatItem { Id = "mp3_44100_192", Naam = L.T("MP3, 192 kbps (betaald abonnement)", "MP3, 192 kbps (paid plan)") },
                new FormaatItem { Id = "pcm_44100", Naam = L.T("WAV, 44,1 kHz (Pro-abonnement)", "WAV, 44.1 kHz (Pro plan)") });
            keuze.AccessibleDescription = L.T("Het soort geluidsbestand dat je krijgt.", "The type of audio file you get.");
            return keuze;
        }

        private static string Formaat(ComboBox keuze)
        {
            var item = keuze.SelectedItem as FormaatItem;
            return item == null ? "mp3_44100_128" : item.Id;
        }
    }
}
