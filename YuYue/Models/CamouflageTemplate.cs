namespace YuYue.Models;

/// <summary>
/// Defines a preset configuration for camouflage views.
/// </summary>
public sealed class CamouflageTemplate
{
    public CamouflageTemplate(string key, string displayName, string description)
    {
        Key = key;
        DisplayName = displayName;
        Description = description;
    }

    public string Key { get; }

    public string DisplayName { get; }

    public string Description { get; }

    public override string ToString() => DisplayName;
}
