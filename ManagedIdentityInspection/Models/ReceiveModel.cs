using Newtonsoft.Json;

namespace ManagedIdentityInspection.Models
{
    /// <summary>
    /// RequestBodyモデル
    /// </summary>
    public class ReceiveModel
    {
        [JsonProperty("title")]
        public string title { get; set; } = string.Empty;
        [JsonProperty("project")]
        public string project { get; set; } = string.Empty;
    }
}
