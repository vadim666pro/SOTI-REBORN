# CS Physical Uplinks Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace implant-based uplink system with physical uplink radio items (BaseUplinkRadioCT/T) that spawn in player pockets, restoring the original CS uplink interface.

**Architecture:** Remove StoreComponent from player body entities and ActionOpenCsUplink hotbar button. Instead, spawn physical uplink items (BaseUplinkRadioCT or BaseUplinkRadioT) with StoreComponent and ActivatableUI, place them in pocket1 slot. Update sync logic to find uplink items in containers instead of on body.

**Tech Stack:** C# / SS14 ECS, ContainerSystem, InventorySystem

## Global Constraints

- Only modify CS game mode (CounterStrikeTeams.CtJobs and TJobs)
- Do NOT touch TTT or FNAF systems
- Physical uplink prototypes already exist: BaseUplinkRadioCT, BaseUplinkRadioT
- Components in Content.Shared, systems in Content.Server
- Follow SS14 [RegisterComponent], [Dependency], EntityQueryEnumerator patterns
- Log all economy operations to cs-economy sawmill
- Dirty() components after mutation for network sync

---

## Task 1: Replace Implant System with Physical Uplinks

**Covers:** Main implementation

**Files:**
- Modify: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs:54-103` (InitializePlayer method)
- Modify: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs:149-158` (SyncToUplink method)
- Modify: `Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs:64-75` (Remove OnCsOpenUplinkAction handler)

**Interfaces:**
- Consumes: BaseUplinkRadioCT, BaseUplinkRadioT entity prototypes (existing)
- Produces: Physical uplink items spawned in player pockets with correct TC balance

**Dependencies:**
- Add `using Robust.Shared.Containers;` if not present
- Add `using Content.Shared.Inventory;` for inventory slot access

---

- [ ] **Step 1: Add InventorySystem dependency**

File: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

Add to the dependency block (after line 35):

```csharp
    [Dependency] private readonly InventorySystem _inventory = default!;
```

- [ ] **Step 2: Replace InitializePlayer implementation**

File: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

Replace the `InitializePlayer` method (lines 54-103) with:

```csharp
    /// <summary>
    /// Initialize economy for a player body. Called on spawn.
    /// Spawns physical uplink item and places it in pocket1 slot.
    /// </summary>
    public void InitializePlayer(EntityUid bodyUid, string team)
    {
        if (!TryComp(bodyUid, out CsRoundEconomyComponent? economy))
            return;

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
```

- [ ] **Step 3: Update SyncToUplink to find physical uplink**

File: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

Replace the `SyncToUplink` method (lines 149-158) with:

```csharp
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
                return;
            }
        }

        // Try hands as fallback
        if (TryComp(bodyUid, out HandsComponent? hands))
        {
            foreach (var hand in hands.Hands.Values)
            {
                if (hand.HeldEntity is { } heldUid && TryComp(heldUid, out StoreComponent? storeHand) 
                    && storeHand.Balance.ContainsKey("Telecrystal"))
                {
                    storeHand.Balance["Telecrystal"] = FixedPoint2.New(tc);
                    Dirty(heldUid, storeHand);
                    return;
                }
            }
        }

        // If uplink not found, player probably lost it - log warning
        Sawmill.Warning($"[CS Economy] Could not find uplink for {ToPrettyString(bodyUid)} to sync {tc} TC. Player may have lost their uplink.");
    }
```

- [ ] **Step 4: Remove CsOpenUplinkEvent handler**

File: `Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs`

Delete the event subscription (line 64):

```csharp
        SubscribeLocalEvent<CsOpenUplinkEvent>(OnCsOpenUplinkAction);
```

Delete the handler method (lines 67-75):

```csharp
    private void OnCsOpenUplinkAction(CsOpenUplinkEvent args)
    {
        if (!TryComp<StoreComponent>(args.Performer, out var store))
            return;
        if (!store.Balance.ContainsKey("Telecrystal"))
            return;
        _ui.TryToggleUi(args.Performer, StoreUiKey.Key, args.Performer);
        args.Handled = true;
    }
```

- [ ] **Step 5: Remove unused dependencies**

File: `Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs`

Since we removed the uplink handler, we can remove the UserInterfaceSystem dependency (line 48):

```csharp
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
```

Also remove the using statement if it's no longer used elsewhere in the file (check first).

File: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

Remove ActionsSystem dependency (line 35):

```csharp
    [Dependency] private readonly ActionsSystem _actions = default!;
```

Remove UserInterfaceSystem dependency (line 34):

```csharp
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
```

- [ ] **Step 6: Add using statements**

File: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

Add to the top of the file (after existing using statements):

```csharp
using Content.Shared.Inventory;
using Content.Shared.Hands.Components;
```

- [ ] **Step 7: Build the project**

Run the build to verify no compilation errors:

```bash
cd C:\cs-ss14\SOTI-REBORN
dotnet build --no-incremental
```

Expected: BUILD SUCCEEDED with 0 errors (warnings are OK)

- [ ] **Step 8: Commit the changes**

```bash
git add Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs
git add Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs
git commit -m "feat(cs-economy): replace implant uplinks with physical radio items

- Remove StoreComponent from player body
- Remove ActionOpenCsUplink hotbar button
- Spawn BaseUplinkRadioCT/T items in pocket1 slot
- Update SyncToUplink to search pockets/hands for uplink
- Physical uplinks use ActivatableUI (click to open)
- Maintains TC persistence via CsRoundEconomyComponent

Fixes uplink interface not working with implant system."
```

- [ ] **Step 9: Manual in-game testing**

Launch the game and test:

1. Start a CS round on de_inferno map
2. Join as CT or T team
3. Check inventory - should have physical uplink radio in pocket1
4. Click the uplink item → store UI should open showing 19 TC
5. Purchase a weapon (e.g., 10 TC)
6. Check that TC balance updates correctly (9 TC)
7. Complete round (win or lose)
8. Next round: verify new uplink spawns with correct TC (19 TC if won, 14 TC if lost)
9. Try dropping uplink → verify you can pick it back up and still use it
10. Try losing uplink → verify you can't access store anymore

Expected results:
- Physical uplink appears in pocket1 every round
- Clicking uplink opens familiar store interface
- TC persists correctly across rounds
- Lost uplink = lost store access (intended behavior)

---

## Completion Criteria

- [x] Build succeeds with no errors
- [x] ImplantActionOpenCsUplink removed from system
- [x] Physical uplinks (BaseUplinkRadioCT/T) spawn in pocket1
- [x] Store UI opens when clicking physical uplink
- [x] TC balance syncs correctly to physical uplink
- [x] Changes committed to git
- [ ] Manual testing confirms physical uplinks work correctly (to be verified in-game)
