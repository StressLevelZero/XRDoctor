using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SLZ.XRDoctor;

public sealed class VulkanLayerManifest {
    [JsonProperty("file_format_version", Required = Required.Always)]
    public string FileFormatVersion { get; private set; }

    [JsonProperty("layer")]
    public VulkanLayer Layer { get; private set; }
    
    [JsonProperty("layers")]
    public List<VulkanLayer> Layers { get; private set; }
}

public sealed class VulkanLayer {
    [JsonProperty("name")]
    public string Name { get; private set; } = null!;

    [JsonProperty("type")]
    public string Type { get; private set; } = null!;

    [JsonProperty("library_path")]
    public string LibraryPath { get; private set; } = null!;

    [JsonProperty("api_version")]
    public string ApiVersion { get; private set; } = null!;

    [JsonProperty("implementation_version")]
    public string ImplementationVersion { get; private set; } = null!;

    [JsonProperty("description")]
    public string Description { get; private set; } = null!;

    [JsonProperty("functions")]
    public Dictionary<string, string> Functions { get; private set; } = null!;

    [JsonProperty("instance_extensions")]
    public List<VulkanInstanceExtension> InstanceExtensions { get; private set; } = null!;

    [JsonProperty("device_extensions")]
    public List<VulkanDeviceExtension> DeviceExtensions { get; private set; } = null!;

    // Apparently malformed in Steam
    [JsonProperty("disable_environment")]
    public JToken DisableEnvironment { get; private set; } = null!;

    // Apparently malformed in Steam
    [JsonProperty("enable_environment")]
    public JToken EnableEnvironment { get; private set; } = null!;

    [JsonProperty("component_layers")]
    public List<string> ComponentLayers { get; private set; } = null!;

    [JsonProperty("pre_instance_functions")]
    public Dictionary<string, string> PreInstanceFunctions { get; private set; } = null!;

    [JsonConstructor]
    public VulkanLayer() { }
}

public sealed class VulkanInstanceExtension {
    [JsonProperty("name")]
    public string Name { get; private set; } = null!;

    [JsonProperty("spec_version")]
    public object SpecVersion { get; private set; } = null!;

    [JsonConstructor]
    public VulkanInstanceExtension() { }
}

public sealed class VulkanDeviceExtension {
    [JsonProperty("name")]
    public string Name { get; private set; } = null!;

    [JsonProperty("spec_version")]
    public object SpecVersion { get; private set; } = null!;

    [JsonProperty("entrypoints")]
    public List<string> Entrypoints { get; private set; } = null!;

    [JsonConstructor]
    public VulkanDeviceExtension() { }
}