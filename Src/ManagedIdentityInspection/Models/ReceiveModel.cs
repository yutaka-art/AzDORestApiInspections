using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagedIdentityInspection.Models
{
    public class ReceiveModel
    {
        [JsonProperty("title")]
        public string title { get; set; } = string.Empty;
        [JsonProperty("project")]
        public string project { get; set; } = string.Empty;
    }
}
