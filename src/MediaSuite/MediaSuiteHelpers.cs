// Omgezet naar C# uit "ElevenLabs Media Suite" (https://github.com/audioses/elevenlabs-media-suite,
// MIT-licentie, Copyright (c) 2026 ElevenLabs Media Suite contributors):
// src/api/models.py, src/config/naming.py, src/api/forced_alignment.py (to_srt) en src/workers/job_queue.py.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ElevenLabsMusicGenerator.MediaSuite
{
    /// <summary>Modellen per functie (models.py). "latest (recommended)" of leeg = het aanbevolen model.</summary>
    internal static class MediaSuiteModels
    {
        public const string LatestLabel = "latest (recommended)";
        /// <summary>De openbare stem "Rachel", als er nog geen stem gekozen is.</summary>
        public const string DefaultVoiceId = "21m00Tcm4TlvDq8ikWAM";

        public static readonly Dictionary<string, string> Latest = new Dictionary<string, string>
        {
            { "tts", "eleven_v3" }, { "dialogue", "eleven_v3" }, { "stt", "scribe_v2" },
            { "sts", "eleven_multilingual_sts_v2" }, { "ttv", "eleven_ttv_v3" }
        };

        public static readonly Dictionary<string, string[]> Choices = new Dictionary<string, string[]>
        {
            { "tts", new[] { "eleven_v3", "eleven_multilingual_v2", "eleven_turbo_v2_5", "eleven_flash_v2_5" } },
            { "dialogue", new[] { "eleven_v3" } },
            { "stt", new[] { "scribe_v2" } },
            { "sts", new[] { "eleven_multilingual_sts_v2", "eleven_english_sts_v2" } },
            { "ttv", new[] { "eleven_ttv_v3", "eleven_multilingual_ttv_v2" } }
        };

        /// <summary>Uitvoerformaten (tts.py OUTPUT_FORMATS).</summary>
        public static readonly Dictionary<string, string> OutputFormats = new Dictionary<string, string>
        {
            { "mp3", "mp3_44100_128" }, { "mp3_high", "mp3_44100_192" }, { "wav", "pcm_44100" }, { "ulaw", "ulaw_8000" }
        };

        /// <summary>Items voor een keuzelijst, met de aanbevolen keuze bovenaan.</summary>
        public static string[] SelectorChoices(string feature)
        {
            string[] ids;
            Choices.TryGetValue(feature, out ids);
            return new[] { LatestLabel }.Concat(ids ?? new string[0]).ToArray();
        }

        public static string Resolve(string feature, string selected)
        {
            if (string.IsNullOrWhiteSpace(selected) || selected.IndexOf(LatestLabel, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string latest;
                return Latest.TryGetValue(feature, out latest) ? latest : string.Empty;
            }
            return selected;
        }
    }

    /// <summary>
    /// Duidelijke bestandsnamen (naming.py): "regen_op_dak-001-a1b2c3.mp3" uit de eerste drie woorden,
    /// met een .json ernaast waarin staat hoe het gemaakt is.
    /// </summary>
    internal static class AssetNaming
    {
        public static readonly string[] Categories = { "Spraak", "Gesprekken", "Stem omzetten", "Schoon geluid", "Stemvoorbeelden", "Nagesynchroniseerd", "Geschiedenis", "Teksten" };

        public static string Slugify(string prompt, int maxWords = 3)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return "zonder_naam";
            var decomposed = prompt.Normalize(NormalizationForm.FormKD);
            var ascii = new StringBuilder();
            foreach (var c in decomposed) if (c < 128) ascii.Append(c);
            var cleaned = Regex.Replace(ascii.ToString().ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();
            if (cleaned.Length == 0) return "zonder_naam";
            var slug = string.Join("_", cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(maxWords));
            if (slug.Length > 48) slug = slug.Substring(0, 48);
            return slug.Length == 0 ? "zonder_naam" : slug;
        }

        public static int NextCounter(string directory, string slug)
        {
            if (!Directory.Exists(directory)) return 1;
            var pattern = new Regex("^" + Regex.Escape(slug) + @"-(\d+)-[0-9a-f]{6}\.[A-Za-z0-9]+$");
            var highest = 0;
            try
            {
                foreach (var path in Directory.EnumerateFiles(directory))
                {
                    var match = pattern.Match(Path.GetFileName(path));
                    int number;
                    if (match.Success && int.TryParse(match.Groups[1].Value, out number)) highest = Math.Max(highest, number);
                }
            }
            catch (IOException) { return 1; }
            catch (UnauthorizedAccessException) { return 1; }
            return highest + 1;
        }

        public static string BuildFileName(string slug, int counter, string suffix)
        {
            if (!suffix.StartsWith(".", StringComparison.Ordinal)) suffix = "." + suffix;
            return slug + "-" + counter.ToString("000", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + suffix;
        }

        /// <summary>Kiest een nieuw pad in rootFolder\category. Schrijft de .json als meta niet null is (het geluid zelf niet).</summary>
        public static string NextAssetPath(string rootFolder, string category, string prompt, string suffix, IDictionary<string, object> meta, out string metaPath)
        {
            if (!Categories.Contains(category)) throw new ArgumentException("Onbekende map: " + category);
            var directory = Path.Combine(rootFolder, category);
            Directory.CreateDirectory(directory);
            var slug = Slugify(prompt);
            var audioPath = Path.Combine(directory, BuildFileName(slug, NextCounter(directory, slug), suffix));
            metaPath = Path.ChangeExtension(audioPath, ".json");
            if (meta != null) WriteSidecar(metaPath, meta);
            return audioPath;
        }

        /// <summary>Schrijft de .json. Gooit nooit een fout: het geluid mag niet verloren gaan om de beschrijving.</summary>
        public static void WriteSidecar(string metaPath, IDictionary<string, object> data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(metaPath));
                File.WriteAllText(metaPath, MediaSuiteClient.Json.Serialize(data) + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (Exception) { }
        }
    }

    /// <summary>Ondertitels (SRT) maken uit het antwoord van ForcedAlignment of SpeechToText (to_srt).</summary>
    internal static class Subtitles
    {
        public static string ToSrt(IDictionary<string, object> alignment)
        {
            var words = MediaSuiteClient.List(alignment, "words");
            if (words.Count == 0) words = MediaSuiteClient.List(alignment, "characters");
            if (words.Count == 0) return string.Empty;
            var output = new StringBuilder();
            var current = new List<Dictionary<string, object>>();
            var counter = 1;
            Action flush = () =>
            {
                if (current.Count == 0) return;
                var start = MediaSuiteClient.Num(current[0], "start");
                var end = MediaSuiteClient.Num(current[current.Count - 1], "end", start + 1.0);
                var text = string.Concat(current.Select(w => MediaSuiteClient.Str(w, "text") ?? string.Empty)).Trim();
                output.Append(counter++).Append("\r\n").Append(Time(start)).Append(" --> ").Append(Time(end)).Append("\r\n").Append(text).Append("\r\n\r\n");
                current.Clear();
            };
            foreach (var word in words)
            {
                current.Add(word);
                var text = string.Concat(current.Select(w => MediaSuiteClient.Str(w, "text") ?? string.Empty));
                if (text.Length > 60 || text.EndsWith(".", StringComparison.Ordinal) || text.EndsWith("!", StringComparison.Ordinal) || text.EndsWith("?", StringComparison.Ordinal)) flush();
            }
            flush();
            return output.ToString();
        }

        private static string Time(double seconds)
        {
            var span = TimeSpan.FromMilliseconds(Math.Floor(Math.Max(0, seconds) * 1000));
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00},{3:000}", (int)span.TotalHours, span.Minutes, span.Seconds, span.Milliseconds);
        }
    }

    /// <summary>
    /// Lange klussen op de achtergrond, een voor een (job_queue.py), zodat het venster en de schermlezer
    /// blijven reageren. Klaar/fout wordt via de SynchronizationContext van het venster gemeld.
    /// </summary>
    internal sealed class JobQueue : IDisposable
    {
        private readonly SynchronizationContext ui;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private CancellationTokenSource cancel = new CancellationTokenSource();
        private int pending;

        public JobQueue() { ui = SynchronizationContext.Current ?? new SynchronizationContext(); }

        public int Pending { get { return pending; } }

        public void Submit<T>(string name, Func<CancellationToken, T> work, Action<T> done, Action<Exception> failed)
        {
            Interlocked.Increment(ref pending);
            var token = cancel.Token;
            Task.Run(async () =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    var result = work(token);
                    if (done != null) ui.Post(_ => done(result), null);
                }
                catch (Exception ex)
                {
                    if (failed != null) ui.Post(_ => failed(ex), null);
                }
                finally
                {
                    gate.Release();
                    Interlocked.Decrement(ref pending);
                }
            });
        }

        /// <summary>Stopt alle klussen die nog bezig zijn of wachten.</summary>
        public void CancelAll()
        {
            var old = cancel;
            cancel = new CancellationTokenSource();
            old.Cancel();
        }

        public void Dispose() { cancel.Cancel(); }
    }
}
