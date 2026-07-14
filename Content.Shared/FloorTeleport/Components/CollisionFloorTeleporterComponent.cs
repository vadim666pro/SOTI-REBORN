using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.FloorTeleport.Components;

/// <summary>
///     Floor teleporter that teleports entities to a linked teleporter's position on step trigger.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CollisionFloorTeleporterComponent : Component
{
    /// <summary>
    ///     The prototype ID of the linked teleporter destination.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string LinkedTeleporterId = string.Empty;

    /// <summary>
    ///     Cooldown in seconds to prevent teleport loops.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float CooldownTime = 1.5f;

    /// <summary>
    ///     Sound to play when teleporting.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
