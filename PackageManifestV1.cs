﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ThunderstoreCLI
{
    public class PackageManifestV1
    {
        [JsonPropertyName("namespace")]
        public string Namespace { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; }

        [JsonPropertyName("dependencies")]
        public string[] Dependencies { get; set; }

        [JsonPropertyName("website_url")]
        public string WebsiteUrl { get; set; }
    }
}
