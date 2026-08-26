namespace Content.Shared.CounterStrike.Components;

/// <summary>
/// Temporary marker component added to player entities during FreezeTime.
/// While present, the entity's movement speed is zeroed.
/// Removed when transitioning to ActionPhase.
/// </summary>
[RegisterComponent]
public sealed partial class CsFrozenComponent : Component
{
}
