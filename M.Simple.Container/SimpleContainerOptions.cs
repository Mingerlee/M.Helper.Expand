namespace M.Simple.Container;

/// <summary>Controls validation. Both checks are enabled by default.</summary>
public sealed class SimpleContainerOptions
{
    public bool ValidateScopes { get; set; } = true;
    public bool ValidateOnBuild { get; set; } = true;
}
