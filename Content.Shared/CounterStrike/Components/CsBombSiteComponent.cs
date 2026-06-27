namespace Content.Shared.CounterStrike.Components;

/// <summary>
/// Invisible marker for a Counter-Strike bomb plant zone.
/// Only one bomb can be planted per site at a time.
/// </summary>
[RegisterComponent]
public sealed partial class CsBombSiteComponent : Component
{
    /// <summary>
    /// Optional label shown in mapping tools (e.g. "A", "B").
    /// </summary>
    [DataField]
    public string? Label;

    /// <summary>
    /// Whether a bomb is currently planted on this site.
    /// </summary>
    [DataField]
    public bool Occupied;

    /// <summary>
    /// The planted bomb entity, if any.
    /// </summary>
    [DataField]
    public EntityUid? PlantedBomb;
}
