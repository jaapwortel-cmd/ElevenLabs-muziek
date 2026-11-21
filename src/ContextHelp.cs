using System;
using System.Collections.Generic;
using System.Windows.Forms;
namespace ElevenLabsMusicGenerator
{
    internal sealed class ContextHelp : IMessageFilter, IDisposable
    {
        private sealed class ControlHelp { public string Text; }
        internal static void SetInstructions(Control control, string text)
        {
            control.Tag = new ControlHelp { Text = text };
        }
        private readonly Form main;
        private readonly Action manual;
        private readonly Func<Control, string> describe;
        private bool disposed;
        private const int KeyDownMessage = 0x0100;
        private static Dictionary<string, string> instructions;
        private static Dictionary<string, string> Instructions
        {
            get
            {
                if (instructions != null) return instructions;
                var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Add(d, "Delen van de compositie", "Composition sections", "Kies het deel dat je wilt bewerken. De delen klinken in deze volgorde; met de knoppen voeg je delen toe, haal je ze weg of verplaats je ze.", "Choose the section to edit. Sections play in the listed order; add, remove or reorder them with the section buttons.");
            Add(d, "Naam van het deel", "Section name", "Kop van het gekozen deel, zoals Intro, Couplet of Refrein.", "Heading for the selected section, such as Intro, Verse or Chorus.");
            Add(d, "Lengte van het deel in seconden", "Section duration in seconds", "Lengte van dit deel, van 3 tot 120 seconden. Het hele plan mag niet langer zijn dan 10 minuten.", "Length of this section, from 3 to 120 seconds. Fractional seconds are accepted; the complete plan must not exceed 10 minutes.");
            Add(d, "Songtekst en korte aanwijzingen", "Lyrics and short directions", "Songtekst of korte aanwijzingen voor dit deel, een regel per keer.", "Lyrics or short performance directions for this section, one line at a time.");
            Add(d, "Stijlen die erin moeten, een per regel", "Include styles, one per line", "Stijlen die je in dit deel wilt horen. Zet elke stijl op een eigen regel.", "Styles to request for this section. Put each style on its own line.");
            Add(d, "Stijlen die er niet in mogen, een per regel", "Exclude styles, one per line", "Stijlen die je in dit deel niet wilt horen. Zet elke stijl op een eigen regel.", "Styles to avoid in this section. Put each style on its own line.");
            Add(d, "Aansluiting op de rest", "Context adherence", "Hoe goed dit deel aansluit op de muziek eromheen. Hoog geeft meer samenhang; lager geeft meer verschil.", "Choose how closely this section follows the surrounding musical context. High favors continuity; lower settings allow more contrast.");
            Add(d, "Toevoegen", "Add", "Voeg een nieuw deel toe aan het eind en kies het om te bewerken.", "Append a new section to the end of the composition and select it for editing.");
            Add(d, "Weghalen", "Remove", "Haal het gekozen deel uit het plan. Het geldt pas als je op OK drukt.", "Remove the selected section from this plan. Changes are applied when you choose OK.");
            Add(d, "Omhoog", "Move up", "Zet het gekozen deel eerder.", "Move the selected section earlier in the composition.");
            Add(d, "Omlaag", "Move down", "Zet het gekozen deel later.", "Move the selected section later in the composition.");
            Add(d, "Voorstel maken uit de prompt", "Suggest from prompt", "Laat ElevenLabs een compositieplan voorstellen op basis van de muziekbeschrijving en de lengte. Kijk de delen na voordat je muziek maakt.", "Ask ElevenLabs to suggest a composition plan using the music prompt and requested length. Review the returned sections before generating audio.");
            Add(d, "Plan of details openen", "Open plan or details", "Open een bewaard compositieplan, of haal het plan uit bewaarde details. Dit vervangt het plan dat je nu bewerkt.", "Load a saved composition plan or extract the plan from saved generation details. This replaces the plan being edited.");
            Add(d, "Plan exporteren", "Export plan", "Bewaar het compositieplan als JSON-bestand, zonder muziek te maken.", "Save the current composition plan as a reusable JSON file without generating audio.");
            Add(d, "Alleen instrumentale muziek, zonder zang", "Force instrumental music", "Vraag muziek zonder zang. Een compositieplan kan zelf zang bepalen.", "Request music without vocals. A composition plan can specify its own vocal choices.");
            Add(d, "Automatische duur van het geluidseffect", "Automatic sound effect duration", "Laat ElevenLabs zelf een goede lengte kiezen, tot 30 seconden.", "Let ElevenLabs choose a suitable sound-effect length, up to 30 seconds.");
            Add(d, "Standaardlengte in seconden", "Default length in seconds", "De lengte waarmee een nieuwe muziekprompt begint. Een compositieplan gebruikt de lengtes van zijn delen.", "Starting duration for new music prompts. A composition plan uses its own section lengths.");
            Add(d, "Standaard aantal varianten", "Default number of variations", "Hoeveel losse versies er voor een nieuwe prompt worden gemaakt, van 1 tot 10.", "Number of separate generations requested for a new prompt, from 1 to 10.");
            Add(d, "Standaard instrumentale muziek", "Default to instrumental music", "Nieuwe muziekprompts beginnen met Instrumentaal aangevinkt.", "Start new music prompts with Instrumental selected. Individual prompts can override this choice.");
            Add(d, "Songteksten en details bewaren", "Save generated lyrics and details", "Bewaar songteksten en gegevens over het maken in de submap Lyrics van de uitvoermap. Je API-sleutel staat daar nooit in.", "Save returned lyrics and reusable generation information in the output folder's Lyrics subfolder. These files do not include your API key.");
            Add(d, "Standaardformaat", "Default output format", "Het geluidsformaat waarmee nieuwe prompts beginnen.", "Starting audio format for new prompts. The main window can override it for a prompt.");
            Add(d, "API-sleutel van ElevenLabs", "ElevenLabs API key", "Een geheime sleutel waarmee dit programma je ElevenLabs-account gebruikt. Geef de sleutel toegang tot Music, Sound Effects, Text to Speech en de andere onderdelen die je wilt gebruiken, en user_read voor het tegoed. Bewaar de voorkeuren om de sleutel te houden; met API-sleutel testen kijk je wat hij mag. Deel de sleutel nooit.", "A private credential that lets this app use your ElevenLabs account. Allow Music or Sound Effects for generation and User read access for balance. Save Preferences to keep the key; Test API Key checks its access.");
            Add(d, "API-sleutel tonen", "Show API key", "Toon of verberg de sleutel. Doe dit niet als iemand meekijkt op je scherm.", "Reveal or hide the credential in Preferences. Avoid revealing it during screen sharing.");
            Add(d, "Een API-sleutel van ElevenLabs maken", "Get an ElevenLabs API key", "Opent de pagina van ElevenLabs waar je een API-sleutel maakt.", "Open the ElevenLabs API keys page in your browser to create a key for your account.");
            Add(d, "Zoeken naar updates", "Check for updates", "Hoe vaak het programma naar een nieuwe versie zoekt. Nooit zet het automatisch zoeken uit.", "Choose how often to check for a new version. Never disables automatic checks; manual checks remain available.");
            Add(d, "OK", "OK", "Bewaar de instellingen of het plan en ga terug.", "Accept the settings or plan changes and return to the previous window.");
            Add(d, "Annuleren", "Cancel", "Stop met genereren in het hoofdvenster, of gooi wijzigingen in een venster weg.", "Cancel the current generation in the main window, or discard unaccepted edits in a dialog.");
            Add(d, "Sluiten", "Close", "Sluit dit venster en ga terug.", "Close this dialog and return to the previous window.");
            Add(d, "Handleiding openen", "Open manual", "Opent de hele handleiding in je browser.", "Open the complete application guide in your browser.");
            Add(d, "Model", "Model", "Kies het model. Wat kan, hangt af van het model en je abonnement.", "Choose the generation model. Available options and limits depend on the selected mode and your account.");
            Add(d, "Muziekbeschrijving", "Music prompt", "Beschrijf de muziek die je wilt. De stijl werkt het best in het Engels; een songtekst mag in het Nederlands. Met een compositieplan bepaal je de delen precies.", "Describe the music you want. A composition plan gives more detailed control over its sections.");
            Add(d, "Beschrijving van het geluidseffect", "Sound effects prompt", "Beschrijf het geluid dat je wilt. Met Automatische duur kiest ElevenLabs de lengte.", "Describe the sound you want. Automatic duration lets the service choose its length.");
            Add(d, "Statusberichten", "Status log", "Toont de voortgang en fouten, het nieuwste onderaan.", "Shows generation progress and errors, newest last.");
            Add(d, "Tegoed", "Credit balance", "Toont je tegoed, wat je hebt gebruikt en wanneer het tegoed weer aangevuld wordt. F5 vernieuwt.", "Shows the last reported included allowance, usage and reset. Refresh balance obtains current account information; this does not control overage billing.");
            Add(d, "Basisbestandsnaam", "Base filename", "Naam voor de bestanden. Laat leeg om de titels van ElevenLabs te gebruiken. Bestaande opnames worden nooit overschreven.", "Name for the output files. Leave blank for returned music titles with numbered prefixes, or a suggested name for sounds. Your typed names keep spaces and receive variation suffixes. Existing recordings are preserved.");
            Add(d, "Aantal varianten", "Number of variations", "Hoeveel losse versies er worden gemaakt.", "Number of separate generations requested.");
            Add(d, "Standaard uitvoermap", "Default output folder", "De map waar muziek, geluidseffecten en spraak vanzelf worden bewaard.", "Choose the folder where completed music and sound effects are saved automatically.");
            Add(d, "Soorten voorkeuren", "Preference categories", "Kies een tabblad met Ctrl+Tab of de pijltjes, en ga dan met Tab naar de instellingen. Geluid bevat de geluidskaart.", "Choose a settings tab, then move to its controls. Audio contains the playback device and optional sequential playback.");
            Add(d, "Gecontroleerde updates stil installeren", "Install verified updates silently", "Installeer gecontroleerde updates zonder te vragen. Standaard uit.", "Install verified updates found by automatic checks without asking. Off by default. Settings, prompts and generated music are preserved.");
            Add(d, "Lengte in seconden", "Length in seconds", "De gevraagde lengte. Bij een compositieplan bepalen de delen de lengte; bij geluidseffecten kan Automatische duur de lengte kiezen.", "Requested duration. In music, a composition plan supplies section lengths; for sounds, automatic duration chooses the length.");
            Add(d, "Invloed van de prompt", "Prompt influence", "Hoe precies het geluidseffect de beschrijving volgt, van nul tot een.", "Controls how closely a sound effect follows the description, from zero to one.");
            Add(d, "Geluidskaart voor afspelen", "Playback device", "Kies waar het geluid van dit programma uit komt. Standaard van Windows volgt Windows.", "Choose the audio output used by this app. System default follows Windows.");
            Add(d, "Geluidje als het klaar is", "Play a completion sound", "Een kort geluidje als het maken klaar is.", "Play a short signal when generation finishes. Automatic audio playback replaces this signal when enabled.");
            Add(d, "Nieuwe geluiden vanzelf na elkaar afspelen", "Play new generations automatically in sequence", "Speel nieuwe geluiden vanzelf af, een voor een. Standaard uit. Escape stopt.", "Off by default. Play only the new audio from a successfully completed batch, one file at a time. Stop ends the queue; cancelled or failed batches do not start automatically.");
            Add(d, "Gemaakte geluiden", "Generated audio", "Kies een gemaakt geluid. Enter speelt af; Escape stopt.", "Choose a saved result. Enter plays it; Escape stops the current audio and the rest of the queue.");
            Add(d, "Genereren", "Generate", "Je krijgt eerst een overzicht om te bevestigen. Daarna gaat je prompt of plan naar ElevenLabs en kost het tegoed.", "Review and confirm the music or sound-effects request. New generations send your prompt or plan to ElevenLabs and spend credits.");
            Add(d, "Afspelen", "Play", "Speel de gekozen opname af.", "Play the selected completed recording.");
            Add(d, "Stoppen", "Stop", "Stop het afspelen.", "Stop audio playback.");
            Add(d, "Compositieplan gebruiken", "Use composition plan", "Gebruik de delen, stijlen en songteksten uit het compositieplan in plaats van een beschrijving.", "Use section lengths, styles and lyrics from the composition plan instead of a single music description.");
            Add(d, "Geluidseffect herhalen (loop)", "Loop sound effect", "Vraag een geluidseffect dat naadloos herhaald kan worden.", "Request a sound effect suitable for looping.");
            Add(d, "API-sleutel testen", "Test API key", "Test de toegang en het tegoed. Voor muziek wordt niets gemaakt; voor geluidseffecten wordt eerst gevraagd, want die test kost een beetje tegoed.", "Test the current mode and balance permissions. Music testing does not generate audio; a Sound Effects test asks before generating a short paid sound.");
            Add(d, "Functie", "Function", "Kies wat je wilt doen: tekst naar spraak, gesprek, stemmen zoeken, stem ontwerpen, klonen of omzetten, achtergrondgeluid weghalen, spraak naar tekst, nasynchroniseren, tekst uitlijnen, uitspraaklijsten of geschiedenis. Het deel eronder past zich aan. Ctrl+PageDown en Ctrl+PageUp wisselen ook.", "Choose what you want to do. The part below changes to match. Ctrl+PageDown and Ctrl+PageUp also switch.");
            Add(d, "Meldingen", "Messages", "Hier staat wat het programma doet en of iets gelukt is. De schermlezer leest nieuwe meldingen vanzelf voor.", "Shows what the program is doing and whether it worked. Screen readers announce new messages automatically.");
                instructions = d;
                return d;
            }
        }

        private static void Add(Dictionary<string, string> d, string nlName, string enName, string nlText, string enText)
        {
            d[L.T(nlName, enName)] = L.T(nlText, enText);
        }
        private ContextHelp(Form main, Action manual, Func<Control, string> describe)
        {
            this.main = main; this.manual = manual; this.describe = describe;
            Application.AddMessageFilter(this);
            main.Disposed += delegate { Dispose(); };
        }
        public static void Attach(Form main, Action manual, Func<Control, string> describe = null)
        {
            new ContextHelp(main, manual, describe ?? Description);
        }
        public bool PreFilterMessage(ref Message message)
        {
            if (disposed || message.Msg != KeyDownMessage || (Keys)message.WParam.ToInt32() != Keys.F1 || Control.ModifierKeys != Keys.None) return false;
            var control = Control.FromChildHandle(message.HWnd);
            var owner = control == null ? null : control.FindForm();
            for (var form = owner; form != null; form = form.Owner)
            {
                if (form != main) continue;
                Show(owner, manual, describe);
                return true;
            }
            return false;
        }
        internal static Control FocusedControl(Control parent)
        {
            foreach (Control child in parent.Controls)
                if (child.ContainsFocus) return FocusedControl(child);
            return parent;
        }
        internal static string ControlName(Control control)
        {
            for (var current = control; current != null && !(current is Form); current = current.Parent)
            {
                if (!string.IsNullOrEmpty(current.AccessibleName)) return current.AccessibleName;
                if (current is ButtonBase || current is LinkLabel) return current.Text.Replace("&", "").Trim().TrimEnd('.', ':');
            }
            return L.T("Huidig venster", "Current window");
        }
        internal static string Description(Control control)
        {
            var local = control.Tag as ControlHelp;
            if (local != null) return local.Text;
            string text;
            if (Instructions.TryGetValue(ControlName(control), out text)) return text;
            for (var current = control; current != null && !(current is Form); current = current.Parent)
                if (!string.IsNullOrEmpty(current.AccessibleDescription)) return current.AccessibleDescription;
            return L.T("Er is geen extra uitleg voor ", "No additional description is available for ") + ControlName(control) + ".";
        }
        internal static void Show(Form owner, Action manual, Func<Control, string> describe = null)
        {
            if (owner == null || (owner.Text.StartsWith("Help: ", StringComparison.Ordinal) || owner.Text.StartsWith("Hulp: ", StringComparison.Ordinal))) return;
            var focused = FocusedControl(owner);
            var title = L.T("Hulp: ", "Help: ") + ControlName(focused);
            using (var dialog = new ApiKeyTestResultForm((describe ?? Description)(focused), title, manual))
                dialog.ShowDialog(owner);
            if (!focused.IsDisposed && focused.CanFocus) focused.Focus();
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Application.RemoveMessageFilter(this);
        }
    }
}
