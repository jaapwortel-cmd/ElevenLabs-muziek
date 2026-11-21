using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ElevenLabsMusicGenerator
{
    internal sealed class PreferencesForm : Form
    {
        private static readonly string[] UpdateFrequencies = { "Startup", "Daily", "Weekly", "Never" };
        private readonly AppSettings settings;
        private readonly TabControl tabs;
        private readonly TextBox outputFolderTextBox;
        private readonly NumericUpDown lengthNumeric;
        private readonly NumericUpDown variationsNumeric;
        private readonly CheckBox instrumentalCheckBox;
        private readonly CheckBox detailsCheckBox;
        private readonly CheckBox autoPlayCheckBox;
        private readonly CheckBox completionSoundCheckBox;
        private readonly ComboBox playbackDeviceComboBox;
        private readonly ComboBox formatComboBox;
        private readonly TextBox apiKeyTextBox;
        private readonly CheckBox showKeyCheckBox;
        private readonly Button testKeyButton;
        private readonly AccessibleStatusLabel apiStatusLabel;
        private readonly ComboBox updateFrequencyComboBox;
        private readonly CheckBox silentUpdatesCheckBox;

        public PreferencesForm(AppSettings settings, int initialTab)
        {
            this.settings = settings;
            Text = L.T("Voorkeuren", "Preferences");
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(650, 500);
            Size = new Size(700, 540);
            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            AccessibleName = L.T("Voorkeuren", "ElevenLabs Music Generator preferences");

            tabs = new TabControl { Dock = DockStyle.Fill, AccessibleName = L.T("Soorten voorkeuren", "Preference categories") };
            var generalPage = new TabPage(L.T("Algemeen", "General"));
            var apiPage = new TabPage(L.T("API-sleutel", "API key"));
            var updatesPage = new TabPage(L.T("Updates", "Updates"));
            tabs.TabPages.Add(generalPage);
            tabs.TabPages.Add(apiPage);
            tabs.TabPages.Add(updatesPage);

            var general = NewPageLayout();
            outputFolderTextBox = NewTextBox(L.T("Standaard uitvoermap", "Default output folder"));
            var browseButton = NewButton(L.T("&Bladeren...", "&Browse..."), L.T("Kies de standaard uitvoermap", "Browse for the default output folder"));
            browseButton.Click += delegate { BrowseForOutputFolder(); };
            var folderRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 };
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            folderRow.Controls.Add(outputFolderTextBox, 0, 0);
            folderRow.Controls.Add(browseButton, 1, 0);
            AddLabeledControl(general, L.T("Standaard &uitvoermap:", "Default &output folder:"), folderRow);

            lengthNumeric = new NumericUpDown { Minimum = 3, Maximum = 600, Width = 100, AccessibleName = L.T("Standaardlengte in seconden", "Default length in seconds") };
            variationsNumeric = new NumericUpDown { Minimum = 1, Maximum = 10, Width = 100, AccessibleName = L.T("Standaard aantal varianten", "Default number of variations") };
            NumericFieldBehavior.SelectCurrentValueOnFocus(lengthNumeric);
            NumericFieldBehavior.SelectCurrentValueOnFocus(variationsNumeric);
            instrumentalCheckBox = new CheckBox { Text = L.T("Standaard &instrumentale muziek", "Default to &instrumental music"), AutoSize = true, AccessibleName = L.T("Standaard instrumentale muziek", "Default to instrumental music") };
            detailsCheckBox = new CheckBox { Text = L.T("&Songteksten en details bewaren", "Save &generated lyrics and details"), AutoSize = true, AccessibleName = L.T("Songteksten en details bewaren", "Save generated lyrics and details") };
            formatComboBox = NewDropDown(L.T("Standaardformaat", "Default output format"), new[] { L.T("WAV (PCM), 44,1 kHz", "PCM 44.1 kHz WAV"), L.T("MP3 44,1 kHz, 192 kbps", "MP3 44.1 kHz, 192 kbps"), L.T("MP3 44,1 kHz, 128 kbps", "MP3 44.1 kHz, 128 kbps"), "MP3 48 kHz, 192 kbps", "MP3 48 kHz, 240 kbps", "MP3 48 kHz, 320 kbps" });
            AddLabeledControl(general, L.T("Standaard&lengte in seconden:", "Default &length in seconds:"), lengthNumeric);
            AddLabeledControl(general, L.T("Standaard aantal &varianten:", "Default &variations:"), variationsNumeric);
            AddFullWidthControl(general, instrumentalCheckBox);
            AddFullWidthControl(general, detailsCheckBox);
            generalPage.Controls.Add(general);
            var audioPage = new TabPage(L.T("Geluid", "Audio")); tabs.TabPages.Add(audioPage);
            var audio = NewPageLayout();
            playbackDeviceComboBox = AudioDevicesForm.DeviceList(settings.PlaybackDevice);
            autoPlayCheckBox = new CheckBox { Text = L.T("Nieuwe geluiden &vanzelf na elkaar afspelen", "Play new &generations automatically in sequence"), AutoSize = true, AccessibleName = L.T("Nieuwe geluiden vanzelf na elkaar afspelen", "Play new generations automatically in sequence"), Checked = settings.AutoPlayGenerations };
            completionSoundCheckBox = new CheckBox { Text = L.T("&Geluidje als het klaar is", "Play a &completion sound"), AutoSize = true, AccessibleName = L.T("Geluidje als het klaar is", "Play a completion sound"), Checked = settings.CompletionSound };
            AddLabeledControl(audio, L.T("Geluids&kaart voor afspelen:", "Playback &device:"), playbackDeviceComboBox);
            AddLabeledControl(audio, L.T("Standaard&formaat:", "Default &format:"), formatComboBox);
            AddFullWidthControl(audio, autoPlayCheckBox);
            AddFullWidthControl(audio, completionSoundCheckBox); audioPage.Controls.Add(audio);

            var api = NewPageLayout();
            apiKeyTextBox = NewTextBox(L.T("API-sleutel van ElevenLabs", "ElevenLabs API key"));
            apiKeyTextBox.UseSystemPasswordChar = true;
            showKeyCheckBox = new CheckBox { Text = L.T("API-sleutel &tonen", "&Show API key"), AutoSize = true, AccessibleName = L.T("API-sleutel tonen", "Show API key") };
            showKeyCheckBox.CheckedChanged += delegate { apiKeyTextBox.UseSystemPasswordChar = !showKeyCheckBox.Checked; };
            testKeyButton = NewButton(L.T("API-sleutel t&esten", "&Test API key"), L.T("Test de toegang van de sleutel en toon de uitkomst; bij geluidseffecten wordt eerst gevraagd, want die test kan tegoed kosten", "Test API access and open a readable result dialog; Sound Effects testing asks before generating audio that may spend credits"));
            testKeyButton.Click += TestKeyButtonClick;
            var getKeyLink = new LinkLabel
            {
                AutoSize = true,
                Text = L.T("Een API-sleutel van ElevenLabs maken", "Get an ElevenLabs API key"),
                AccessibleName = L.T("Een API-sleutel van ElevenLabs maken", "Get an ElevenLabs API key"),
                TabStop = true
            };
            getKeyLink.LinkClicked += delegate
            {
                try { Process.Start("https://elevenlabs.io/app/settings/api-keys"); }
                catch (Exception ex) { SetApiStatus(L.T("De pagina voor API-sleutels kon niet worden geopend: ", "Could not open the API key page: ") + ex.Message); }
            };
            apiStatusLabel = new AccessibleStatusLabel { AutoSize = true, MaximumSize = new Size(610, 0), Text = L.T("Een test voor muziek maakt geen geluid. Een test voor geluidseffecten vraagt eerst toestemming en kan tegoed kosten.", "Music key tests do not generate audio. Sound Effects tests require confirmation and may spend credits."), AccessibleName = L.T("Status van de API-sleutel", "API key status") };
            AddLabeledControl(api, L.T("API-&sleutel:", "API &key:"), apiKeyTextBox);
            AddFullWidthControl(api, showKeyCheckBox);
            AddFullWidthControl(api, testKeyButton);
            AddFullWidthControl(api, getKeyLink);
            AddFullWidthControl(api, apiStatusLabel);
            apiPage.Controls.Add(api);

            var updates = NewPageLayout();
            updateFrequencyComboBox = NewDropDown(L.T("Zoeken naar updates", "Check for updates"), new[] { L.T("Bij het starten", "At startup"), L.T("Elke dag", "Daily"), L.T("Elke week", "Weekly"), L.T("Nooit", "Never") });
            silentUpdatesCheckBox = new CheckBox { Text = L.T("Gecontroleerde updates &stil installeren", "Install verified updates &silently"), AutoSize = true, AccessibleName = L.T("Gecontroleerde updates stil installeren", "Install verified updates silently") };
            var updateNote = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(610, 0),
                Text = L.T("Een update wordt alleen geinstalleerd als het pakket een geldige handtekening heeft. Instellingen, de map User, prompts, API-sleutels, logboeken en gemaakte geluiden blijven bewaard.", "Updates are accepted only when the package has a valid project signature. Settings, the User folder, prompts, API keys, logs, and generated audio are preserved.")
            };
            AddLabeledControl(updates, L.T("&Zoeken naar updates:", "&Check for updates:"), updateFrequencyComboBox);
            AddFullWidthControl(updates, silentUpdatesCheckBox);
            AddFullWidthControl(updates, updateNote);
            updatesPage.Controls.Add(updates);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var cancelButton = NewButton(L.T("&Annuleren", "Cancel"), L.T("Wijzigingen niet bewaren", "Cancel preference changes"));
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.TabIndex = 1;
            var okButton = NewButton(L.T("&OK", "OK"), L.T("Voorkeuren bewaren", "Save preferences"));
            okButton.Click += OkButtonClick;
            okButton.TabIndex = 0;
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(okButton);

            Controls.Add(tabs);
            Controls.Add(buttons);
            AcceptButton = okButton;
            CancelButton = cancelButton;

            LoadValues();
            UpdateModelOptions();
            tabs.SelectedIndex = Math.Max(0, Math.Min(tabs.TabCount - 1, initialTab));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.D1)) { tabs.SelectedIndex = 0; return true; }
            if (keyData == (Keys.Control | Keys.D2)) { tabs.SelectedIndex = 1; return true; }
            if (keyData == (Keys.Control | Keys.D3)) { tabs.SelectedIndex = 2; return true; }
            if (keyData == (Keys.Control | Keys.D4)) { tabs.SelectedIndex = 3; return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void UpdateModelOptions()
        {
            var effects = settings.ModelId == MusicGenerationRequest.SoundEffectsModel;
            lengthNumeric.Enabled = !effects;
            instrumentalCheckBox.Enabled = !effects;
            detailsCheckBox.Enabled = !effects;
            var selected = Convert.ToString(formatComboBox.SelectedItem);
            formatComboBox.Items.Clear();
            formatComboBox.Items.AddRange(new object[] { L.T("WAV (PCM), 44,1 kHz", "PCM 44.1 kHz WAV"), L.T("MP3 44,1 kHz, 192 kbps", "MP3 44.1 kHz, 192 kbps"), L.T("MP3 44,1 kHz, 128 kbps", "MP3 44.1 kHz, 128 kbps") });
            if (!effects) formatComboBox.Items.AddRange(new object[] { "MP3 48 kHz, 192 kbps", "MP3 48 kHz, 240 kbps", "MP3 48 kHz, 320 kbps" });
            formatComboBox.SelectedItem = formatComboBox.Items.Contains(selected) ? selected : L.T("MP3 44,1 kHz, 192 kbps", "MP3 44.1 kHz, 192 kbps");
        }

        private void LoadValues()
        {
            outputFolderTextBox.Text = settings.DefaultOutputFolder;
            lengthNumeric.Value = settings.DefaultLengthSeconds;
            variationsNumeric.Value = settings.DefaultVariations;
            instrumentalCheckBox.Checked = settings.DefaultInstrumental;
            detailsCheckBox.Checked = settings.SaveGeneratedDetails;
            formatComboBox.SelectedItem = FormatDisplay(settings.OutputFormat);
            apiKeyTextBox.Text = AppPaths.LoadApiKey();
            if (AppPaths.ApiKeyLoadMessage.Length > 0) SetApiStatus(AppPaths.ApiKeyLoadMessage);
            updateFrequencyComboBox.SelectedIndex = Math.Max(0, Array.IndexOf(UpdateFrequencies, AppSettings.NormalizeUpdateFrequency(settings.UpdateCheckFrequency)));
            silentUpdatesCheckBox.Checked = settings.InstallUpdatesSilently;
        }

        private void OkButtonClick(object sender, EventArgs e)
        {
            string folder;
            try
            {
                folder = AppSettings.ResolveOutputFolder(outputFolderTextBox.Text, true);
                Directory.CreateDirectory(folder);
                outputFolderTextBox.Text = folder;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.T("De standaard uitvoermap kan niet worden gebruikt.", "Default output folder could not be used.") + Environment.NewLine + Environment.NewLine + ex.Message, L.T("Uitvoermap niet bruikbaar", "Could not use output folder"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabs.SelectedIndex = 0;
                outputFolderTextBox.Focus();
                return;
            }

            try { AppPaths.SaveApiKey(apiKeyTextBox.Text); }
            catch (Exception ex)
            {
                tabs.SelectedIndex = 1;
                SetApiStatus(L.T("De API-sleutel kon niet worden bewaard: ", "API key could not be saved: ") + ex.Message);
                apiKeyTextBox.Focus();
                return;
            }
            settings.DefaultOutputFolder = folder;
            settings.DefaultLengthSeconds = Convert.ToInt32(lengthNumeric.Value);
            settings.DefaultVariations = Convert.ToInt32(variationsNumeric.Value);
            settings.DefaultInstrumental = instrumentalCheckBox.Checked;
            settings.SaveGeneratedDetails = detailsCheckBox.Checked;
            settings.AutoPlayGenerations = autoPlayCheckBox.Checked;
            settings.CompletionSound = completionSoundCheckBox.Checked;
            settings.PlaybackDevice = playbackDeviceComboBox.SelectedIndex - 1;
            settings.OutputFormat = StoredFormat(Convert.ToString(formatComboBox.SelectedItem));
            settings.UpdateCheckFrequency = UpdateFrequencies[Math.Max(0, updateFrequencyComboBox.SelectedIndex)];
            settings.InstallUpdatesSilently = silentUpdatesCheckBox.Checked;
            settings.LastPreferencesTab = tabs.SelectedIndex;
            try
            {
                settings.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.T("De voorkeuren konden niet worden bewaard.", "Preferences could not be saved.") + Environment.NewLine + Environment.NewLine + ex.Message, L.T("Voorkeuren niet bewaard", "Could not save preferences"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private async void TestKeyButtonClick(object sender, EventArgs e)
        {
            var key = apiKeyTextBox.Text.Trim();
            if (key.Length == 0)
            {
                ShowApiTestResult(L.T("Typ eerst een API-sleutel.", "Enter an API key before testing it."));
                apiKeyTextBox.Focus();
                return;
            }
            var modelId = settings.ModelId;
            if (modelId == MusicGenerationRequest.SoundEffectsModel &&
                MessageBox.Show(this, L.T("Om de toegang tot geluidseffecten te testen, moet ElevenLabs een geluidje van een halve seconde maken. Dat wordt weggegooid, maar het kan tegoed kosten. Doorgaan?", "To verify Sound Effects access, ElevenLabs must generate a 0.5-second test effect. It will be discarded, but this request may spend credits. Continue?"),
                    L.T("API-sleutel testen voor geluidseffecten", "Test Sound Effects API key"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                SetApiStatus(L.T("Test voor geluidseffecten geannuleerd. Er is niets verstuurd.", "Sound Effects key test cancelled. No request was sent."));
                return;
            }
            testKeyButton.Enabled = false;
            SetApiStatus(modelId == MusicGenerationRequest.SoundEffectsModel ? L.T("Toegang tot geluidseffecten testen...", "Testing Sound Effects access...") : L.T("Toegang tot muziek testen...", "Testing Music access..."));
            try
            {
                var result = await Task.Run(delegate
                {
                    var client = new ElevenLabsMusicClient(key);
                    string modelResult;
                    try { modelResult = client.TestApiKey(modelId); }
                    catch (Exception ex) { modelResult = L.T("De test van de API-sleutel is mislukt: ", "API key test failed: ") + ex.Message; }
                    return modelResult + Environment.NewLine + client.TestBalanceAccess();
                });
                ShowApiTestResult(result);
            }
            catch (Exception ex)
            {
                ShowApiTestResult(L.T("De test van de API-sleutel is mislukt: ", "API key test failed: ") + ex.Message);
            }
            finally
            {
                testKeyButton.Enabled = true;
            }
        }

        private void ShowApiTestResult(string result)
        {
            SetApiStatus(result);
            using (var dialog = new ApiKeyTestResultForm(result)) dialog.ShowDialog(this);
        }

        private void SetApiStatus(string message)
        {
            apiStatusLabel.Text = message;
            apiStatusLabel.AccessibleName = L.T("Status van de API-sleutel: ", "API key status: ") + message;
            apiStatusLabel.NotifyNameChanged();
        }

        private void BrowseForOutputFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = L.T("Kies de standaardmap voor gemaakte geluiden.", "Choose the default folder for generated audio.");
                dialog.ShowNewFolderButton = true;
                var folder = AppSettings.NormalizeFolderInput(outputFolderTextBox.Text);
                if (Directory.Exists(folder)) dialog.SelectedPath = folder;
                if (dialog.ShowDialog(this) == DialogResult.OK) outputFolderTextBox.Text = dialog.SelectedPath;
            }
        }

        private static TableLayoutPanel NewPageLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(14) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }

        private static void AddLabeledControl(TableLayoutPanel panel, string text, Control control)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var label = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 10, 8) };
            label.UseMnemonic = true;
            label.TabStop = false;
            label.Tag = control;
            label.Click += delegate { control.Focus(); };
            panel.Controls.Add(label, 0, row);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            control.Margin = new Padding(3, 5, 3, 5);
            panel.Controls.Add(control, 1, row);
        }

        private static void AddFullWidthControl(TableLayoutPanel panel, Control control)
        {
            var row = panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Margin = new Padding(3, 8, 3, 8);
            panel.Controls.Add(control, 0, row);
            panel.SetColumnSpan(control, 2);
        }

        private static TextBox NewTextBox(string accessibleName)
        {
            return new TextBox { Dock = DockStyle.Fill, AccessibleName = accessibleName };
        }

        private static Button NewButton(string text, string accessibleDescription)
        {
            return new Button { Text = text, AutoSize = true, AccessibleDescription = accessibleDescription };
        }

        private static ComboBox NewDropDown(string accessibleName, string[] values)
        {
            var combo = new PrefixComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, AccessibleName = accessibleName };
            combo.Items.AddRange(values);
            return combo;
        }

        private static string FormatDisplay(string value)
        {
            if (value == "mp3_44100_192") return L.T("MP3 44,1 kHz, 192 kbps", "MP3 44.1 kHz, 192 kbps");
            if (value == "mp3_44100_128") return L.T("MP3 44,1 kHz, 128 kbps", "MP3 44.1 kHz, 128 kbps");
            if (value == "mp3_48000_192") return "MP3 48 kHz, 192 kbps";
            if (value == "mp3_48000_240") return "MP3 48 kHz, 240 kbps";
            if (value == "mp3_48000_320") return "MP3 48 kHz, 320 kbps";
            return L.T("WAV (PCM), 44,1 kHz", "PCM 44.1 kHz WAV");
        }

        private static string StoredFormat(string value)
        {
            if (value != null && value.Contains("48 kHz"))
            {
                if (value.Contains("320")) return "mp3_48000_320";
                if (value.Contains("240")) return "mp3_48000_240";
                return "mp3_48000_192";
            }
            if (value != null && value.Contains("192")) return "mp3_44100_192";
            if (value != null && value.Contains("128")) return "mp3_44100_128";
            return "pcm_44100";
        }

    }
}
