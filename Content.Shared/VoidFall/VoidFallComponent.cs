using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.VoidFall;

/// <summary>
/// Attached to players who are currently falling into the void (out of bounds).
/// Triggers shrink animation and entity deletion.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VoidFallComponent : Component
{
    /// <summary>
    /// How long the player has been falling (seconds).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FallTime;

    /// <summary>
    /// Total duration of the fall before deletion (seconds).
    /// </summary>
    [DataField]
    public float FallDuration = 2f;

    /// <summary>
    /// Original sprite scale before falling started.
    /// </summary>
    [DataField]
    public Vector2 OriginalScale = Vector2.One;

    /// <summary>
    /// Target scale at the end of the fall (shrink to this).
    /// </summary>
    [DataField]
    public Vector2 AnimationScale = Vector2.Zero;

    /// <summary>
    /// Whether the fall animation has started.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AnimationStarted;
}
