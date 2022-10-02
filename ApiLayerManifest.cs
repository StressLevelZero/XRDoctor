using Newtonsoft.Json;

namespace SLZ.XRDoctor;

public sealed class ApiLayerManifest {
    [JsonProperty("file_format_version", Required = Required.Always)]
    public string FileFormatVersion { get; private set; }

    [JsonProperty("api_layer", Required = Required.Always)]
    public ApiLayer ApiLayer { get; private set; }
}

public sealed class ApiLayer {
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; private set; }

    [JsonProperty("library_path", Required = Required.Always)]
    public string LibraryPath { get; private set; }

    [JsonProperty("api_version", Required = Required.Always)]
    public string ApiVersion { get; private set; }

    [JsonProperty("implementation_version", Required = Required.Always)]
    public string ImplementationVersion { get; private set; }

    [JsonProperty("description", Required = Required.Always)]
    public string Description { get; private set; }

    [JsonProperty("functions")]
    public Dictionary<string, string> Functions { get; private set; }

    [JsonProperty("instance_extensions")]
    public List<InstanceExtension> InstanceExtensions { get; private set; }

    [JsonProperty("disable_environment")]
    public string DisableEnvironment { get; private set; }
    
    [JsonProperty("enable_environment")]
    public string EnableEnvironment { get; private set; }
}

public sealed class InstanceExtension {
    [JsonProperty("name")]
    public string Name { get; private set; }

    [JsonProperty("extension_version")]
    public object ExtensionVersion { get; private set; }

    [JsonProperty("entrypoints")]
    public List<string> Entrypoints { get; private set; }
}