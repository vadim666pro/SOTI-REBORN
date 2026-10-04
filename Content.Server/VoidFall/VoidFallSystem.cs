using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.VoidFall;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.VoidFall;

/// <summary>
/// Detects players standing on space tiles (out of bounds) and applies void fall.
/// After FallDuration seconds the entity is deleted.
/// </summary>
public sealed class VoidFallSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private const float CheckInterval = 0.1f;

    /// <summary>
    /// Подстроки в ID прототипа тайла, которые тоже считаются «пустотой».
    /// Проверка регистронезависимая.
    /// </summary>
    private static readonly string[] VoidTileIdSubstrings =
    {
        "FakeWall",
    };

    private float _accumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < CheckInterval)
            return;
        _accumulator = 0f;

        // Уже падающие: наносим радиационный урон,
        // либо снимаем компонент, если игрок выбрался из void.
        var fallQuery = EntityQueryEnumerator<VoidFallComponent, TransformComponent>();
        while (fallQuery.MoveNext(out var uid, out var fall, out var xform))
        {
            if (!IsInVoid(xform, uid))
            {
                RemComp<VoidFallComponent>(uid);
                continue;
            }

            fall.FallTime += frameTime;
            Dirty(uid, fall);

            // Apply radiation damage while falling
            var radiationDamage = new DamageSpecifier();
            radiationDamage.DamageDict.Add("Radiation", 500);
            _damageable.TryChangeDamage(uid, radiationDamage, ignoreResistances: true);
        }

        // Новые живые игроки, попавшие в void.
        var mobQuery = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (mobQuery.MoveNext(out var uid, out var mobState, out var xform))
        {
            if (!_mobState.IsAlive(uid, mobState))
                continue;

            if (HasComp<VoidFallComponent>(uid))
                continue;

            if (!IsInVoid(xform, uid))
                continue;

            EnsureComp<VoidFallComponent>(uid);
        }
    }

    /// <summary>
    /// Возвращает true, если сущность стоит на «пустоте»:
    /// нет грида, координаты за пределами грида, тайл пустой
    /// или ID тайла содержит одну из подстрок из <see cref="VoidTileIdSubstrings"/>.
    /// </summary>
    private bool IsInVoid(TransformComponent xform, EntityUid uid)
    {
        // Проверка на валидность координат
        if (!xform.Coordinates.IsValid(EntityManager))
            return true;

        if (xform.GridUid is not { } gridUid)
            return true;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return true;

        // Проверка на NaN/Infinity в координатах
        var coords = xform.Coordinates;
        if (float.IsNaN(coords.X) || float.IsInfinity(coords.X) ||
            float.IsNaN(coords.Y) || float.IsInfinity(coords.Y))
            return true;

        if (!_mapSystem.TryGetTileRef(gridUid, grid, xform.Coordinates, out var tileRef))
            return true;

        if (tileRef.Tile.IsEmpty)
            return true;

        if (TryGetTileDefinitionId(tileRef, out var tileId))
        {
            foreach (var substring in VoidTileIdSubstrings)
            {
                if (tileId.Contains(substring, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Пытается получить строковый ID прототипа тайла из <see cref="TileRef"/>.
    /// </summary>
    private bool TryGetTileDefinitionId(TileRef tileRef, out string tileId)
    {
        tileId = string.Empty;

        // В этой версии RobustToolbox ITileDefinitionManager находится в Robust.Shared.Map,
        // а ContentTileDefinition — в Content.Shared.Maps.
        if (_tileDefinitionManager[tileRef.Tile.TypeId] is not ContentTileDefinition definition)
            return false;

        tileId = definition.ID;
        return true;
    }
}
