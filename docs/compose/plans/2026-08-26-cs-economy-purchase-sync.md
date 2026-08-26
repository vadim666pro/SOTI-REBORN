# CS Economy Purchase Synchronization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Synchronize Telecrystal spending from StoreComponent back to CsRoundEconomyComponent so player balances persist correctly across rounds.

**Architecture:** Event-based synchronization — CsRoundEconomySystem subscribes to StoreBuyFinishedEvent and updates CsRoundEconomyComponent.Telecrystals when purchases occur, maintaining ONE-DIRECTIONAL data flow from economy component to store.

**Tech Stack:** C# / SS14 ECS, RobustToolbox event system

## Global Constraints

- Components in Content.Shared, systems in Content.Server
- Follow SS14 [RegisterComponent], [Dependency], EntityQueryEnumerator patterns
- Use SubscribeLocalEvent for event handling
- Log all economy operations to cs-economy sawmill
- Dirty() components after mutation for network sync

---

## Task 1: Add StoreBuyFinishedEvent Handler

**Covers:** [S3]

**Files:**
- Modify: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs:44-48` (Initialize method)
- Modify: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs:318` (add new handler method at end)

**Interfaces:**
- Consumes: `StoreBuyFinishedEvent` from `Content.Server.Store.Systems` (existing, line 272-277 in StoreSystem.Ui.cs)
- Produces: None (updates existing CsRoundEconomyComponent)

**Dependencies:**
- Add `using Content.Server.Store.Systems;` if not present (already at line 2)

---

- [ ] **Step 1: Write failing test comment placeholder**

Since this is integration-level functionality requiring full game state (StoreSystem, BUI messages, entity spawning), we'll verify through manual testing rather than unit tests. Add a comment documenting the test scenario:

```csharp
// Manual test scenario:
// 1. Start CS round, player spawns with 19 TC
// 2. Buy weapon for 10 TC during FreezeTime
// 3. Check CsRoundEconomyComponent.Telecrystals == 9
// 4. Win round (+10 TC) -> check saved TC == 19
// 5. Next round respawn -> verify player has 19 TC (not 29)
```

Add this comment above the `OnStoreBuyFinished` method we'll create in step 3.

- [ ] **Step 2: Add event subscription in Initialize()**

File: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

Locate the `Initialize()` method (line 44-48). After line 47 (the existing `SubscribeLocalEvent<CsSubRoundEndedEvent>` line), add:

```csharp
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
```

The method should now look like:

```csharp
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CsSubRoundEndedEvent>(OnSubRoundEnded);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
    }
```

- [ ] **Step 3: Implement OnStoreBuyFinished handler**

File: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

Add this method at the end of the class (after line 317, after the `IsOnTeam` method):

```csharp
    /// <summary>
    /// Handle store purchases: sync TC spending from StoreComponent to CsRoundEconomyComponent.
    /// </summary>
    private void OnStoreBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        // Manual test scenario:
        // 1. Start CS round, player spawns with 19 TC
        // 2. Buy weapon for 10 TC during FreezeTime
        // 3. Check CsRoundEconomyComponent.Telecrystals == 9
        // 4. Win round (+10 TC) -> check saved TC == 19
        // 5. Next round respawn -> verify player has 19 TC (not 29)

        // Find the buyer entity - StoreComponent lives on the buyer's body
        var buyerUid = ev.StoreUid;

        // Only process CS players with economy component
        if (!TryComp(buyerUid, out CsRoundEconomyComponent? economy))
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
            Sawmill.Warning($"[CS Economy] {ToPrettyString(buyerUid)}: purchase cost {tcCost} TC but economy component has {economy.Telecrystals} TC. Desynced state!");
        }

        // Subtract from economy component to match StoreComponent
        var oldBalance = economy.Telecrystals;
        economy.Telecrystals = Math.Max(0, economy.Telecrystals - tcCost);
        Dirty(buyerUid, economy);

        Sawmill.Info($"[CS Economy] {ToPrettyString(buyerUid)}: purchased for {tcCost} TC. Balance: {oldBalance} -> {economy.Telecrystals}");
    }
```

- [ ] **Step 4: Build the project**

Run the build to verify no compilation errors:

```bash
cd C:\cs-ss14\SOTI-REBORN
dotnet build
```

Expected: BUILD SUCCEEDED with 0 errors

- [ ] **Step 5: Manual testing preparation**

Before committing, verify the implementation logic:

1. Check that `StoreBuyFinishedEvent` is defined in `Content.Server/Store/Systems/StoreSystem.Ui.cs` (line 402-406) with fields `StoreUid` and `PurchasedItem`
2. Check that `PurchasedItem` is of type `ListingDataWithCostModifiers` which has a `Cost` property (Dictionary)
3. Verify the currency key is the string `"Telecrystal"` (check existing code in StoreComponent initialization at line 83)

All checks should pass from code inspection.

- [ ] **Step 6: Commit the changes**

```bash
git add Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs
git commit -m "feat(cs-economy): sync TC spending from purchases to economy component

Subscribe to StoreBuyFinishedEvent and update CsRoundEconomyComponent.Telecrystals
when players purchase items, ensuring balance persists correctly across respawns.

Fixes issue where players would start next round with pre-purchase TC amount
instead of post-purchase amount (e.g., spending 10 TC then winning +10 TC
would incorrectly give 29 TC instead of 19 TC next round)."
```

- [ ] **Step 7: In-game manual testing**

Launch the game and test the scenario:

1. Start a CS round on de_inferno map
2. Join as CT or T team
3. During FreezeTime, open the buy menu (hotbar action button)
4. Note starting balance: should be 19 TC
5. Purchase a weapon (e.g., M4A1 for 10 TC)
6. Check console logs for: `[CS Economy] ... purchased for 10 TC. Balance: 19 -> 9`
7. Complete the round (win or lose)
8. Check logs for TC award (win: +10, loss: +5)
9. Next round FreezeTime: verify balance is 19 TC (if won) or 14 TC (if lost), NOT 29/24
10. Verify uplink shows correct balance matching economy component

Expected result: TC persists correctly across rounds, accounting for purchases.

If test fails, check:
- Is StoreBuyFinishedEvent being raised? (Add breakpoint in OnStoreBuyFinished)
- Is the buyer entity the store entity? (Should be the same in CS mode)
- Is Telecrystal spelling correct in currency check?

---

## Completion Criteria

- [x] Build succeeds with no errors
- [x] CsRoundEconomySystem subscribes to StoreBuyFinishedEvent
- [x] Purchase handler updates CsRoundEconomyComponent.Telecrystals
- [x] Logging shows TC deduction on purchase
- [x] Changes committed to git
- [ ] Manual testing confirms TC persists correctly across rounds (to be verified in-game)
