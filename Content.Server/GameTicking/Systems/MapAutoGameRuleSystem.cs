using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Systems;

/// <summary>
/// Reads MapAutoGameRuleComponent from the map and automatically adds/starts
/// the configured GameRules on the appropriate run levels.
/// </summary>
public sealed class MapAutoGameRuleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    private static readonly ISawmill Sawmill = Logger.GetSawmill("map-auto-gamerule");

    private bool _added;
    private bool _started;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        // Process current level in case this system initializes mid-round
        HandleLevel(_gameTicker.RunLevel);
    }

    private bool TryGetConfig([NotNullWhen(true)] out MapAutoGameRuleComponent? comp)
    {
        comp = EntityQuery<MapAutoGameRuleComponent>().FirstOrDefault();
        return comp != null && comp.Rules.Count > 0;
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        HandleLevel(ev.New);
    }

    private void HandleLevel(GameRunLevel level)
    {
        if (!TryGetConfig(out var comp))
            return;

        switch (level)
        {
            case GameRunLevel.PreRoundLobby:
                if (!_added && comp.AddInLobby)
                {
                    Sawmill.Info($"[MAGR] Adding rules in lobby: [{string.Join(", ", comp.Rules)}]");
                    foreach (var id in comp.Rules)
                    {
                        _gameTicker.AddGameRule(id);
                        Sawmill.Info($"[MAGR] Added rule '{id}' in lobby.");
                    }
                    _added = true;
                }
                _started = false; // reset
                break;

            case GameRunLevel.InRound:
                if (!_started && comp.StartOnRoundStart)
                {
                    Sawmill.Info($"[MAGR] Starting rules at InRound: [{string.Join(", ", comp.Rules)}]");
                    foreach (var id in comp.Rules)
                    {
                        // StartGameRule(string) internally calls AddGameRule, so we only need this one call.
                        _gameTicker.StartGameRule(id);
                        Sawmill.Info($"[MAGR] Started rule '{id}'.");
                    }
                    _added = true;
                    _started = true;
                }
                break;

            default:
                break;
        }
    }
}
