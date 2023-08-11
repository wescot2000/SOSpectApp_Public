using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace sospect.Models
{
    public class DeviceInstallation
    {
        [JsonProperty("installationId")]
        public string InstallationId { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("pushChannel")]
        public string PushChannel { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
}

