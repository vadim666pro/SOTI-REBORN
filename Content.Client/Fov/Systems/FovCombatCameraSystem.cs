using Content.Client.Movement.Components;
using Content.Shared.CombatMode;
using Robust.Client.Player;

namespace Content.Client.Fov.Systems;

/// <summary>
/// Toggles EyeCursorOffsetComponent.Enabled based on Harm Mode.
/// Camera offset follows cursor only during combat.
/// </summary>
public sealed class FovCombatCameraSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var playerUid = _player.LocalEntity;
        if (playerUid == null)
            return;

        var comp = EnsureComp<EyeCursorOffsetComponent>(playerUid.Value);
        comp.Enabled = _combatMode.IsInCombatMode(playerUid.Value);
    }
}
