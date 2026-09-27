// Pollinations Unity package — runtime API client.
// BYOP: every generation is billed to the API key's own Pollen account.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Pollinations
{
    /// <summary>Static configuration; set your API key before first use.</summary>
    public static class PollinationsConfig
    {
        /// <summary>API key from https://enter.pollinations.ai/keys — costs go to your own Pollen.</summary>
        public static string ApiKey = "";

        /// <summary>API base; the hosted endpoint by default.</summary>
        public static string BaseUrl = "https://gen.pollinations.ai";

        public static string TextModel = "openai";
        public static string ImageModel = "flux";
        public static string SpeechModel = "openai-audio";
        public static string Voice = "alloy";

        internal static void ApplyHeaders(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + ApiKey);
            }
        }
    }

    /// <summary>One assistant/user message in a chat.</summary>
    [Serializable]
    public struct ChatMessage
    {
        public string role;
        public string content;

        public static ChatMessage User(string content) =>
            new ChatMessage { role = "user", content };

        public static ChatMessage System(string content) =>
            new ChatMessage { role = "system", content };

        public static ChatMessage Assistant(string content) =>
            new ChatMessage { role = "assistant", content };
    }

    /// <summary>
    /// Async helpers for Pollinations text, image and speech generation.
    /// All methods run on the main thread's continuation (safe for Unity APIs).
    /// </summary>
    public static class PollinationsClient
    {
        public const string DefaultBase = "https://gen.pollinations.ai";

        // ---- text ------------------------------------------------------

        /// <summary>Single-prompt completion. Returns the reply text.</summary>
        public static Task<string> GenerateTextAsync(
            string prompt, string model = null, string system = null)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(system))
            {
                messages.Add(ChatMessage.System(system));
            }
            messages.Add(ChatMessage.User(prompt));
            return ChatAsync(messages, model);
        }

        /// <summary>Full chat completion over an arbitrary message list.</summary>
        public static async Task<string> ChatAsync(
            List<ChatMessage> messages, string model = null)
        {
            var body = new StringBuilder("{\"model\":");
            body.Append(Json(model ?? PollinationsConfig.TextModel));
            body.Append(",\"messages\":[");
            for (int i = 0; i < messages.Count; i += 1)
            {
                if (i > 0) body.Append(',');
                body.Append("{\"role\":");
                body.Append(Json(messages[i].role));
                body.Append(",\"content\":");
                body.Append(Json(messages[i].content));
                body.Append('}');
            }
            body.Append("]}");

            string json = await PostTextAsync(
                $"{Base()}/v1/chat/completions", body.ToString());
            string content = ExtractString(json, "content");
            if (content == null)
            {
                throw new PollinationsException("no content in completion");
            }
            return content;
        }

        // ---- image -----------------------------------------------------

        /// <summary>
        /// Generates an image and returns it as a Texture2D.
        /// Caller owns the texture and must Destroy it.
        /// </summary>
        public static async Task<Texture2D> GenerateImageAsync(
            string prompt, int width = 1024, int height = 1024,
            string model = null)
        {
            string payload =
                "{\"model\":" + Json(model ?? PollinationsConfig.ImageModel) +
                ",\"prompt\":" + Json(prompt) +
                ",\"size\":\"" + width + "x" + height + "\"}";
            string json = await PostTextAsync(
                $"{Base()}/v1/images/generations", payload);

            string base64 = ExtractString(json, "b64_json");
            byte[] png;
            if (base64 != null)
            {
                png = Convert.FromBase64String(base64);
            }
            else
            {
                string url = ExtractString(json, "url");
                if (url == null)
                {
                    throw new PollinationsException("no image in response");
                }
                png = await DownloadBytesAsync(url);
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(png))
            {
                UnityEngine.Object.Destroy(texture);
                throw new PollinationsException("invalid PNG payload");
            }
            return texture;
        }

        // ---- speech ----------------------------------------------------

        /// <summary>
        /// Text-to-speech; returns raw audio bytes (wav/mp3 per the model).
        /// </summary>
        public static Task<byte[]> GenerateSpeechAsync(
            string text, string voice = null, string model = null)
        {
            var payload = new StringBuilder("{\"model\":");
            payload.Append(Json(model ?? PollinationsConfig.SpeechModel));
            payload.Append(",\"voice\":");
            payload.Append(Json(voice ?? PollinationsConfig.Voice));
            payload.Append(",\"input\":");
            payload.Append(Json(text));
            payload.Append("}");
            return DownloadBytesAsync(
                $"{Base()}/v1/audio/speech", payload.ToString());
        }

        // ---- plumbing --------------------------------------------------

        internal static string Base() =>
            PollinationsConfig.BaseUrl.TrimEnd('/');

        private static async Task<string> PostTextAsync(
            string url, string jsonBody)
        {
            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                PollinationsConfig.ApplyHeaders(request);
                await SendAsync(request);
                ThrowIfFailed(request);
                return request.downloadHandler.text;
            }
        }

        private static async Task<byte[]> DownloadBytesAsync(
            string url, string jsonBody = null)
        {
            using (var request = string.IsNullOrEmpty(jsonBody)
                ? UnityWebRequest.Get(url)
                : BuildPost(url, jsonBody))
            {
                PollinationsConfig.ApplyHeaders(request);
                await SendAsync(request);
                ThrowIfFailed(request);
                return request.downloadHandler.data;
            }
        }

        private static UnityWebRequest BuildPost(string url, string jsonBody)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] body = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static Task SendAsync(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();
            var completion = new TaskCompletionSource<bool>();
            operation.completed += _ => completion.SetResult(true);
            return completion.Task;
        }

        private static void ThrowIfFailed(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                throw new PollinationsException(request.error);
            }
            if (request.responseCode >= 400)
            {
                throw new PollinationsException(
                    $"HTTP {request.responseCode}: {request.downloadHandler.text}");
            }
        }

        /// <summary>Minimal JSON string encoder (RFC 8259 escapes).</summary>
        internal static string Json(string value)
        {
            if (value == null) return "null";
            var builder = new StringBuilder("\"");
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }

        /// <summary>Extracts a top-level or first-level string field.</summary>
        internal static string ExtractString(string json, string field)
        {
            // Deliberately tiny: the fields we read are flat strings in
            // OpenAI-shaped responses. Full JSON parsing stays out of the
            // runtime to keep the package dependency-free.
            string token = "\"" + field + "\"";
            int fieldIndex = json.IndexOf(token);
            if (fieldIndex < 0) return null;
            int colon = json.IndexOf(':', fieldIndex + token.Length);
            if (colon < 0) return null;
            int quoteStart = json.IndexOf('"', colon + 1);
            if (quoteStart < 0) return null;
            var result = new StringBuilder();
            for (int i = quoteStart + 1; i < json.Length; i += 1)
            {
                char c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    i += 1;
                    char escaped = json[i];
                    switch (escaped)
                    {
                        case 'n': result.Append('\n'); break;
                        case 't': result.Append('\t'); break;
                        case 'r': result.Append('\r'); break;
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        default: result.Append(escaped); break;
                    }
                }
                else if (c == '"')
                {
                    return result.ToString();
                }
                else
                {
                    result.Append(c);
                }
            }
            return null;
        }
    }

    /// <summary>Represents a failed Pollinations API call.</summary>
    public class PollinationsException : Exception
    {
        public PollinationsException(string message) : base(message) { }
    }
}
