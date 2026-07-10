using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Hitscan.Components;

/// <summary>
/// Marks an entity as passable by hitscan raycasts.
/// The ray will deal damage and continue through this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HitscanPassthroughComponent : Component
{
}
