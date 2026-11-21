using System;

namespace ElevenLabsMusicGenerator
{
    internal enum SoundtrackStage { Preparing, Uploading, Waiting, Downloading, Saving }

    internal sealed class SoundtrackProgress
    {
        public readonly SoundtrackStage Stage;
        public readonly long Bytes;
        public readonly long TotalBytes;
        public SoundtrackProgress(SoundtrackStage stage, long bytes = 0, long totalBytes = 0)
        {
            Stage = stage; Bytes = Math.Max(0, bytes); TotalBytes = Math.Max(0, totalBytes);
        }
        public string Message
        {
            get
            {
                switch (Stage)
                {
                    case SoundtrackStage.Uploading: return L.T("Video's opsturen.", "Uploading videos.");
                    case SoundtrackStage.Waiting: return L.T("Opgestuurd. Wachten tot ElevenLabs de muziek heeft gemaakt. Dat kan een paar minuten duren, ook bij een korte video. Er is geen percentage of resterende tijd bekend.", "Upload complete. Waiting for ElevenLabs to generate the soundtrack. This can take several minutes, even for a short video. No generation percentage or time remaining is available.");
                    case SoundtrackStage.Downloading: return L.T("Muziek ontvangen van ElevenLabs.", "Receiving soundtrack from ElevenLabs.");
                    case SoundtrackStage.Saving: return L.T("Opnames bewaren.", "Saving soundtrack recordings.");
                    default: return L.T("Opsturen voorbereiden.", "Preparing video upload.");
                }
            }
        }
        public int? Percent
        {
            get { return TotalBytes > 0 && (Stage == SoundtrackStage.Uploading || Stage == SoundtrackStage.Downloading) ? (int?)Math.Min(100, 100.0 * Bytes / TotalBytes) : null; }
        }
        public string Describe(TimeSpan elapsed)
        {
            var text = Message + Environment.NewLine + "Elapsed: " + ElapsedText(elapsed) + ".";
            if (Stage == SoundtrackStage.Uploading || Stage == SoundtrackStage.Downloading)
            {
                text += Environment.NewLine + (Stage == SoundtrackStage.Uploading ? "Sent: " : "Received: ") + Bytes.ToString("N0") + " bytes";
                if (TotalBytes > 0) text += L.T(" van ", " of ") + TotalBytes.ToString("N0") + " (" + Percent + L.T(" procent)", " percent)");
                text += ".";
            }
            return text;
        }
        internal static string ElapsedText(TimeSpan elapsed)
        {
            var total = Math.Max(0, (long)elapsed.TotalSeconds);
            var minutes = total / 60; var seconds = total % 60;
            return (minutes > 0 ? minutes + (minutes == 1 ? L.T(" minuut, ", " minute, ") : L.T(" minuten, ", " minutes, ")) : "") + seconds + (seconds == 1 ? L.T(" seconde", " second") : L.T(" seconden", " seconds"));
        }
    }
}
