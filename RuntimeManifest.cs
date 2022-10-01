using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SLZ.XRDoctor;

[Serializable]
public sealed class RuntimeManifest {
    [JsonProperty("file_format_version", Required = Required.Always)]
    public string FileFormatVersion { get; private set; }

    [JsonProperty("runtime", Required = Required.Always)]
    public Runtime Runtime { get; private set; }
}

[Serializable]
public sealed class Runtime {
    [JsonProperty("name")] public string Name { get; private set; }

    [JsonProperty("library_path", Required = Required.Always)]
    public string LibraryPath { get; private set; }

    [JsonExtensionData] public IDictionary<string, object> AdditionalData { get; private set; }
}