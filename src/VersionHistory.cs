using System;
using System.Drawing;
using System.Windows.Forms;

namespace ElevenLabsMusicGenerator
{
    internal static class VersionHistory
    {
        private static readonly string N = Environment.NewLine;

        internal static string Text
        {
            get
            {
                return L.T(
                    "1.5.1.1 (Nederlandse versie van Jaap Wortel)" + N + N +
                    "Het programma is nu helemaal in het Nederlands. Het Engels blijft beschikbaar: menu Opties, Taal (Language)." + N +
                    "Nieuw: het venster Spraak en stemmen (Ctrl+T), met de functies van ElevenLabs Media Suite: tekst naar spraak, gesprek met meerdere stemmen, " +
                    "stemmen zoeken, stem ontwerpen, stem klonen, stem omzetten, achtergrondgeluid weghalen, spraak naar tekst, nasynchroniseren, " +
                    "tekst uitlijnen op een opname, uitspraaklijsten en geschiedenis." + N + N +
                    "1.5.1 (Andre Louis)" + N + N +
                    "Geluidseffecten gebruiken nu ElevenLabs SFX v3, voordat v2 op 1 februari 2027 stopt. Bewaarde prompts voor geluidseffecten blijven werken." + N + N +
                    "1.5.0 (Andre Louis)" + N + N +
                    "Nieuw: Muziek bij een video (Ctrl+Shift+V): video's achter elkaar zetten, muziek en geluidseffecten laten maken, het originele geluid als voorbeeld gebruiken, en een gemengde geluidsband of losse sporen bewaren." + N +
                    "De voortgang toont opsturen, wachten en bewaren met de verstreken tijd. F5 toont de voortgang in een venster." + N + N +
                    "1.4.4 (Andre Louis)" + N + N +
                    "In keuzelijsten kun je nu meerdere letters typen om te zoeken, ook met spaties en cijfers.",
                    "1.5.1.1 (Dutch version by Jaap Wortel)" + N + N +
                    "The program is now fully available in Dutch, which is the default language. English remains available: Options menu, Language (Taal)." + N +
                    "New: the Speech and voices window (Ctrl+T), with the features of ElevenLabs Media Suite: text to speech, dialogue with multiple voices, " +
                    "voice search, voice design, voice cloning, voice changer, background noise removal, speech to text, dubbing, " +
                    "forced alignment, pronunciation dictionaries and history." + N + N +
                    "1.5.1" + N + N +
                    "Sound Effects now uses ElevenLabs SFX v3 ahead of the v2 retirement on 1 February 2027. Saved sound-effects prompts and your selected mode continue to work, with duration, looping and prompt influence preserved. Music and Video Soundtrack are unchanged." + N + N +
                    "1.5.0" + N + N +
                    "Added Video Soundtrack: combine ordered videos, request music and sound effects, use original audio as a reference, and save a mixed soundtrack or separate tracks. Open it from Generate with Ctrl+Shift+V." + N +
                    "Soundtrack progress shows upload, waiting and saving stages with elapsed time. F5 opens a stable, readable progress snapshot; generation can take several minutes even for a short video." + N +
                    "Generated audio is grouped with Play and Stop. Tab from the list reaches Play, then Stop; Shift+Tab from the prompt reaches the status log." + N + N +
                    "1.4.4" + N + N +
                    "Dropdowns now accept multi-letter typing navigation, including spaces and digits, throughout the main window, Preferences and composition-plan editor.");
            }
        }

        internal static ApiKeyTestResultForm CreateDialog()
        {
            return new ApiKeyTestResultForm(Text, L.T("Versiegeschiedenis", "Version history")) { Size = new Size(720, 480) };
        }

        internal static void Show(IWin32Window owner)
        {
            using (var dialog = CreateDialog()) dialog.ShowDialog(owner);
        }
    }
}
