using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ElevenLabsMusicGenerator
{
    internal sealed class VideoSoundtrackForm : Form
    {
        private readonly AppSettings settings;
        private readonly ListBox videos = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, AccessibleName = L.T("Video's in afspeelvolgorde", "Videos in playback order") };
        private readonly TextBox description = new TextBox { Multiline = true, AcceptsReturn = true, MaxLength = 4100, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, AccessibleName = L.T("Beschrijving van de muziek", "Soundtrack description"), Height = 90 };
        private readonly TextBox tags = new TextBox { Multiline = true, AcceptsReturn = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, AccessibleName = L.T("Stijlen, een per regel", "Style tags, one per line"), Height = 55 };
        private readonly TextBox name = new TextBox { Dock = DockStyle.Fill, AccessibleName = L.T("Naam van de map", "Soundtrack folder name") };
        private readonly CheckBox effects = new CheckBox { Text = L.T("Geluids&effecten toevoegen", "Add sound &effects"), AutoSize = true, AccessibleName = L.T("Geluidseffecten toevoegen", "Add sound effects") };
        private readonly CheckBox separate = new CheckBox { Text = L.T("Losse s&poren", "Separate &tracks"), AutoSize = true, AccessibleName = L.T("Losse sporen", "Separate tracks") };
        private readonly CheckBox reference = new CheckBox { Text = L.T("Het o&riginele geluid als voorbeeld gebruiken", "Use original &audio as reference"), AutoSize = true, AccessibleName = L.T("Het originele geluid als voorbeeld gebruiken", "Use original audio as reference") };
        private readonly ComboBox model = new PrefixComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = L.T("Model voor de muziek", "Soundtrack model"), Width = 150 };
        private readonly ComboBox format = new PrefixComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = L.T("Formaat van de muziek", "Soundtrack output format"), Width = 230 };
        private readonly AccessibleStatusTextBox status = new AccessibleStatusTextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, AccessibleName = L.T("Status", "Soundtrack status"), Text = L.T("Klaar.", "Ready."), Height = 65 };
        private readonly ListBox results = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, AccessibleName = L.T("Gemaakte opnames", "Soundtrack recordings"), Height = 65 };
        private readonly Button generate = new ShortcutButton { Text = L.T("&Genereren", "&Generate"), AutoSize = true, AccessibleName = L.T("Muziek genereren", "Generate soundtrack"), ShortcutText = "Ctrl+Enter" };
        private readonly Button cancel = new Button { Text = L.T("Genereren &annuleren", "&Cancel generation"), AutoSize = true, Enabled = false };
        private readonly Playback playback = new Playback();
        private readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Height = 12, AccessibleName = L.T("Voortgang", "Soundtrack progress"), TabStop = false };
        private readonly Label elapsed = new Label { AutoSize = true, Text = L.T("Verstreken: 0 seconden.", "Elapsed: 0 seconds."), AccessibleName = L.T("Verstreken tijd", "Elapsed time") };
        private readonly System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        private readonly System.Windows.Forms.Timer elapsedTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        private SoundtrackProgress latestProgress;
        private readonly List<Control> inputs = new List<Control>();
        private CancellationTokenSource cancellation;
        private bool readingVideos;
        public readonly List<string> Completed = new List<string>();
        private static readonly string[] Models = { "music_v2_5", "music_v2", "music_v1" };
        private static readonly string[] Formats = { "mp3_44100_128", "mp3_44100_192", "pcm_44100", "mp3_48000_192", "mp3_48000_240", "mp3_48000_320" };

        public VideoSoundtrackForm(AppSettings settings)
        {
            this.settings = settings;
            Text = L.T("Muziek bij een video", "Video soundtrack"); ShowInTaskbar = false; StartPosition = FormStartPosition.CenterParent;
            Size = new Size(820, 790); MinimumSize = new Size(680, 650);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12), ColumnCount = 1 };
            Controls.Add(root);
            root.Controls.Add(new Label { Text = L.T("&Video's:", "&Videos:"), AutoSize = true }); videos.Height = 90; root.Controls.Add(videos);
            var fileButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            AddButton(fileButtons, L.T("&Bestanden toevoegen...", "Add &files..."), AddVideos, L.T("Kies tot 10 video's. Samen hooguit 200 MB en 10 minuten. Kiezen stuurt ze nog niet op.", "Choose up to 10 videos. Together they must be at most 200 MB and 10 minutes. Selecting does not upload them."), true);
            AddButton(fileButtons, L.T("&Weghalen", "&Remove"), delegate { if (videos.SelectedIndex >= 0) videos.Items.RemoveAt(videos.SelectedIndex); }, L.T("Haal de gekozen video uit dit verzoek. Het origineel blijft bestaan.", "Remove the selected video from this request, without deleting the original."), true);
            AddButton(fileButtons, L.T("Om&hoog", "Move &up"), delegate { MoveVideo(-1); }, L.T("Zet de gekozen video eerder in de volgorde.", "Move the selected video earlier in playback order."), true);
            AddButton(fileButtons, L.T("Om&laag", "Move &down"), delegate { MoveVideo(1); }, L.T("Zet de gekozen video later in de volgorde.", "Move the selected video later in playback order."), true); root.Controls.Add(fileButtons);
            root.Controls.Add(new Label { Text = L.T("Beschri&jving (mag leeg):", "Descri&ption (optional):"), AutoSize = true }); root.Controls.Add(description);
            root.Controls.Add(new Label { Text = L.T("&Stijlen (mag leeg, een per regel):", "St&yle tags (optional, one per line):"), AutoSize = true }); root.Controls.Add(tags);
            root.Controls.Add(new Label { Text = L.T("&Naam van de map (mag leeg):", "Folder &name (optional):"), AutoSize = true }); root.Controls.Add(name);
            var options = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; options.Controls.AddRange(new Control[] { effects, separate, reference }); root.Controls.Add(options);
            var formats = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            formats.Controls.Add(new Label { Text = L.T("&Model:", "&Model:"), AutoSize = true }); formats.Controls.Add(model);
            formats.Controls.Add(new Label { Text = L.T("F&ormaat:", "F&ormat:"), AutoSize = true }); formats.Controls.Add(format); root.Controls.Add(formats);
            model.Items.AddRange(new object[] { L.T("Muziek v2.5", "Music v2.5"), L.T("Muziek v2", "Music v2"), L.T("Muziek v1", "Music v1") }); model.SelectedIndex = 0;
            format.Items.AddRange(new object[] { L.T("MP3 44,1 kHz, 128 kbps", "MP3 44.1 kHz, 128 kbps"), L.T("MP3 44,1 kHz, 192 kbps", "MP3 44.1 kHz, 192 kbps"), L.T("WAV (PCM) 44,1 kHz", "PCM WAV 44.1 kHz"), "MP3 48 kHz, 192 kbps", "MP3 48 kHz, 240 kbps", "MP3 48 kHz, 320 kbps" });
            format.SelectedIndex = Math.Max(0, Array.IndexOf(Formats, settings.OutputFormat));
            root.Controls.Add(status); root.Controls.Add(elapsed); root.Controls.Add(progress); root.Controls.Add(results);
            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; buttons.Controls.Add(generate); buttons.Controls.Add(cancel);
            var checkProgress = new ShortcutButton { Text = L.T("Voortgang be&kijken...", "C&heck progress..."), AutoSize = true, AccessibleName = L.T("Voortgang bekijken", "Check soundtrack progress"), ShortcutText = "F5" };
            ContextHelp.SetInstructions(checkProgress, L.T("Toont de huidige stap, de verstreken tijd en hoeveel er is verstuurd. F5. Dit vraagt niets extra aan ElevenLabs; ElevenLabs geeft geen percentage of resterende tijd.", "Show the current stage, elapsed time and real transfer counts in a read-only dialog. F5. This reads local progress without another API request; ElevenLabs provides no generation percentage or remaining time."));
            checkProgress.Click += delegate { ShowProgress(); }; buttons.Controls.Add(checkProgress);
            AddButton(buttons, L.T("Afspelen", "Play"), PlaySelected, L.T("Speel de gekozen opname af. In de lijst met opnames werkt Enter ook.", "Play the selected soundtrack recording. Enter also plays in the recordings list."), false);
            AddButton(buttons, L.T("S&toppen", "&Stop"), playback.Stop, L.T("Stop het afspelen.", "Stop audio playback."), false);
            AddButton(buttons, L.T("&Uitvoermap openen", "&Browse output"), delegate { Directory.CreateDirectory(settings.DefaultOutputFolder); System.Diagnostics.Process.Start(settings.DefaultOutputFolder); }, L.T("Open de uitvoermap uit Voorkeuren.", "Open the output folder configured in Preferences."), false);
            AddButton(buttons, L.T("Slu&iten", "C&lose"), Close, L.T("Sluit dit venster. Wat bezig is, moet eerst klaar zijn of geannuleerd worden.", "Close this dialog. An active generation must finish or cancel first."), false); root.Controls.Add(buttons);
            inputs.AddRange(new Control[] { videos, description, tags, name, effects, separate, reference, model, format });
            description.AccessibleDescription = L.T("Aanwijzingen voor de muziek, tot 4100 tekens. Laat leeg als de video de muziek mag bepalen.", "Optional musical direction, up to 4100 characters. Leave blank to let the video guide the soundtrack.");
            tags.AccessibleDescription = L.T("Stijlen voor de muziek, zoals cinematic of upbeat. Tot 10 stijlen, elk op een eigen regel.", "Optional musical styles, such as cinematic or upbeat. Enter up to 10 styles, each on its own line.");
            name.AccessibleDescription = L.T("Naam voor een nieuwe submap. Er komt een uniek stukje achter, zodat niets wordt overschreven.", "Optional name for a new soundtrack subfolder. A unique suffix prevents existing recordings being overwritten.");
            effects.AccessibleDescription = L.T("Voeg geluidseffecten toe bij wat er in de video gebeurt.", "Include sound-effect cues based on events in the video.");
            separate.AccessibleDescription = L.T("Bewaar muziek en geluidseffecten als losse opnames in plaats van samen gemengd.", "Save returned music and sound effects as separate recordings rather than a mixed soundtrack.");
            reference.AccessibleDescription = L.T("Gebruik het geluid van de video als voorbeeld voor stijl, tempo, instrumenten en sfeer. Het wordt niet in de uitkomst gekopieerd.", "Use the video's existing audio to guide genre, tempo, instruments, mood and musical progression. It is not copied into the output.");
            model.AccessibleDescription = L.T("Het muziekmodel. Muziek v2.5 is eerst gekozen.", "Music model used to generate the soundtrack. Music v2.5 is selected initially.");
            format.AccessibleDescription = L.T("Kies het geluidsformaat. PCM wordt bewaard als stereo WAV. Niet elk abonnement kan alles.", "Choose the audio format. PCM is saved as stereo WAV. Subscription restrictions can apply.");
            videos.AccessibleDescription = L.T("De video's worden in deze volgorde achter elkaar gezet. Kiezen stuurt nog niets op.", "Videos are joined in this order. Selecting a file does not upload it. Use Add, Remove and Move to change the request.");
            generate.AccessibleDescription = L.T("Controleert de video's en vraagt of ze naar ElevenLabs mogen. Dit kost tegoed; de originelen veranderen niet.", "Validate the videos, then confirm their upload to ElevenLabs. Generation can spend credits; the originals are not changed.");
            results.AccessibleDescription = L.T("Klare opnames, vanzelf bewaard. Enter speelt af; Escape stopt.", "Completed recordings saved automatically. Enter plays the selected recording; Escape stops playback.");
            ContextHelp.SetInstructions(status, L.T("Toont de voortgang en fouten. Er wordt pas iets opgestuurd als je Genereren bevestigt.", "Shows upload progress, generation status and errors. No files are uploaded until you confirm Generate."));
            foreach (var control in inputs.Concat(new Control[] { generate, results }))
            {
                if (!string.IsNullOrWhiteSpace(control.AccessibleDescription)) ContextHelp.SetInstructions(control, control.AccessibleDescription);
                control.AccessibleDescription = null;
            }
            generate.Click += async delegate { await Generate(); };
            cancel.Click += delegate { if (cancellation != null) cancellation.Cancel(); SetStatus(L.T("Annuleren. Een verzoek dat al is aangenomen, kan nog tegoed kosten.", "Cancelling. An accepted request can still be charged.")); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (cancellation != null || readingVideos) { e.Cancel = true; if (cancellation != null) cancellation.Cancel(); SetStatus(L.T("Wacht tot het huidige werk gestopt is voordat je sluit.", "Please wait for the current operation to stop before closing.")); } };
            elapsedTimer.Tick += delegate { elapsed.Text = L.T("Verstreken: ", "Elapsed: ") + SoundtrackProgress.ElapsedText(clock.Elapsed) + "."; };
            Disposed += delegate { elapsedTimer.Dispose(); playback.Dispose(); };
            playback.Failed += ShowError;
            results.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; PlaySelected(); } };
            Shown += delegate { videos.Focus(); };
        }

        private void AddButton(FlowLayoutPanel panel, string text, Action action, string help, bool input)
        {
            var button = new Button { Text = text, AutoSize = true };
            ContextHelp.SetInstructions(button, help);
            button.Click += delegate { action(); }; panel.Controls.Add(button); if (input) inputs.Add(button);
        }
        private void PlaySelected() { var selected = results.SelectedItem as FileInfo; if (selected != null) playback.Play(selected.FullName, settings.PlaybackDevice); }
        private void MoveVideo(int step)
        {
            int index = videos.SelectedIndex;
            if (index < 0 || index + step < 0 || index + step >= videos.Items.Count) return;
            var item = videos.Items[index]; videos.Items.RemoveAt(index); videos.Items.Insert(index + step, item); videos.SelectedIndex = index + step;
        }
        private async void AddVideos()
        {
            using (var picker = new OpenFileDialog { Filter = L.T("Videobestanden|*.mp4;*.mov;*.m4v;*.avi;*.wmv;*.mkv;*.webm|Alle bestanden|*.*", "Video files|*.mp4;*.mov;*.m4v;*.avi;*.wmv;*.mkv;*.webm|All files|*.*"), Multiselect = true })
            {
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                readingVideos = true; foreach (var control in inputs) control.Enabled = false; generate.Enabled = false;
                try
                {
                    foreach (var path in picker.FileNames)
                    {
                        if (videos.Items.Count == VideoSoundtrackRequest.MaximumVideos) throw new InvalidDataException(L.T("Je kunt hooguit 10 video's toevoegen.", "You can add no more than 10 videos."));
                        var video = await Task.Run(() => SoundtrackVideo.Read(path)); videos.Items.Add(video);
                        if (videos.Items.Cast<SoundtrackVideo>().Sum(x => x.Bytes) > VideoSoundtrackRequest.MaximumUploadBytes || videos.Items.Cast<SoundtrackVideo>().Sum(x => x.Seconds) > VideoSoundtrackRequest.MaximumSeconds)
                        {
                            videos.Items.Remove(video);
                            throw new InvalidDataException(L.T("Samen mogen de video's niet groter zijn dan 200 MB of langer dan 10 minuten. Dit bestand is niet toegevoegd.", "The videos together must not exceed 200 MB or 10 minutes. This file was not added."));
                        }
                    }
                    SetStatus(videos.Items.Count + L.T(" video's; ", " videos; ") + videos.Items.Cast<SoundtrackVideo>().Sum(x => x.Seconds).ToString("0.0") + L.T(" seconden in afspeelvolgorde. Nog niets opgestuurd.", " seconds in playback order. Nothing uploaded."));
                }
                catch (Exception ex) { ShowError(ex); }
                finally { readingVideos = false; foreach (var control in inputs) control.Enabled = true; generate.Enabled = true; }
            }
        }
        private async Task Generate()
        {
            if (cancellation != null || readingVideos) return;
            try
            {
                var input = new VideoSoundtrackRequest { Videos = videos.Items.Cast<SoundtrackVideo>().ToList(), Description = description.Text, Tags = tags.Text, BaseName = name.Text, OutputFolder = settings.DefaultOutputFolder, OutputFormat = Formats[format.SelectedIndex], ModelId = Models[model.SelectedIndex], AddSoundEffects = effects.Checked, SeparateTracks = separate.Checked, UseAudioAsReference = reference.Checked };
                input.Validate();
                var key = AppPaths.LoadApiKey();
                if (string.IsNullOrWhiteSpace(key)) throw new InvalidDataException(L.T("Zet eerst je API-sleutel in Voorkeuren.", "Save your API key in Preferences before generating a soundtrack."));
                var summary = L.T("Wil je ", "Upload ") + input.Videos.Count + L.T(" video's (", " videos (") + input.Videos.Sum(x => x.Seconds).ToString("0.0") + L.T(" seconden) naar ElevenLabs sturen?", " seconds) to ElevenLabs?") + Environment.NewLine + Environment.NewLine + L.T("De bestanden worden in deze volgorde achter elkaar gezet. ", "The files will be combined in their listed order. ") + (input.SeparateTracks ? L.T("Losse geluidssporen", "Separate audio tracks") : L.T("Een gemengde geluidsband", "A mixed audio soundtrack")) + L.T(" komen in:", " will be saved under:") + Environment.NewLine + settings.DefaultOutputFolder + Environment.NewLine + Environment.NewLine + L.T("Je originele video's veranderen niet. Dit kost tegoed. Ook annuleren na het opsturen kan nog tegoed kosten.", "Your original videos will not be changed. Generation uses account credits. Cancelling after upload may still be charged.");
                if (MessageBox.Show(this, summary, L.T("Muziek bij de video genereren?", "Generate video soundtrack?"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                cancellation = new CancellationTokenSource(); foreach (var control in inputs) control.Enabled = false; generate.Enabled = false; cancel.Enabled = true;
                clock.Restart(); elapsed.Text = L.T("Verstreken: 0 seconden.", "Elapsed: 0 seconds."); elapsedTimer.Start();
                progress.Style = ProgressBarStyle.Marquee;
                playback.Stop(); latestProgress = null;
                var paths = await Task.Run(() => new ElevenLabsMusicClient(key).GenerateSoundtrack(input, cancellation.Token, update => BeginInvoke((Action)(() => UpdateProgress(update)))));
                results.DisplayMember = "Name";
                foreach (var path in paths) { Completed.Add(path); results.Items.Add(new FileInfo(path)); } results.SelectedIndex = results.Items.Count - paths.Length;
                SetStatus(L.T("Klaar. Bewaard: ", "Generation complete. Saved ") + paths.Length + L.T(" opnames in ", " soundtrack recordings in ") + Path.GetDirectoryName(paths[0]) + "." + Environment.NewLine + L.T("Verstreken: ", "Elapsed: ") + SoundtrackProgress.ElapsedText(clock.Elapsed) + ".");
                if (settings.AutoPlayGenerations) playback.PlaySequence(paths, settings.PlaybackDevice);
                else if (settings.CompletionSound) System.Media.SystemSounds.Asterisk.Play();
            }
            catch (OperationCanceledException) { SetStatus(L.T("Genereren geannuleerd. De originele video's en klare opnames zijn bewaard. Een verzoek dat al is aangenomen, kan nog tegoed kosten.", "Generation cancelled. Original videos and previously completed recordings were kept. An accepted request can still be charged.")); }
            catch (Exception ex) { ShowError(ex); }
            finally
            {
                clock.Stop(); elapsedTimer.Stop(); elapsed.Text = L.T("Verstreken: ", "Elapsed: ") + SoundtrackProgress.ElapsedText(clock.Elapsed) + ".";
                if (cancellation != null) { cancellation.Dispose(); cancellation = null; }
                foreach (var control in inputs) control.Enabled = true; generate.Enabled = true; cancel.Enabled = false;
                progress.Style = ProgressBarStyle.Blocks;
            }
        }
        private void SetStatus(string message) { status.Text = message; status.NotifyValueChanged(); }
        private void UpdateProgress(SoundtrackProgress update)
        {
            if (cancellation == null || cancellation.IsCancellationRequested) return;
            var changed = latestProgress == null || latestProgress.Stage != update.Stage;
            latestProgress = update;
            var percent = update.Percent;
            progress.Style = percent.HasValue ? ProgressBarStyle.Blocks : ProgressBarStyle.Marquee;
            if (percent.HasValue) progress.Value = percent.Value;
            if (changed) SetStatus(update.Message);
        }
        private void ShowProgress()
        {
            var text = cancellation == null || latestProgress == null ? status.Text :
                cancellation.IsCancellationRequested ? L.T("Annuleren.", "Cancelling.") + Environment.NewLine + L.T("Verstreken: ", "Elapsed: ") + SoundtrackProgress.ElapsedText(clock.Elapsed) + L.T(". Een verzoek dat al is aangenomen, kan nog tegoed kosten.", ". An accepted request can still be charged.") : latestProgress.Describe(clock.Elapsed);
            var focused = ContextHelp.FocusedControl(this);
            using (var dialog = new ApiKeyTestResultForm(text, L.T("Voortgang", "Soundtrack progress"))) dialog.ShowDialog(this);
            if (!focused.IsDisposed && focused.CanFocus) focused.Focus();
        }
        private void ShowError(Exception error) { SetStatus(error.Message); using (var dialog = new ApiKeyTestResultForm(error.Message, L.T("Muziek bij een video", "Video soundtrack"))) dialog.ShowDialog(this); }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Enter)) { generate.PerformClick(); return true; }
            if (keyData == Keys.F5) { ShowProgress(); return true; }
            if (keyData == Keys.Escape) { if (cancellation != null) cancellation.Cancel(); else if (playback.IsPlaying || results.Focused) playback.Stop(); else Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
