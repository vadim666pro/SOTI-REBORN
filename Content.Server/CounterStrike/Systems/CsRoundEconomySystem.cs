using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.CounterStrike;
using Content.Shared.CounterStrike.Components;
using Content.Shared.CounterStrike.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Server.StoreDiscount.Systems;
using Robust.Server.GameObjects;
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
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("cs-economy");

    /// <summary>
    /// TC saved before respawn. Keyed by NetUserId.
    /// </summary>
    private readonly Dictionary<NetUserId, int> _playerTc = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CsSubRoundEndedEvent>(OnSubRoundEnded);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
    }

    /// <summary>
    /// Initialize economy for a player body. Called on spawn.
    /// Spawns physical uplink item and places it in pocket1 slot.
    /// </summary>
    public void InitializePlayer(EntityUid bodyUid, string team)
    {
        var economy = EnsureComp<CsRoundEconomyComponent>(bodyUid);

        economy.Telecrystals = CsRoundControllerComponent.StartingTC;
        Dirty(bodyUid, economy);

        // Spawn the appropriate physical uplink based on team
        var uplinkProto = team switch
        {
            "CT" or "КТ" => "BaseUplinkRadioCT",
            _ => "BaseUplinkRadioT"
        };

        var uplinkUid = Spawn(uplinkProto, Transform(bodyUid).Coordinates);

        // Update the uplink's store balance to match economy component
        if (TryComp(uplinkUid, out StoreComponent? store))
        {
            store.Balance["Telecrystal"] = FixedPoint2.New(economy.Telecrystals);
            Dirty(uplinkUid, store);
        }

        // Try to place uplink in pocket1 slot
        if (!_inventory.TryGetSlotEntity(bodyUid, "pocket1", out _))
        {
            if (!_inventory.TryEquip(bodyUid, uplinkUid, "pocket1", force: true))
            {
                // Fallback: place in hands if pocket1 failed
                _hands.TryPickup(bodyUid, uplinkUid);
                Sawmill.Warning($"[CS Economy] Could not place uplink in pocket1 for {ToPrettyString(bodyUid)}, placed in hands");
            }
        }
        else
        {
            // Pocket1 occupied, try pocket2
            if (!_inventory.TryEquip(bodyUid, uplinkUid, "pocket2", force: true))
            {
                // Fallback: place in hands
                _hands.TryPickup(bodyUid, uplinkUid);
                Sawmill.Warning($"[CS Economy] Could not place uplink in pockets for {ToPrettyString(bodyUid)}, placed in hands");
            }
        }

        Sawmill.Info($"[CS Economy] Initialized {ToPrettyString(bodyUid)} with physical uplink ({uplinkProto}) containing {economy.Telecrystals} TC (team: {team})");
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
    /// ONE-DIRECTIONAL: write component balance to player's physical uplink StoreComponent.
    /// Searches in pockets and hands for an item with StoreComponent.
    /// </summary>
    private void SyncToUplink(EntityUid bodyUid, int tc)
    {
        // Try to find uplink in pocket1
        if (_inventory.TryGetSlotEntity(bodyUid, "pocket1", out var pocket1Item))
        {
            if (TryComp(pocket1Item, out StoreComponent? store1) && store1.Balance.ContainsKey("Telecrystal"))
            {
                store1.Balance["Telecrystal"] = FixedPoint2.New(tc);
                Dirty(pocket1Item.Value, store1);
                _store.UpdateUserInterface(bodyUid, pocket1Item.Value, store1);
                return;
            }
        }

        // Try pocket2
        if (_inventory.TryGetSlotEntity(bodyUid, "pocket2", out var pocket2Item))
        {
            if (TryComp(pocket2Item, out StoreComponent? store2) && store2.Balance.ContainsKey("Telecrystal"))
            {
                store2.Balance["Telecrystal"] = FixedPoint2.New(tc);
                Dirty(pocket2Item.Value, store2);
                _store.UpdateUserInterface(bodyUid, pocket2Item.Value, store2);
                return;
            }
        }

        // Try hands as fallback
        if (TryComp(bodyUid, out HandsComponent? hands))
        {
            foreach (var heldUid in _hands.EnumerateHeld((bodyUid, hands)))
            {
                if (TryComp(heldUid, out StoreComponent? storeHand) && storeHand.Balance.ContainsKey("Telecrystal"))
                {
                    storeHand.Balance["Telecrystal"] = FixedPoint2.New(tc);
                    Dirty(heldUid, storeHand);
                    _store.UpdateUserInterface(bodyUid, heldUid, storeHand);
                    return;
                }
            }
        }

        // If uplink not found, player probably lost it - log warning
        Sawmill.Warning($"[CS Economy] Could not find uplink for {ToPrettyString(bodyUid)} to sync {tc} TC. Player may have lost their uplink.");
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
        // First pass: restore TC for respawned players
        if (_playerTc.Count > 0)
        {
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
                    {
                        InitializePlayer(bodyUid, team);
                        // InitializePlayer resets TC to StartingTC — restore saved amount
                        economy.Telecrystals = savedTc;
                        Dirty(bodyUid, economy);
                        SyncToUplink(bodyUid, savedTc);
                    }
                }
                else
                {
                    SyncToUplink(bodyUid, savedTc);
                }

                _playerTc.Remove(userId);
                Sawmill.Info($"[CS Economy] Restored {savedTc} TC for {ToPrettyString(bodyUid)}");
            }
        }

        // Second pass: initialize first-time players who have no economy component
        var initQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent>();
        while (initQuery.MoveNext(out var bodyUid, out _, out var mindContainer))
        {
            if (!mindContainer.HasMind)
                continue;

            if (HasComp<CsRoundEconomyComponent>(bodyUid))
                continue;

            var mindId = mindContainer.Mind!.Value;
            var team = GetPlayerTeam(mindId);
            if (team == null)
                continue;

            InitializePlayer(bodyUid, team);
            Sawmill.Info($"[CS Economy] First-time init for {ToPrettyString(bodyUid)} (team={team})");
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

    /// <summary>
    /// Handle store purchases: sync TC spending from StoreComponent to CsRoundEconomyComponent.
    /// </summary>
    private void OnStoreBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        // ev.StoreUid is the uplink entity (with StoreComponent), NOT the player.
        // We need to find the player who owns this uplink.
        var uplinkUid = ev.StoreUid;
        EntityUid? ownerUid = null;

        // Search all players' inventory for this uplink
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MindContainerComponent>();
        while (query.MoveNext(out var bodyUid, out _, out var mindContainer))
        {
            if (!mindContainer.HasMind)
                continue;

            // Check pocket1
            if (_inventory.TryGetSlotEntity(bodyUid, "pocket1", out var pocket1) && pocket1 == uplinkUid)
            {
                ownerUid = bodyUid;
                break;
            }

            // Check pocket2
            if (_inventory.TryGetSlotEntity(bodyUid, "pocket2", out var pocket2) && pocket2 == uplinkUid)
            {
                ownerUid = bodyUid;
                break;
            }

            // Check hands
            if (TryComp(bodyUid, out HandsComponent? hands))
            {
                foreach (var heldUid in _hands.EnumerateHeld((bodyUid, hands)))
                {
                    if (heldUid == uplinkUid)
                    {
                        ownerUid = bodyUid;
                        break;
                    }
                }
                if (ownerUid != null) break;
            }
        }

        if (ownerUid == null)
        {
            Sawmill.Warning($"[CS Economy] StoreBuyFinished for uplink {ToPrettyString(uplinkUid)} but could not find owner.");
            return;
        }

        // Only process CS players with economy component
        if (!TryComp(ownerUid.Value, out CsRoundEconomyComponent? economy))
            return;

        // Calculate Telecrystal cost
        var tcCost = 0;
        foreach (var (currency, amount) in ev.PurchasedItem.Cost)
        {
            if (currency == "Telecrystal")
            {
                tcCost = (int)amount;
                break;
            }
        }

        // No TC cost means this purchase doesn't affect CS economy
        if (tcCost <= 0)
            return;

        // Sanity check: economy component should have enough TC
        // (StoreSystem already verified StoreComponent.Balance)
        if (economy.Telecrystals < tcCost)
        {
            Sawmill.Warning($"[CS Economy] {ToPrettyString(ownerUid.Value)}: purchase cost {tcCost} TC but economy component has {economy.Telecrystals} TC. Desynced state!");
        }

        // Subtract from economy component to match StoreComponent
        var oldBalance = economy.Telecrystals;
        economy.Telecrystals = Math.Max(0, economy.Telecrystals - tcCost);
        Dirty(ownerUid.Value, economy);

        Sawmill.Info($"[CS Economy] {ToPrettyString(ownerUid.Value)}: purchased for {tcCost} TC. Balance: {oldBalance} -> {economy.Telecrystals}");
    }
}
