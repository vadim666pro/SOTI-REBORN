using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Damage.Systems;

/// <summary>
/// Client-side system that adds damage sprites when the local player takes damage.
/// </summary>
public sealed class ClientDamageSpriteSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly DamageSpriteSystem _damageSpriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageSpriteSettingsComponent, ComponentInit>(OnSettingsInit);
        SubscribeLocalEvent<DamageSpriteSettingsComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnSettingsInit(EntityUid uid, DamageSpriteSettingsComponent component, ComponentInit args)
    {
        // Ensure component is initialized
    }

    private void OnDamageChanged(EntityUid uid, DamageSpriteSettingsComponent component, DamageChangedEvent args)
    {
        // Only add sprites for the local player
        if (_playerManager.LocalEntity != uid)
            return;

        // Check if damage was actually taken (not healed)
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= FixedPoint2.Zero)
            return;

        // Only show sprites for direct hits (have an origin entity). Environmental/indirect damage won't show.
        if (args.Origin == null)
            return;

        // Request the server to add the damage sprite so the server is authoritative and clients don't fight over state.
            var lifetimeMs = (int) TimeSpan.FromSeconds(1.5).TotalMilliseconds;
        RaiseNetworkEvent(new Content.Shared.Damage.Events.AddDamageSpriteRequest(GetNetEntity(uid), component.SpriteScale, lifetimeMs));
    }
}
