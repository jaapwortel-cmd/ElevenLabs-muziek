// Omgezet naar C# uit "ElevenLabs Media Suite" (https://github.com/audioses/elevenlabs-media-suite,
// MIT-licentie, Copyright (c) 2026 ElevenLabs Media Suite contributors), src/api/client.py en sdk_client.py.
// De Python-versie gebruikte het ElevenLabs-pakket; hier praten we rechtstreeks met de REST-API,
// net als ElevenLabsMusicClient van Andre Louis. Webadressen en veldnamen gecontroleerd in het
// officiele Python-pakket elevenlabs 2.79.0 (19 nov 2026).
// Nog NIET in het programma opgenomen: Build.ps1 bouwt alleen de bestanden direct in src.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace ElevenLabsMusicGenerator.MediaSuite
{
    /// <summary>Een bestand dat mee moet in een multipart-verzoek.</summary>
    internal sealed class UploadFile
    {
        public string FieldName;
        public string Path;
        public UploadFile(string fieldName, string path) { FieldName = fieldName; Path = path; }
    }

    /// <summary>Geluid dat ElevenLabs teruggaf.</summary>
    internal sealed class AudioResult
    {
        public byte[] Audio;
        public string ContentType;
        public string RequestId;
        public string Extension
        {
            get
            {
                var type = (ContentType ?? string.Empty).ToLowerInvariant();
                if (type.Contains("wav") || type.Contains("pcm")) return ".wav";
                if (type.Contains("opus") || type.Contains("ogg")) return ".opus";
                if (type.Contains("mp4") || type.Contains("m4a")) return ".m4a";
                if (type.Contains("video")) return ".mp4";
                return ".mp3";
            }
        }
    }

    /// <summary>
    /// Basis om met de ElevenLabs-API te praten: JSON, multipart, query-parameters,
    /// opnieuw proberen bij 429/5xx (zoals Retry(total=3, backoff_factor=0.6) in de Python-versie).
    /// </summary>
    internal sealed partial class MediaSuiteClient
    {
        private const string DefaultApiRoot = "https://api.elevenlabs.io";
        private const int MaxAttempts = 4;
        private readonly string apiKey;
        private readonly string apiRoot;

        public int TimeoutMilliseconds = 10 * 60 * 1000;

        public MediaSuiteClient(string apiKey) : this(apiKey, DefaultApiRoot) { }

        internal MediaSuiteClient(string apiKey, string apiRoot)
        {
            this.apiKey = (apiKey ?? string.Empty).Trim();
            this.apiRoot = (apiRoot ?? DefaultApiRoot).TrimEnd('/');
            if (this.apiKey.Length == 0) throw new ArgumentException(L.T("Er is nog geen API-sleutel van ElevenLabs. Zet hem in Opties, Voorkeuren.", "There is no ElevenLabs API key yet. Enter it in Options, Preferences."), "apiKey");
        }

        // ------------------------------------------------------------------ JSON

        internal static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 256 };

        internal static Dictionary<string, object> ParseObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new Dictionary<string, object>();
            var value = Json.DeserializeObject(text) as Dictionary<string, object>;
            return value ?? new Dictionary<string, object>();
        }

        internal static string Str(IDictionary<string, object> data, string key)
        {
            object value;
            if (data == null || !data.TryGetValue(key, out value) || value == null) return null;
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        internal static double Num(IDictionary<string, object> data, string key, double fallback = 0)
        {
            object value;
            if (data == null || !data.TryGetValue(key, out value) || value == null) return fallback;
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); } catch { return fallback; }
        }

        internal static bool Bool(IDictionary<string, object> data, string key)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) && value is bool && (bool)value;
        }

        internal static List<Dictionary<string, object>> List(IDictionary<string, object> data, string key)
        {
            var result = new List<Dictionary<string, object>>();
            object value;
            if (data == null || !data.TryGetValue(key, out value)) return result;
            var array = value as System.Collections.IEnumerable;
            if (array == null || value is string) return result;
            foreach (var item in array)
            {
                var entry = item as Dictionary<string, object>;
                if (entry != null) result.Add(entry);
            }
            return result;
        }

        /// <summary>Voegt alleen waarden toe die niet null zijn (zoals "OMIT" in het Python-pakket).</summary>
        internal static void Put(IDictionary<string, object> target, string key, object value)
        {
            if (value == null) return;
            var text = value as string;
            if (text != null && text.Length == 0) return;
            target[key] = value;
        }

        // ------------------------------------------------------------------ Verzoeken

        private static string Query(IDictionary<string, object> query)
        {
            if (query == null || query.Count == 0) return string.Empty;
            var builder = new StringBuilder();
            foreach (var pair in query)
            {
                if (pair.Value == null) continue;
                var values = pair.Value as System.Collections.IEnumerable;
                if (values != null && !(pair.Value is string))
                {
                    foreach (var item in values) Append(builder, pair.Key, item);
                }
                else Append(builder, pair.Key, pair.Value);
            }
            return builder.Length == 0 ? string.Empty : "?" + builder.ToString(1, builder.Length - 1);
        }

        private static void Append(StringBuilder builder, string key, object value)
        {
            builder.Append('&').Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(FormatValue(value)));
        }

        private static string FormatValue(object value)
        {
            if (value is bool) return (bool)value ? "true" : "false";
            if (value is IFormattable) return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private HttpWebRequest CreateRequest(string method, string path, IDictionary<string, object> query)
        {
            var request = (HttpWebRequest)WebRequest.Create(apiRoot + "/" + path.TrimStart('/') + Query(query));
            request.Method = method;
            request.Timeout = TimeoutMilliseconds;
            request.ReadWriteTimeout = TimeoutMilliseconds;
            request.UserAgent = "ElevenLabsMusicGenerator-MediaSuite";
            request.Headers["xi-api-key"] = apiKey;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }

        /// <summary>Stuurt een verzoek en geeft het antwoord als bytes terug. Probeert opnieuw bij 429 en 5xx.</summary>
        internal byte[] Send(string method, string path, IDictionary<string, object> query, IDictionary<string, object> json,
            IDictionary<string, object> form, IList<UploadFile> files, CancellationToken cancel, out string contentType, out string requestId)
        {
            for (var attempt = 1; ; attempt++)
            {
                cancel.ThrowIfCancellationRequested();
                var request = CreateRequest(method, path, query);
                using (cancel.Register(request.Abort))
                {
                    try
                    {
                        if (json != null) WriteJson(request, json);
                        else if (form != null || files != null) WriteMultipart(request, form, files);
                        using (var response = (HttpWebResponse)request.GetResponse())
                        using (var stream = response.GetResponseStream())
                        using (var memory = new MemoryStream())
                        {
                            stream.CopyTo(memory);
                            contentType = response.ContentType ?? string.Empty;
                            requestId = response.Headers["request-id"];
                            return memory.ToArray();
                        }
                    }
                    catch (WebException ex)
                    {
                        if (cancel.IsCancellationRequested) throw new OperationCanceledException(cancel);
                        var response = ex.Response as HttpWebResponse;
                        var status = response == null ? (HttpStatusCode?)null : response.StatusCode;
                        var body = ReadError(response);
                        var code = status.HasValue ? (int)status.Value : 0;
                        var retry = code == 429 || code >= 500 || ex.Status == WebExceptionStatus.Timeout || ex.Status == WebExceptionStatus.ConnectFailure;
                        if (retry && attempt < MaxAttempts)
                        {
                            Thread.Sleep((int)(600 * Math.Pow(2, attempt - 1)));
                            continue;
                        }
                        throw new ElevenLabsApiException(DescribeError(code, body, ex.Message), status, body, ex);
                    }
                }
            }
        }

        internal Dictionary<string, object> SendJson(string method, string path, IDictionary<string, object> query = null,
            IDictionary<string, object> json = null, IDictionary<string, object> form = null, IList<UploadFile> files = null,
            CancellationToken cancel = default(CancellationToken))
        {
            string type, id;
            var bytes = Send(method, path, query, json, form, files, cancel, out type, out id);
            return ParseObject(Encoding.UTF8.GetString(bytes));
        }

        internal AudioResult SendAudio(string method, string path, IDictionary<string, object> query = null,
            IDictionary<string, object> json = null, IDictionary<string, object> form = null, IList<UploadFile> files = null,
            CancellationToken cancel = default(CancellationToken))
        {
            string type, id;
            var bytes = Send(method, path, query, json, form, files, cancel, out type, out id);
            if (type.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidDataException(L.T("ElevenLabs stuurde tekst in plaats van geluid: ", "ElevenLabs returned text instead of audio: ") + Encoding.UTF8.GetString(bytes));
            return new AudioResult { Audio = bytes, ContentType = type, RequestId = id };
        }

        private static void WriteJson(HttpWebRequest request, IDictionary<string, object> json)
        {
            var bytes = Encoding.UTF8.GetBytes(Json.Serialize(json));
            request.ContentType = "application/json";
            request.ContentLength = bytes.Length;
            using (var output = request.GetRequestStream()) output.Write(bytes, 0, bytes.Length);
        }

        private static void WriteMultipart(HttpWebRequest request, IDictionary<string, object> form, IList<UploadFile> files)
        {
            var boundary = "----MediaSuite" + Guid.NewGuid().ToString("N");
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.AllowWriteStreamBuffering = false;
            request.SendChunked = true;
            using (var output = request.GetRequestStream())
            {
                if (form != null)
                {
                    foreach (var pair in form)
                    {
                        if (pair.Value == null) continue;
                        var values = pair.Value as System.Collections.IEnumerable;
                        if (values != null && !(pair.Value is string))
                            foreach (var item in values) WriteField(output, boundary, pair.Key, FormatValue(item));
                        else WriteField(output, boundary, pair.Key, FormatValue(pair.Value));
                    }
                }
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        var header = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"" + file.FieldName + "\"; filename=\"" +
                            System.IO.Path.GetFileName(file.Path).Replace("\"", "'") + "\"\r\nContent-Type: " + MimeType(file.Path) + "\r\n\r\n";
                        WriteText(output, header);
                        using (var input = File.OpenRead(file.Path)) input.CopyTo(output);
                        WriteText(output, "\r\n");
                    }
                }
                WriteText(output, "--" + boundary + "--\r\n");
            }
        }

        private static void WriteField(Stream output, string boundary, string name, string value)
        {
            WriteText(output, "--" + boundary + "\r\nContent-Disposition: form-data; name=\"" + name + "\"\r\n\r\n" + value + "\r\n");
        }

        private static void WriteText(Stream output, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            output.Write(bytes, 0, bytes.Length);
        }

        internal static string MimeType(string path)
        {
            switch ((System.IO.Path.GetExtension(path) ?? string.Empty).ToLowerInvariant())
            {
                case ".mp3": return "audio/mpeg";
                case ".wav": return "audio/wav";
                case ".m4a": return "audio/mp4";
                case ".aac": return "audio/aac";
                case ".flac": return "audio/flac";
                case ".ogg": case ".opus": return "audio/ogg";
                case ".webm": return "video/webm";
                case ".mp4": return "video/mp4";
                case ".mov": return "video/quicktime";
                case ".mkv": return "video/x-matroska";
                case ".csv": return "text/csv";
                case ".pls": case ".xml": return "application/xml";
                case ".txt": return "text/plain";
                default: return "application/octet-stream";
            }
        }

        private static string ReadError(HttpWebResponse response)
        {
            if (response == null) return string.Empty;
            try
            {
                using (response)
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var text = reader.ReadToEnd();
                    return text.Length > 4000 ? text.Substring(0, 4000) : text;
                }
            }
            catch { return string.Empty; }
        }

        private static string DescribeError(int code, string body, string fallback)
        {
            var message = string.Empty;
            try
            {
                var data = ParseObject(body);
                object detail;
                if (data.TryGetValue("detail", out detail))
                {
                    var detailObject = detail as Dictionary<string, object>;
                    message = detailObject != null ? (Str(detailObject, "message") ?? Json.Serialize(detailObject)) : Convert.ToString(detail, CultureInfo.InvariantCulture);
                }
            }
            catch { message = body; }
            if (string.IsNullOrWhiteSpace(message)) message = string.IsNullOrWhiteSpace(body) ? fallback : body;
            var hint = string.Empty;
            if (code == 401) hint = L.T("De API-sleutel klopt niet, of hij mag dit onderdeel niet gebruiken. Kijk bij ElevenLabs welke toegang de sleutel heeft. ", "The API key is wrong, or it is not allowed to use this feature. Check the key's permissions at ElevenLabs. ");
            else if (code == 402 || (message ?? string.Empty).IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0) hint = L.T("Je tegoed bij ElevenLabs is op, of dit kan niet met jouw abonnement. ", "Your ElevenLabs credits have run out, or your plan does not include this. ");
            else if (code == 429) hint = L.T("ElevenLabs is even te druk, of je doet te veel tegelijk. Probeer het zo nog eens. ", "ElevenLabs is busy, or too many requests were made at once. Try again shortly. ");
            else if (code >= 500) hint = L.T("Er is een storing bij ElevenLabs. Probeer het later nog eens. ", "ElevenLabs has a problem. Try again later. ");
            return code > 0 ? hint + L.T("ElevenLabs gaf fout ", "ElevenLabs returned error ") + code + ": " + message : message;
        }
    }
}
