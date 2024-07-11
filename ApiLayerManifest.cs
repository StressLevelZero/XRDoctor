using Newtonsoft.Json;

namespace SLZ.XRDoctor;

public sealed class ApiLayerManifest {
    [JsonProperty("file_format_version", Required = Required.Always)]
    public string FileFormatVersion { get; private set; } = null!;

    [JsonProperty("api_layer", Required = Required.Always)]
    public ApiLayer ApiLayer { get; private set; } = null!;

    [JsonConstructor]
    public ApiLayerManifest() { }
}

public sealed class ApiLayer {
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; private set; } = null!;

    [JsonProperty("library_path", Required = Required.Always)]
    public string LibraryPath { get; private set; } = null!;

    [JsonProperty("api_version", Required = Required.Always)]
    public string ApiVersion { get; private set; } = null!;

    [JsonProperty("implementation_version", Required = Required.Always)]
    public string ImplementationVersion { get; private set; } = null!;

    [JsonProperty("description", Required = Required.Always)]
    public string Description { get; private set; } = null!;

    [JsonProperty("functions")]
    public Dictionary<string, string> Functions { get; private set; } = null!;

    [JsonProperty("instance_extensions")]
    public List<InstanceExtension> InstanceExtensions { get; private set; } = null!;

    [JsonProperty("disable_environment")]
    public string DisableEnvironment { get; private set; } = null!;

    [JsonProperty("enable_environment")]
    public string EnableEnvironment { get; private set; } = null!;

    [JsonConstructor]
    public ApiLayer() { }
}

public sealed class InstanceExtension {
    [JsonProperty("name")]
    public string Name { get; private set; } = null!;

    [JsonProperty("extension_version")]
    public object ExtensionVersion { get; private set; } = null!;

    [JsonProperty("entrypoints")]
    public List<string> Entrypoints { get; private set; } = null!;

    [JsonConstructor]
    public InstanceExtension() { }
}