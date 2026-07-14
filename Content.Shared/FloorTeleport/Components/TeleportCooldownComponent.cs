using Robust.Shared.GameStates;

namespace Content.Shared.FloorTeleport.Components;

/// <summary>
///     Temporary component added to entities after teleportation to prevent instant re-teleport.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleportCooldownComponent : Component
{
    /// <summary>
    ///     When this cooldown expires (game time in seconds).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float ExpiresAt;
}
