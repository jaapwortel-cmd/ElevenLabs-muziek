using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ElevenLabs Muziek en Spraak")]
[assembly: AssemblyDescription("Toegankelijk programma voor muziek, geluidseffecten en spraak met ElevenLabs (Nederlandse versie)")]
[assembly: AssemblyCompany("Andre Louis en Jaap Wortel")]
[assembly: AssemblyProduct("ElevenLabs Muziek en Spraak")]
[assembly: AssemblyCopyright("Copyright 2026 Andre Louis; Nederlandse versie en spraakfuncties 2026 Jaap Wortel")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion(ElevenLabsMusicGenerator.AppVersion.Full)]
[assembly: AssemblyFileVersion(ElevenLabsMusicGenerator.AppVersion.Full)]

namespace ElevenLabsMusicGenerator
{
    internal static class AppVersion
    {
        public const string Short = "1.5.1.1";
        public const string Full = Short;
    }
}
