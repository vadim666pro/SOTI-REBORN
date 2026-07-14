using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.FloorTeleport.Components;

/// <summary>
///     Floor teleporter that teleports entities to a linked teleporter's position on step trigger.
///     Uses LinkedEntityComponent for linking (set UIDs in map file).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CollisionFloorTeleporterComponent : Component
{
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
