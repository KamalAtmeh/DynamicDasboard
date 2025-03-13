using System.Text.Json.Serialization;

namespace DynamicDashboardCommon.Models
{
    public class DeepSeekResponse
    {
        [JsonPropertyName("choices")]
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public string content { get; set; }
    }
}