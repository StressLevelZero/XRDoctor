using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SLZ.XRDoctor;

[Serializable]
public sealed class RuntimeManifest {
    [JsonProperty("file_format_version", Required = Required.Always)]
    public string FileFormatVersion { get; private set; } = null!;

    [JsonProperty("runtime", Required = Required.Always)]
    public Runtime Runtime { get; private set; } = null!;

    [JsonConstructor]
    public RuntimeManifest() { }
}

[Serializable]
public sealed class Runtime {
    [JsonProperty("name")]
    public string Name { get; private set; } = null!;

    [JsonProperty("library_path", Required = Required.Always)]
    public string LibraryPath { get; private set; } = null!;

    [JsonExtensionData]
    public IDictionary<string, object> AdditionalData { get; private set; } = null!;

    [JsonConstructor]
    public Runtime() { }
}