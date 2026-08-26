using Robust.Shared.GameStates;

namespace Content.Shared.CounterStrike.Components;

/// <summary>
/// Telecrystal economy attached to a player body entity.
/// Source of truth for TC balance. Synced to uplink StoreComponent.
/// Persists across respawns via _playerTc dictionary in the system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CsRoundEconomyComponent : Component
{
    /// <summary>
    /// Current Telecrystal balance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Telecrystals;
}
