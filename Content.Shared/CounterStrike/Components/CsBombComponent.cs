using Robust.Shared.GameStates;

namespace Content.Shared.CounterStrike.Components;

/// <summary>
/// Counter-Strike C4 bomb. Can be planted only on <see cref="CsBombSiteComponent"/> tiles.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CsBombComponent : Component
{
    /// <summary>
    /// Seconds required to plant the bomb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PlantTime = 3f;

    /// <summary>
    /// Seconds required to defuse the bomb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DefuseTime = 10f;

    /// <summary>
    /// Whether the bomb has been planted on a site.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Planted;

    /// <summary>
    /// The bomb site this bomb is planted on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Site;
}
