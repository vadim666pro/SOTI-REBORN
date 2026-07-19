using Content.Server.Store.Systems;
using Content.Shared.CounterStrike;
using Content.Shared.CounterStrike.Components;
using Content.Shared.CounterStrike.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.CounterStrike.Systems;

/// <summary>
/// Manages Telecrystal economy for the Counter-Strike game mode.
/// Persists balance across subrounds and respawns.
/// Source of truth: CsRoundEconomyComponent.Telecrystals on the player body.
/// ONE-DIRECTIONAL sync: Component → StoreComponent.Balance only.
/// </summary>
public sealed class CsRoundEconomySystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("cs-economy");

    /// <summary>
    /// TC saved before respawn. Keyed by NetUserId.
    /// </summary>
    private readonly Dictionary<NetUserId, int> _playerTc = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CsSubRoundEndedEvent>(OnSubRoundEnded);
    }

    /// <summary>
    /// Initialize economy for a player body. Called on spawn.
    /// Sets starting TC, creates StoreComponent, and syncs.
    /// </summary>
    public void InitializePlayer(EntityUid bodyUid, string team)
    {
        if (!TryComp(bodyUid, out CsRoundEconomyComponent? economy))
            return;

        economy.Telecrystals = CsRoundControllerComponent.StartingTC;
        Dirty(bodyUid, economy);

        var store = EnsureComp<StoreComponent>(bodyUid);
        store.Categories = team switch
        {
            "CT" or "КТ" => new HashSet<ProtoId<StoreCategoryPrototype>>
            {
                "UplinkPrimaryCT",
                "UplinkSecondaryCT",
                "UplinkEquipmentCSGO"
            },
            _ => new HashSet<ProtoId<StoreCategoryPrototype>>
            {
                "UplinkPrimaryT",
                "UplinkSecondaryT",
                "UplinkEquipmentCSGO"
            }
        };
        store.CurrencyWhitelist = new HashSet<ProtoId<CurrencyPrototype>> { "Telecrystal" };
        store.Balance = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>
        {
            ["Telecrystal"] = FixedPoint2.New(CsRoundControllerComponent.StartingTC)
        };
        store.Name = team switch
        {
            "CT" or "КТ" => "ZAKUP CT",
            _ => "ZARUP T"
        };
        Dirty(bodyUid, store);
        Sawmill.Info($"[CS Economy] Initialized {ToPrettyString(bodyUid)} with {economy.Telecrystals} TC (team: {team})");
    }

    /// <summary>
    /// Get current TC balance for a player.
    /// </summary>
    public int GetBalance(EntityUid bodyUid)
    {
        if (TryComp(bodyUid, out CsRoundEconomyComponent? economy))
            return economy.Telecrystals;
        return CsRoundControllerComponent.StartingTC;
    }

    /// <summary>
    /// Try to spend TC. Returns true if successful.
    /// </summary>
    public bool TrySpendCoins(EntityUid bodyUid, int amount)
    {
        if (!TryComp(bodyUid, out CsRoundEconomyComponent? economy))
            return false;

        if (economy.Telecrystals < amount)
            return false;

        economy.Telecrystals -= amount;
        Dirty(bodyUid, economy);
        SyncToUplink(bodyUid, economy.Telecrystals);
        Sawmill.Info($"[CS Economy] {ToPrettyString(bodyUid)}: spent {amount} TC. Balance: {economy.Telecrystals}");
        return true;
    }

    /// <summary>
    /// Add TC to the player's balance.
    /// </summary>
    public void AddCoins(EntityUid bodyUid, int amount)
    {
        if (!TryComp(bodyUid, out CsRoundEconomyComponent? economy))
            return;

        var oldTc = economy.Telecrystals;
        economy.Telecrystals = Math.Clamp(economy.Telecrystals + amount, 0, CsRoundControllerComponent.MaxTC);
        Dirty(bodyUid, economy);
        SyncToUplink(bodyUid, economy.Telecrystals);
        Sawmill.Info($"[CS Economy] {ToPrettyString(bodyUid)}: {oldTc} +{amount} TC. Balance: {economy.Telecrystals}");
    }

    /// <summary>
    /// ONE-DIRECTIONAL: write component balance to player's StoreComponent.
    /// </summary>
    private void SyncToUplink(EntityUid bodyUid, int tc)
    {
        if (!TryComp<StoreComponent>(bodyUid, out var store))
            return;
        if (!store.Balance.ContainsKey("Telecrystal"))
            return;
        store.Balance["Telecrystal"] = FixedPoint2.New(tc);
        Dirty(bodyUid, store);
    }

    /// <summary>
    /// Save TC before respawn.
    /// </summary>
    public void SaveTcForRespawn(EntityUid bodyUid, NetUserId userId)
    {
        var savedTc = GetBalance(bodyUid);
        _playerTc[userId] = savedTc;
        Sawmill.Info($"[CS Economy] Saved {savedTc} TC for {ToPrettyString(bodyUid)} before respawn.");
    }

    /// <summary>
    /// Restore TC from dictionary to newly spawned players and sync uplink.
    /// Called during FreezeTime to handle async respawns.
    /// </summary>
    public void RestorePlayerTc()
    {
        if (_playerTc.Count == 0)
            return;

        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent>();
        while (query.MoveNext(out var bodyUid, out _, out var mindContainer))
        {
            if (!mindContainer.HasMind)
                continue;

            var mindId = mindContainer.Mind!.Value;

            if (!TryComp(mindId, out MindComponent? mind))
                continue;

            if (mind.UserId is not { } userId)
                continue;

            if (!_playerTc.TryGetValue(userId, out var savedTc))
                continue;

            // Ensure the component exists on new body, then set balance
            var economy = EnsureComp<CsRoundEconomyComponent>(bodyUid);
            if (economy.Telecrystals == savedTc && HasComp<StoreComponent>(bodyUid))
            {
                _playerTc.Remove(userId);
                continue;
            }

            economy.Telecrystals = savedTc;
            Dirty(bodyUid, economy);

            if (!HasComp<StoreComponent>(bodyUid))
            {
                var team = GetPlayerTeam(mindId);
                if (team != null)
                    InitializePlayer(bodyUid, team);
            }
            else
            {
                SyncToUplink(bodyUid, savedTc);
            }

            _playerTc.Remove(userId);
            Sawmill.Info($"[CS Economy] Restored {savedTc} TC for {ToPrettyString(bodyUid)}");
        }
    }

    /// <summary>
    /// Clear all saved TC data (called on round reset).
    /// </summary>
    public void ClearSavedTc()
    {
        _playerTc.Clear();
    }

    /// <summary>
    /// Handle sub-round end: award TC to all players based on team win/loss.
    /// </summary>
    private void OnSubRoundEnded(CsSubRoundEndedEvent ev)
    {
        // Query minds directly — dead players whose bodies were deleted still have minds.
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            if (mind.UserId is not { } userId)
                continue;

            if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
                continue;

            bool isWinner = IsOnTeam(jobId.Value, ev.WinnerTeam);
            int bonus = isWinner
                ? CsRoundControllerComponent.WinBonusTC
                : CsRoundControllerComponent.LossBonusTC;

            // Try to add coins to the player's current body if alive
            if (mind.CurrentEntity is { } currentBody && TryComp(currentBody, out CsRoundEconomyComponent? _))
            {
                AddCoins(currentBody, bonus);
                Sawmill.Info($"[CS Economy] {ToPrettyString(currentBody)}: sub-round ended, winner={isWinner}, +{bonus} TC");
            }
            else
            {
                // Body deleted or no economy component — update saved TC so respawn gets the bonus
                if (_playerTc.TryGetValue(userId, out var saved))
                {
                    var newAmount = Math.Clamp(saved + bonus, 0, CsRoundControllerComponent.MaxTC);
                    _playerTc[userId] = newAmount;
                    Sawmill.Info($"[CS Economy] {userId}: sub-round ended (no body), winner={isWinner}, saved TC {saved} -> {newAmount}");
                }
                else
                {
                    // No saved TC yet (first round, player wasn't respawned) — save startingTC + bonus
                    var startingWithBonus = Math.Clamp(CsRoundControllerComponent.StartingTC + bonus, 0, CsRoundControllerComponent.MaxTC);
                    _playerTc[userId] = startingWithBonus;
                    Sawmill.Info($"[CS Economy] {userId}: sub-round ended (no body, no save), winner={isWinner}, saved TC = {startingWithBonus}");
                }
            }
        }
    }

    /// <summary>
    /// Determine a player's team from their mind's job ID.
    /// </summary>
    private string? GetPlayerTeam(EntityUid mindId)
    {
        if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
            return null;
        if (CounterStrikeTeams.CtJobs.Contains(jobId.Value))
            return "CT";
        if (CounterStrikeTeams.TJobs.Contains(jobId.Value))
            return "T";
        return null;
    }

    /// <summary>
    /// Check if a job belongs to the specified team.
    /// </summary>
    private static bool IsOnTeam(ProtoId<JobPrototype> jobId, string team)
    {
        if (team == "КТ" || team == "CT")
            return CounterStrikeTeams.CtJobs.Contains(jobId);
        return CounterStrikeTeams.TJobs.Contains(jobId);
    }
}
