using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Component that stores damage sprites to be displayed on screen overlay.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageSpriteComponent : Component
{
    /// <summary>
    /// List of active damage sprites with their properties.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<DamageSpriteData> Sprites = new();

    /// <summary>
    /// Last time the sprite position was changed for this entity, in Unix milliseconds.
    /// Used to enforce a cooldown before the position can change again. Stored as a long
    /// because DateTimeOffset is not serializable by the network serializer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public long LastPositionChangeMs = 0;

    /// <summary>
    /// Last position used for the sprite (normalized 0-1 coordinates).
    /// Kept so we can reuse the same position until the cooldown expires.
    /// </summary>
    [DataField, AutoNetworkedField]
    public System.Numerics.Vector2 LastPosition = new(0.5f, 0.5f);
}

/// <summary>
/// Data for a single damage sprite.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class DamageSpriteData
{
    /// <summary>
    /// Position of the sprite (normalized 0-1).
    /// </summary>
    [DataField]
    public Vector2 Position = Vector2.Zero;

    /// <summary>
    /// Rotation of the sprite in degrees.
    /// </summary>
    [DataField]
    public float Rotation = 0f;

    /// <summary>
    /// Current opacity (0-1).
    /// </summary>
    [DataField]
    public float Opacity = 1f;

    /// <summary>
    /// Scale of the sprite.
    /// </summary>
    [DataField]
    public float Scale = 1f;

    /// <summary>
    /// Time remaining before the sprite disappears.
    /// </summary>
    [DataField]
    public TimeSpan RemainingTime = TimeSpan.Zero;

    /// <summary>
    /// Total lifetime of the sprite.
    /// </summary>
    [DataField]
    public TimeSpan TotalLifetime = TimeSpan.Zero;

    /// <summary>
    /// Index of the sprite state to use (client maps to state name).
    /// This prevents picking a new RSI state every frame which caused flicker.
    /// </summary>
    [DataField]
    public int StateIndex = 0;
}
