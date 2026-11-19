// Omgezet naar C# uit "ElevenLabs Media Suite" (https://github.com/audioses/elevenlabs-media-suite,
// MIT-licentie, Copyright (c) 2026 ElevenLabs Media Suite contributors), src/api/*.py.
// Muziek en geluidseffecten zitten al in Andres ElevenLabsMusicClient en zijn hier daarom niet overgenomen.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace ElevenLabsMusicGenerator.MediaSuite
{
    /// <summary>Stem-instellingen (stability, similarity_boost, style, use_speaker_boost, speed).</summary>
    internal sealed class VoiceSettings
    {
        public double? Stability;
        public double? SimilarityBoost;
        public double? Style;
        public bool? UseSpeakerBoost;
        public double? Speed;

        public Dictionary<string, object> ToDictionary()
        {
            var data = new Dictionary<string, object>();
            MediaSuiteClient.Put(data, "stability", Stability);
            MediaSuiteClient.Put(data, "similarity_boost", SimilarityBoost);
            MediaSuiteClient.Put(data, "style", Style);
            MediaSuiteClient.Put(data, "use_speaker_boost", UseSpeakerBoost);
            MediaSuiteClient.Put(data, "speed", Speed);
            return data;
        }
    }

    /// <summary>Verwijzing naar een uitspraaklijst (id + version_id). Hooguit drie per verzoek.</summary>
    internal sealed class DictionaryLocator
    {
        public string Id;
        public string VersionId;
        public Dictionary<string, object> ToDictionary()
        {
            var data = new Dictionary<string, object> { { "pronunciation_dictionary_id", Id } };
            MediaSuiteClient.Put(data, "version_id", VersionId);
            return data;
        }
    }

    internal sealed class TextToSpeechRequest
    {
        public string Text;
        public string VoiceId = MediaSuiteModels.DefaultVoiceId;
        public string ModelId;
        public string OutputFormat = "mp3_44100_128";
        public VoiceSettings Settings;
        public string LanguageCode;
        public long? Seed;
        public List<DictionaryLocator> Dictionaries = new List<DictionaryLocator>();
        public string PreviousText;
        public string NextText;
        public List<string> PreviousRequestIds = new List<string>();
        public List<string> NextRequestIds = new List<string>();
        /// <summary>"auto", "on" of "off".</summary>
        public string ApplyTextNormalization;
        public bool? ApplyLanguageTextNormalization;
        public bool? EnableLogging;
        public int? OptimizeStreamingLatency;
    }

    /// <summary>Een regel in een gesprek: wie zegt wat.</summary>
    internal sealed class DialogueLine
    {
        public string Text;
        public string VoiceId;
        public DialogueLine(string text, string voiceId) { Text = text; VoiceId = voiceId; }
    }

    internal sealed class DialogueRequest
    {
        public List<DialogueLine> Lines = new List<DialogueLine>();
        public string ModelId;
        public string OutputFormat = "mp3_44100_128";
        public string LanguageCode;
        /// <summary>Bijvoorbeeld stability voor eleven_v3.</summary>
        public Dictionary<string, object> Settings;
        public List<DictionaryLocator> Dictionaries = new List<DictionaryLocator>();
        public long? Seed;
        public string ApplyTextNormalization;
    }

    internal sealed class SpeechToSpeechRequest
    {
        public string AudioPath;
        public string VoiceId;
        public string ModelId;
        public string OutputFormat = "mp3_44100_128";
        public VoiceSettings Settings;
        public long? Seed;
        public bool? RemoveBackgroundNoise;
        /// <summary>"pcm_s16le_16" of leeg (gewoon bestand).</summary>
        public string FileFormat;
        public bool? EnableLogging;
        public int? OptimizeStreamingLatency;
    }

    internal sealed class SpeechToTextRequest
    {
        public string AudioPath;
        /// <summary>Openbaar adres in plaats van een bestand (cloud_storage_url).</summary>
        public string CloudStorageUrl;
        public string ModelId;
        public string LanguageCode;
        public bool TagAudioEvents = true;
        public int? NumSpeakers;
        /// <summary>"none", "word" of "character".</summary>
        public string TimestampsGranularity;
        public bool? Diarize;
        public double? DiarizationThreshold;
        /// <summary>Extra uitvoer, bijvoorbeeld {"format":"srt"}; wordt als JSON-tekst meegestuurd.</summary>
        public List<Dictionary<string, object>> AdditionalFormats = new List<Dictionary<string, object>>();
        public string FileFormat;
        public double? Temperature;
        public long? Seed;
        public bool? UseMultiChannel;
        public bool? EnableLogging;
        public bool? Webhook;
        public string WebhookId;
    }

    internal sealed class VoiceDesignRequest
    {
        public string VoiceDescription;
        public string Text;
        public string ModelId = "eleven_ttv_v3";
        public string OutputFormat = "mp3_44100_192";
        public bool? AutoGenerateText;
        public double? Loudness;
        public long? Seed;
        public double? GuidanceScale;
        public bool? ShouldEnhance;
        public double? Quality;
        public string ReferenceAudioBase64;
        public double? PromptStrength;
        public string RemixingSessionId;
        public string RemixingSessionIterationId;
    }

    internal sealed class VoicePreview
    {
        public string GeneratedVoiceId;
        public byte[] Audio;
        public string MediaType;
        public double DurationSeconds;
        public string Language;
    }

    internal sealed class VoiceInfo
    {
        public string VoiceId;
        public string Name;
        public string Category;
        public string Description;
        public string PreviewUrl;
        public Dictionary<string, object> Labels;
        public Dictionary<string, object> Raw;
        public override string ToString() { return string.IsNullOrEmpty(Category) ? Name : Name + " (" + Category + ")"; }
    }

    internal sealed class SharedVoicesPage
    {
        public List<VoiceInfo> Voices = new List<VoiceInfo>();
        public int TotalCount;
        public bool HasMore;
        public int Page;
        public int PageSize;
    }

    internal sealed class SharedVoiceQuery
    {
        public int Page;
        public int PageSize = 30;
        public string Search;
        public string Sort;
        public string Category;
        public string Gender;
        public string Age;
        public string Accent;
        public string Language;
        public List<string> UseCases = new List<string>();
        public List<string> Descriptives = new List<string>();
        public bool? Featured;
    }

    internal sealed class DubbingRequest
    {
        public string FilePath;
        public string SourceUrl;
        public string SourceLanguage = "auto";
        public string TargetLanguage;
        public string Name;
        public int? NumSpeakers;
        public bool? Watermark;
        public int? StartTime;
        public int? EndTime;
        public bool? HighestResolution;
        public bool? DropBackgroundAudio;
        public bool? UseProfanityFilter;
        public bool? DubbingStudio;
        public bool? DisableVoiceCloning;
        /// <summary>"automatic" of "manual".</summary>
        public string Mode;
        public string TargetAccent;
        public string CsvFilePath;
        public double? CsvFps;
        public string ForegroundAudioPath;
        public string BackgroundAudioPath;
    }

    /// <summary>Een regel voor een uitspraaklijst: alias ("tomato" = "toe-may-toe") of phoneme (IPA/CMU).</summary>
    internal sealed class PronunciationRule
    {
        public string StringToReplace;
        public string Alias;
        public string Phoneme;
        public string Alphabet = "ipa";

        public Dictionary<string, object> ToDictionary()
        {
            if (!string.IsNullOrEmpty(Phoneme))
                return new Dictionary<string, object> { { "type", "phoneme" }, { "string_to_replace", StringToReplace }, { "phoneme", Phoneme }, { "alphabet", Alphabet } };
            return new Dictionary<string, object> { { "type", "alias" }, { "string_to_replace", StringToReplace }, { "alias", Alias ?? string.Empty } };
        }
    }

    internal sealed class HistoryPage
    {
        public List<Dictionary<string, object>> Items = new List<Dictionary<string, object>>();
        public bool HasMore;
        public string LastHistoryItemId;
    }

    internal sealed partial class MediaSuiteClient
    {
        // ------------------------------------------------------------------ Tekst naar spraak (tts.py)

        public AudioResult TextToSpeech(TextToSpeechRequest r, CancellationToken cancel = default(CancellationToken))
        {
            if (r == null || string.IsNullOrWhiteSpace(r.Text)) throw new ArgumentException("Text is required.");
            var body = new Dictionary<string, object> { { "text", r.Text }, { "model_id", MediaSuiteModels.Resolve("tts", r.ModelId) } };
            if (r.Settings != null) body["voice_settings"] = r.Settings.ToDictionary();
            Put(body, "language_code", r.LanguageCode);
            Put(body, "seed", r.Seed);
            if (r.Dictionaries.Count > 0) body["pronunciation_dictionary_locators"] = Locators(r.Dictionaries);
            Put(body, "previous_text", r.PreviousText);
            Put(body, "next_text", r.NextText);
            if (r.PreviousRequestIds.Count > 0) body["previous_request_ids"] = r.PreviousRequestIds.ToArray();
            if (r.NextRequestIds.Count > 0) body["next_request_ids"] = r.NextRequestIds.ToArray();
            Put(body, "apply_text_normalization", r.ApplyTextNormalization);
            Put(body, "apply_language_text_normalization", r.ApplyLanguageTextNormalization);
            var query = new Dictionary<string, object> { { "output_format", r.OutputFormat } };
            Put(query, "enable_logging", r.EnableLogging);
            Put(query, "optimize_streaming_latency", r.OptimizeStreamingLatency);
            var voice = string.IsNullOrEmpty(r.VoiceId) ? MediaSuiteModels.DefaultVoiceId : r.VoiceId;
            return SendAudio("POST", "v1/text-to-speech/" + Uri.EscapeDataString(voice), query, body, null, null, cancel);
        }

        // ------------------------------------------------------------------ Gesprek met meerdere stemmen (dialogue.py)

        public AudioResult TextToDialogue(DialogueRequest r, CancellationToken cancel = default(CancellationToken))
        {
            if (r == null || r.Lines.Count == 0) throw new ArgumentException("The dialogue must contain at least one line.");
            var inputs = new List<object>();
            for (var i = 0; i < r.Lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(r.Lines[i].Text)) throw new ArgumentException("Line " + (i + 1) + " has no text.");
                if (string.IsNullOrWhiteSpace(r.Lines[i].VoiceId)) throw new ArgumentException("Line " + (i + 1) + " has no voice.");
                inputs.Add(new Dictionary<string, object> { { "text", r.Lines[i].Text }, { "voice_id", r.Lines[i].VoiceId } });
            }
            var body = new Dictionary<string, object> { { "inputs", inputs }, { "model_id", MediaSuiteModels.Resolve("dialogue", r.ModelId) } };
            Put(body, "language_code", r.LanguageCode);
            if (r.Settings != null && r.Settings.Count > 0) body["settings"] = r.Settings;
            if (r.Dictionaries.Count > 0) body["pronunciation_dictionary_locators"] = Locators(r.Dictionaries);
            Put(body, "seed", r.Seed);
            Put(body, "apply_text_normalization", r.ApplyTextNormalization);
            return SendAudio("POST", "v1/text-to-dialogue", new Dictionary<string, object> { { "output_format", r.OutputFormat } }, body, null, null, cancel);
        }

        // ------------------------------------------------------------------ Stem omzetten (speech_to_speech.py)

        public AudioResult SpeechToSpeech(SpeechToSpeechRequest r, CancellationToken cancel = default(CancellationToken))
        {
            RequireFile(r == null ? null : r.AudioPath);
            if (string.IsNullOrWhiteSpace(r.VoiceId)) throw new ArgumentException("A target voice is required.");
            var form = new Dictionary<string, object> { { "model_id", MediaSuiteModels.Resolve("sts", r.ModelId) } };
            if (r.Settings != null) form["voice_settings"] = Json.Serialize(r.Settings.ToDictionary());
            Put(form, "seed", r.Seed);
            Put(form, "remove_background_noise", r.RemoveBackgroundNoise);
            Put(form, "file_format", r.FileFormat);
            var query = new Dictionary<string, object> { { "output_format", r.OutputFormat } };
            Put(query, "enable_logging", r.EnableLogging);
            Put(query, "optimize_streaming_latency", r.OptimizeStreamingLatency);
            return SendAudio("POST", "v1/speech-to-speech/" + Uri.EscapeDataString(r.VoiceId), query, null, form,
                new[] { new UploadFile("audio", r.AudioPath) }, cancel);
        }

        // ------------------------------------------------------------------ Achtergrondgeluid weghalen (voice_isolation.py)

        public AudioResult IsolateVoice(string audioPath, string fileFormat = null, CancellationToken cancel = default(CancellationToken))
        {
            RequireFile(audioPath);
            var form = new Dictionary<string, object>();
            Put(form, "file_format", fileFormat);
            return SendAudio("POST", "v1/audio-isolation", null, null, form, new[] { new UploadFile("audio", audioPath) }, cancel);
        }

        // ------------------------------------------------------------------ Spraak naar tekst (speech_to_text.py)

        /// <summary>Geeft het hele antwoord terug (text, language_code, words, additional_formats ...).</summary>
        public Dictionary<string, object> SpeechToText(SpeechToTextRequest r, CancellationToken cancel = default(CancellationToken))
        {
            if (r == null || (string.IsNullOrEmpty(r.AudioPath) && string.IsNullOrEmpty(r.CloudStorageUrl)))
                throw new ArgumentException("Choose an audio file or a cloud storage URL.");
            var form = new Dictionary<string, object> { { "model_id", MediaSuiteModels.Resolve("stt", r.ModelId) }, { "tag_audio_events", r.TagAudioEvents } };
            Put(form, "language_code", r.LanguageCode);
            Put(form, "num_speakers", r.NumSpeakers);
            Put(form, "timestamps_granularity", r.TimestampsGranularity);
            Put(form, "diarize", r.Diarize);
            Put(form, "diarization_threshold", r.DiarizationThreshold);
            if (r.AdditionalFormats.Count > 0) form["additional_formats"] = Json.Serialize(r.AdditionalFormats);
            Put(form, "file_format", r.FileFormat);
            Put(form, "temperature", r.Temperature);
            Put(form, "seed", r.Seed);
            Put(form, "use_multi_channel", r.UseMultiChannel);
            Put(form, "webhook", r.Webhook);
            Put(form, "webhook_id", r.WebhookId);
            Put(form, "cloud_storage_url", r.CloudStorageUrl);
            var query = new Dictionary<string, object>();
            Put(query, "enable_logging", r.EnableLogging);
            UploadFile[] files = null;
            if (!string.IsNullOrEmpty(r.AudioPath)) { RequireFile(r.AudioPath); files = new[] { new UploadFile("file", r.AudioPath) }; }
            return SendJson("POST", "v1/speech-to-text", query, null, form, files, cancel);
        }

        // ------------------------------------------------------------------ Tekst uitlijnen op een opname (forced_alignment.py)

        /// <summary>Geeft characters, words (text, start, end) en loss terug.</summary>
        public Dictionary<string, object> ForcedAlignment(string audioPath, string transcript, CancellationToken cancel = default(CancellationToken))
        {
            RequireFile(audioPath);
            if (string.IsNullOrWhiteSpace(transcript)) throw new ArgumentException("A transcript is required.");
            return SendJson("POST", "v1/forced-alignment", null, null, new Dictionary<string, object> { { "text", transcript } },
                new[] { new UploadFile("file", audioPath) }, cancel);
        }

        // ------------------------------------------------------------------ Stem ontwerpen (voice_design.py)

        /// <summary>Maakt voorbeeldstemmen. Bewaar een ervan met SaveDesignedVoice.</summary>
        public List<VoicePreview> DesignVoice(VoiceDesignRequest r, out string previewText, CancellationToken cancel = default(CancellationToken))
        {
            if (r == null || string.IsNullOrWhiteSpace(r.VoiceDescription)) throw new ArgumentException("A voice description is required.");
            var body = new Dictionary<string, object> { { "voice_description", r.VoiceDescription } };
            Put(body, "model_id", r.ModelId);
            Put(body, "text", r.Text);
            Put(body, "auto_generate_text", r.AutoGenerateText);
            Put(body, "loudness", r.Loudness);
            Put(body, "seed", r.Seed);
            Put(body, "guidance_scale", r.GuidanceScale);
            Put(body, "should_enhance", r.ShouldEnhance);
            Put(body, "quality", r.Quality);
            Put(body, "reference_audio_base64", r.ReferenceAudioBase64);
            Put(body, "prompt_strength", r.PromptStrength);
            Put(body, "remixing_session_id", r.RemixingSessionId);
            Put(body, "remixing_session_iteration_id", r.RemixingSessionIterationId);
            var data = SendJson("POST", "v1/text-to-voice/design", new Dictionary<string, object> { { "output_format", r.OutputFormat } }, body, null, null, cancel);
            previewText = Str(data, "text");
            var previews = new List<VoicePreview>();
            foreach (var item in List(data, "previews"))
            {
                var audio = Str(item, "audio_base_64");
                previews.Add(new VoicePreview
                {
                    GeneratedVoiceId = Str(item, "generated_voice_id"),
                    Audio = string.IsNullOrEmpty(audio) ? new byte[0] : Convert.FromBase64String(audio),
                    MediaType = Str(item, "media_type") ?? "audio/mpeg",
                    DurationSeconds = Num(item, "duration_secs"),
                    Language = Str(item, "language")
                });
            }
            return previews;
        }

        /// <summary>Bewaart een ontworpen stem in je account. Geeft de nieuwe voice_id terug.</summary>
        public string SaveDesignedVoice(string name, string description, string generatedVoiceId, CancellationToken cancel = default(CancellationToken))
        {
            var body = new Dictionary<string, object> { { "voice_name", name }, { "voice_description", description }, { "generated_voice_id", generatedVoiceId } };
            return Str(SendJson("POST", "v1/text-to-voice", null, body, null, null, cancel), "voice_id");
        }

        // ------------------------------------------------------------------ Stem klonen (voice_cloning.py)

        /// <summary>Instant Voice Cloning. Gebruik alleen stemmen waarvoor je toestemming hebt.</summary>
        public Dictionary<string, object> CloneVoice(string name, IList<string> samplePaths, string description = null,
            Dictionary<string, string> labels = null, bool? removeBackgroundNoise = null, CancellationToken cancel = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A name is required.");
            if (samplePaths == null || samplePaths.Count == 0) throw new ArgumentException("Choose at least one recording.");
            var files = new List<UploadFile>();
            foreach (var path in samplePaths) { RequireFile(path); files.Add(new UploadFile("files", path)); }
            var form = new Dictionary<string, object> { { "name", name } };
            Put(form, "description", description);
            if (labels != null && labels.Count > 0) form["labels"] = Json.Serialize(labels);
            Put(form, "remove_background_noise", removeBackgroundNoise);
            return SendJson("POST", "v1/voices/add", null, null, form, files, cancel);
        }

        public void DeleteVoice(string voiceId, CancellationToken cancel = default(CancellationToken))
        {
            SendJson("DELETE", "v1/voices/" + Uri.EscapeDataString(voiceId), cancel: cancel);
        }

        public void EditVoice(string voiceId, string name, string description = null, Dictionary<string, string> labels = null,
            CancellationToken cancel = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name)) name = GetVoice(voiceId, cancel).Name;
            var form = new Dictionary<string, object> { { "name", name } };
            Put(form, "description", description);
            if (labels != null && labels.Count > 0) form["labels"] = Json.Serialize(labels);
            SendJson("POST", "v1/voices/" + Uri.EscapeDataString(voiceId) + "/edit", null, null, form, null, cancel);
        }

        // ------------------------------------------------------------------ Stemmen (tts.py, voice_library.py)

        /// <summary>Stemmen in je eigen account. Met search wordt op naam gefilterd.</summary>
        public List<VoiceInfo> ListVoices(string search = null, CancellationToken cancel = default(CancellationToken))
        {
            var data = SendJson("GET", "v1/voices", cancel: cancel);
            var result = new List<VoiceInfo>();
            foreach (var item in List(data, "voices"))
            {
                var voice = ToVoice(item);
                if (string.IsNullOrEmpty(voice.VoiceId)) continue;
                if (!string.IsNullOrEmpty(search) && (voice.Name ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                result.Add(voice);
            }
            return result;
        }

        /// <summary>Stemmen uit de openbare stemmenbibliotheek van ElevenLabs, per pagina.</summary>
        public SharedVoicesPage GetSharedVoices(SharedVoiceQuery q, CancellationToken cancel = default(CancellationToken))
        {
            q = q ?? new SharedVoiceQuery();
            var query = new Dictionary<string, object> { { "page", q.Page }, { "page_size", q.PageSize } };
            Put(query, "search", q.Search);
            Put(query, "sort", q.Sort);
            Put(query, "category", q.Category);
            Put(query, "gender", q.Gender);
            Put(query, "age", q.Age);
            Put(query, "accent", q.Accent);
            Put(query, "language", q.Language);
            if (q.UseCases.Count > 0) query["use_cases"] = q.UseCases;
            if (q.Descriptives.Count > 0) query["descriptives"] = q.Descriptives;
            Put(query, "featured", q.Featured);
            var data = SendJson("GET", "v1/shared-voices", query, cancel: cancel);
            var page = new SharedVoicesPage { HasMore = Bool(data, "has_more"), Page = q.Page, PageSize = q.PageSize };
            foreach (var item in List(data, "voices")) page.Voices.Add(ToVoice(item));
            page.TotalCount = (int)Num(data, "total_count", page.Voices.Count);
            return page;
        }

        /// <summary>Haalt meerdere pagina's op, tot maxVoices of het einde.</summary>
        public SharedVoicesPage GetAllSharedVoices(SharedVoiceQuery q, int maxVoices = 500, CancellationToken cancel = default(CancellationToken))
        {
            q = q ?? new SharedVoiceQuery();
            var all = new SharedVoicesPage { Page = q.Page, PageSize = q.PageSize };
            while (true)
            {
                var page = GetSharedVoices(q, cancel);
                all.Voices.AddRange(page.Voices);
                all.TotalCount = page.TotalCount;
                all.HasMore = page.HasMore;
                if (!page.HasMore || page.Voices.Count == 0 || all.Voices.Count >= maxVoices) break;
                q.Page++;
            }
            if (all.Voices.Count > maxVoices) all.Voices.RemoveRange(maxVoices, all.Voices.Count - maxVoices);
            return all;
        }

        public VoiceInfo GetVoice(string voiceId, CancellationToken cancel = default(CancellationToken))
        {
            return ToVoice(SendJson("GET", "v1/voices/" + Uri.EscapeDataString(voiceId), cancel: cancel));
        }

        private static readonly string[] PreviewHosts = { "elevenlabs.io", "elevenlabs.com", "storage.googleapis.com" };
        private const int PreviewMaxBytes = 5 * 1024 * 1024;

        /// <summary>Downloadt het voorbeeldje van een stem: alleen https, alleen ElevenLabs-hosts, geen omleiding, max 5 MB.</summary>
        public static byte[] FetchPreview(string previewUrl, int timeoutMilliseconds = 20000)
        {
            Uri uri;
            if (!Uri.TryCreate(previewUrl, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Refusing non-HTTPS preview URL.");
            var host = uri.Host.ToLowerInvariant();
            var allowed = false;
            foreach (var suffix in PreviewHosts) if (host == suffix || host.EndsWith("." + suffix, StringComparison.Ordinal)) allowed = true;
            if (!allowed) throw new ArgumentException("Refusing preview URL from untrusted host: " + host);
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AllowAutoRedirect = false;
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if ((int)response.StatusCode >= 300) throw new InvalidDataException("Preview fetch failed with HTTP " + (int)response.StatusCode);
                using (var stream = response.GetResponseStream())
                using (var memory = new MemoryStream())
                {
                    var buffer = new byte[64 * 1024];
                    int count;
                    while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        memory.Write(buffer, 0, count);
                        if (memory.Length > PreviewMaxBytes) throw new InvalidDataException("Preview exceeds maximum allowed size (5 MB).");
                    }
                    return memory.ToArray();
                }
            }
        }

        // ------------------------------------------------------------------ Nasynchroniseren (dubbing.py)

        /// <summary>Start nasynchroniseren. Geeft dubbing_id en expected_duration_sec terug.</summary>
        public Dictionary<string, object> StartDubbing(DubbingRequest r, CancellationToken cancel = default(CancellationToken))
        {
            if (r == null || string.IsNullOrWhiteSpace(r.TargetLanguage)) throw new ArgumentException("A target language is required.");
            if (string.IsNullOrEmpty(r.FilePath) && string.IsNullOrEmpty(r.SourceUrl)) throw new ArgumentException("Choose a file or a source URL.");
            var form = new Dictionary<string, object> { { "target_lang", r.TargetLanguage } };
            Put(form, "source_lang", r.SourceLanguage);
            Put(form, "name", r.Name);
            Put(form, "source_url", r.SourceUrl);
            Put(form, "num_speakers", r.NumSpeakers);
            Put(form, "watermark", r.Watermark);
            Put(form, "start_time", r.StartTime);
            Put(form, "end_time", r.EndTime);
            Put(form, "highest_resolution", r.HighestResolution);
            Put(form, "drop_background_audio", r.DropBackgroundAudio);
            Put(form, "use_profanity_filter", r.UseProfanityFilter);
            Put(form, "dubbing_studio", r.DubbingStudio);
            Put(form, "disable_voice_cloning", r.DisableVoiceCloning);
            Put(form, "mode", r.Mode);
            Put(form, "target_accent", r.TargetAccent);
            Put(form, "csv_fps", r.CsvFps);
            var files = new List<UploadFile>();
            if (!string.IsNullOrEmpty(r.FilePath)) { RequireFile(r.FilePath); files.Add(new UploadFile("file", r.FilePath)); }
            if (!string.IsNullOrEmpty(r.CsvFilePath)) { RequireFile(r.CsvFilePath); files.Add(new UploadFile("csv_file", r.CsvFilePath)); }
            if (!string.IsNullOrEmpty(r.ForegroundAudioPath)) { RequireFile(r.ForegroundAudioPath); files.Add(new UploadFile("foreground_audio_file", r.ForegroundAudioPath)); }
            if (!string.IsNullOrEmpty(r.BackgroundAudioPath)) { RequireFile(r.BackgroundAudioPath); files.Add(new UploadFile("background_audio_file", r.BackgroundAudioPath)); }
            return SendJson("POST", "v1/dubbing", null, null, form, files, cancel);
        }

        /// <summary>Status van een nasynchronisatie: "dubbing", "dubbed" of "failed".</summary>
        public string GetDubbingStatus(string dubbingId, CancellationToken cancel = default(CancellationToken))
        {
            return Str(SendJson("GET", "v1/dubbing/" + Uri.EscapeDataString(dubbingId), cancel: cancel), "status");
        }

        public AudioResult GetDubbedFile(string dubbingId, string languageCode, CancellationToken cancel = default(CancellationToken))
        {
            return SendAudio("GET", "v1/dubbing/" + Uri.EscapeDataString(dubbingId) + "/audio/" + Uri.EscapeDataString(languageCode), cancel: cancel);
        }

        // ------------------------------------------------------------------ Uitspraaklijsten (pronunciation.py)

        public List<Dictionary<string, object>> ListPronunciationDictionaries(CancellationToken cancel = default(CancellationToken))
        {
            return List(SendJson("GET", "v1/pronunciation-dictionaries", new Dictionary<string, object> { { "page_size", 100 } }, cancel: cancel),
                "pronunciation_dictionaries");
        }

        /// <summary>Maakt een uitspraaklijst. Geeft id en version_id terug.</summary>
        public Dictionary<string, object> CreatePronunciationDictionary(string name, IList<PronunciationRule> rules, string description = null,
            CancellationToken cancel = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A name is required.");
            var list = new List<object>();
            foreach (var rule in rules) list.Add(rule.ToDictionary());
            var body = new Dictionary<string, object> { { "name", name }, { "rules", list } };
            Put(body, "description", description);
            return SendJson("POST", "v1/pronunciation-dictionaries/add-from-rules", null, body, null, null, cancel);
        }

        /// <summary>Maakt een uitspraaklijst uit een .pls-bestand.</summary>
        public Dictionary<string, object> CreatePronunciationDictionaryFromFile(string name, string plsPath, string description = null,
            CancellationToken cancel = default(CancellationToken))
        {
            RequireFile(plsPath);
            var form = new Dictionary<string, object> { { "name", name } };
            Put(form, "description", description);
            return SendJson("POST", "v1/pronunciation-dictionaries/add-from-file", null, null, form, new[] { new UploadFile("file", plsPath) }, cancel);
        }

        // ------------------------------------------------------------------ Geschiedenis (history.py)

        public HistoryPage ListHistory(int pageSize = 100, string startAfterHistoryItemId = null, CancellationToken cancel = default(CancellationToken))
        {
            var query = new Dictionary<string, object> { { "page_size", pageSize } };
            Put(query, "start_after_history_item_id", startAfterHistoryItemId);
            var data = SendJson("GET", "v1/history", query, cancel: cancel);
            return new HistoryPage { Items = List(data, "history"), HasMore = Bool(data, "has_more"), LastHistoryItemId = Str(data, "last_history_item_id") };
        }

        public AudioResult DownloadHistoryItem(string historyItemId, CancellationToken cancel = default(CancellationToken))
        {
            return SendAudio("GET", "v1/history/" + Uri.EscapeDataString(historyItemId) + "/audio", cancel: cancel);
        }

        public void DeleteHistoryItem(string historyItemId, CancellationToken cancel = default(CancellationToken))
        {
            SendJson("DELETE", "v1/history/" + Uri.EscapeDataString(historyItemId), cancel: cancel);
        }

        // ------------------------------------------------------------------ Account en modellen (user.py, tts.py)

        public Dictionary<string, object> GetUser(CancellationToken cancel = default(CancellationToken))
        {
            return SendJson("GET", "v1/user", cancel: cancel);
        }

        /// <summary>tier, character_count, character_limit, next_character_count_reset_unix ...</summary>
        public Dictionary<string, object> GetSubscription(CancellationToken cancel = default(CancellationToken))
        {
            return SendJson("GET", "v1/user/subscription", cancel: cancel);
        }

        /// <summary>De modellen van je account (model_id, name, can_do_text_to_speech, languages ...).</summary>
        public List<Dictionary<string, object>> ListModels(CancellationToken cancel = default(CancellationToken))
        {
            string type, id;
            var bytes = Send("GET", "v1/models", null, null, null, null, cancel, out type, out id);
            var result = new List<Dictionary<string, object>>();
            var array = Json.DeserializeObject(Encoding.UTF8.GetString(bytes)) as object[];
            if (array != null) foreach (var item in array) { var d = item as Dictionary<string, object>; if (d != null) result.Add(d); }
            return result;
        }

        // ------------------------------------------------------------------ Hulpjes

        private static object[] Locators(List<DictionaryLocator> dictionaries)
        {
            if (dictionaries.Count > 3) throw new ArgumentException("At most three pronunciation dictionaries can be used.");
            var list = new object[dictionaries.Count];
            for (var i = 0; i < list.Length; i++) list[i] = dictionaries[i].ToDictionary();
            return list;
        }

        private static void RequireFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Choose a file.");
            if (!File.Exists(path)) throw new FileNotFoundException("The file was not found.", path);
        }

        private static VoiceInfo ToVoice(Dictionary<string, object> item)
        {
            object labels;
            item.TryGetValue("labels", out labels);
            return new VoiceInfo
            {
                VoiceId = Str(item, "voice_id"),
                Name = Str(item, "name") ?? Str(item, "voice_id"),
                Category = Str(item, "category"),
                Description = Str(item, "description"),
                PreviewUrl = Str(item, "preview_url"),
                Labels = labels as Dictionary<string, object>,
                Raw = item
            };
        }
    }
}
