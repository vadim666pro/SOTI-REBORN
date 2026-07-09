using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Hitscan entities that have this component will cause an EMP pulse when striking a target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanEmpEffectComponent : Component
{
    /// <summary>
    /// The range of the EMP pulse
    /// </summary>
    [DataField]
    public float Range = 1.0f;

    /// <summary>
    /// How much energy will be consumed per battery in range
    /// </summary>
    [DataField]
    public float EnergyConsumption;

    /// <summary>
    /// How long it disables targets in seconds
    /// </summary>
    [DataField]
    public TimeSpan DisableDuration = TimeSpan.FromSeconds(60);
}
