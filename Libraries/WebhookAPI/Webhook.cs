using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace DiscordWebhook
{
    [JsonObject]
    public class Webhook
    {
        private HttpClient _httpClient;

        private readonly string _webhookUrl;

        internal void Dispose()
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }

        internal Webhook(string webhookUrl)
        {
            _httpClient = new HttpClient();
            _webhookUrl = webhookUrl;
        }

        internal Webhook(ulong id, string token) : this($"https://discord.com/api/webhooks/{id}/{token}")
        {
        }

        [JsonProperty("content")] public string Content { get; set; }

        [JsonProperty("username")] public string Username { get; set; }

        [JsonProperty("avatar_url")] public string AvatarUrl { get; set; }

        // ReSharper disable once InconsistentNaming
        [JsonProperty("tts")] public bool IsTTS { get; set; }

        [JsonProperty("embeds")] public List<Embed> Embeds { get; set; } = new();

        internal void Send()
        {
            var content = new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
            var thing = _httpClient.PostAsync(_webhookUrl, content).Result;
        }

        // ReSharper disable once InconsistentNaming
        internal void Send(string content, string username = null, string avatarUrl = null, bool isTTS = false,
                           IEnumerable<Embed> embeds = null)
        {
            Content = content;
            Username = username;
            AvatarUrl = avatarUrl;
            IsTTS = isTTS;

            Embeds.Clear();

            if (embeds != null)
            {
                Embeds.AddRange(embeds);
            }

            Send();
        }
    }

    public static class Ext
    {
        internal static int ToRgb(this Color color)
        {
            return int.Parse(ColorTranslator.ToHtml(Color.FromArgb(color.ToArgb())).Replace("#", ""),
                NumberStyles.HexNumber);
        }
    }
}