using Content.Client.Gameplay;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Client.Sprite;

public sealed class SpriteFadeSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    // Радиус квадрата: 1 = 3x3 тайла.
    private const int FadeRadius = 1;

    // 5 сэмплов на тайл: центр + 4 под-угла.
    private const int SamplesPerTile = 5;
    private readonly List<(MapCoordinates Point, bool ExcludeBoundingBox)> _points =
        new((2 * FadeRadius + 1) * (2 * FadeRadius + 1) * SamplesPerTile + 1);

    private readonly HashSet<FadingSpriteComponent> _comps = new();

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<SpriteFadeComponent> _fadeQuery;
    private EntityQuery<FadingSpriteComponent> _fadingQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    private const float TargetAlpha = 0.4f;
    private const float ChangeRate = 1f;

    // Троттлинг: раз в 2 кадра для слабых ПК
    private const int UpdateInterval = 2;
    private int _frameCounter;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery   = GetEntityQuery<SpriteComponent>();
        _fadeQuery     = GetEntityQuery<SpriteFadeComponent>();
        _fadingQuery   = GetEntityQuery<FadingSpriteComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        SubscribeLocalEvent<FadingSpriteComponent, ComponentShutdown>(OnFadingShutdown);
    }

    private void OnFadingShutdown(EntityUid uid, FadingSpriteComponent component, ComponentShutdown args)
    {
        if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating ||
            !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(component.OriginalAlpha));
    }

    /// <summary>
    ///     Добавляет 5 сэмплов на каждый тайл в квадрате (2R+1)x(2R+1) вокруг точки.
    ///     ExcludeBoundingBox = false — иначе стены (у которых коллизия покрывает весь тайл)
    ///     будут пропускаться и не затемняться.
    /// </summary>
    private void AddSquareTiles(MapCoordinates center, int radius)
    {
        var centerTileX = (float)Math.Floor(center.Position.X);
        var centerTileY = (float)Math.Floor(center.Position.Y);

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                var tx = centerTileX + dx;
                var ty = centerTileY + dy;

                // Центр тайла
                _points.Add((new MapCoordinates(new Vector2(tx + 0.5f, ty + 0.5f), center.MapId), false));
                // 4 под-угла (с небольшим отступом, чтобы не выйти за пределы тайла)
                _points.Add((new MapCoordinates(new Vector2(tx + 0.2f, ty + 0.2f), center.MapId), false));
                _points.Add((new MapCoordinates(new Vector2(tx + 0.8f, ty + 0.2f), center.MapId), false));
                _points.Add((new MapCoordinates(new Vector2(tx + 0.2f, ty + 0.8f), center.MapId), false));
                _points.Add((new MapCoordinates(new Vector2(tx + 0.8f, ty + 0.8f), center.MapId), false));
            }
        }
    }

    private void FadeIn(float change)
    {
        var player = _playerManager.LocalEntity;
        if (player == null)
            return;

        if (_stateManager.CurrentState is not GameplayState state)
            return;

        _points.Clear();

        // Квадрат 3x3 вокруг курсора
        if (_uiManager.CurrentlyHovered is IViewportControl vp &&
            _inputManager.MouseScreenPosition.IsValid)
        {
            var mouseMapPos = vp.PixelToMap(_inputManager.MouseScreenPosition.Position);
            AddSquareTiles(mouseMapPos, FadeRadius);
        }

        // Точка под игроком
        if (TryComp(player.Value, out TransformComponent? playerXform))
        {
            _points.Add((_transform.GetMapCoordinates(player.Value, xform: playerXform), false));
        }

        if (!_spriteQuery.TryGetComponent(player, out var playerSprite))
            return;

        for (int p = 0; p < _points.Count; p++)
        {
            var (mapPos, excludeBB) = _points[p];

            foreach (var ent in state.GetClickableEntities(mapPos, excludeFaded: false))
            {
                if (ent == player ||
                    !_fadeQuery.HasComponent(ent) ||
                    !_spriteQuery.TryGetComponent(ent, out var sprite) ||
                    sprite.DrawDepth < playerSprite.DrawDepth)
                {
                    continue;
                }

                // Зарезервировано: если понадобится точечное поведение с проверкой коллизий.
                if (excludeBB && _fixturesQuery.TryComp(ent, out var body))
                {
                    var transform = _physics.GetPhysicsTransform(ent);
                    var collided = false;

                    foreach (var fixture in body.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        if (_fixtures.TestPoint(fixture.Shape, transform, mapPos.Position))
                        {
                            collided = true;
                            break;
                        }
                    }

                    if (collided)
                        continue;
                }

                if (!_fadingQuery.TryComp(ent, out var fading))
                {
                    fading = AddComp<FadingSpriteComponent>(ent);
                    fading.OriginalAlpha = sprite.Color.A;
                }

                _comps.Add(fading);
                var newColor = Math.Max(sprite.Color.A - change, TargetAlpha);

                if (sprite.Color.A > newColor)
                {
                    _sprite.SetColor((ent, sprite), sprite.Color.WithAlpha(newColor));
                }
            }
        }
    }

    private void FadeOut(float change)
    {
        var query = AllEntityQuery<FadingSpriteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_comps.Contains(comp))
                continue;

            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            var newColor = Math.Min(sprite.Color.A + change, comp.OriginalAlpha);

            if (newColor > sprite.Color.A)
            {
                _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(newColor));
            }
            else
            {
                RemCompDeferred<FadingSpriteComponent>(uid);
            }
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _frameCounter++;
        if (_frameCounter < UpdateInterval)
            return;
        _frameCounter = 0;

        var change = ChangeRate * frameTime * UpdateInterval;

        FadeIn(change);
        FadeOut(change);

        _comps.Clear();
    }
}