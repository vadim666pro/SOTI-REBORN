# Built-In Uplink Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace physical uplink radio items with a built-in uplink directly on the player entity, auto-open during FreezeTime, block during ActionPhase.

**Architecture:** StoreComponent is placed on the player entity instead of a separate item. CsRoundEconomySystem syncs TC directly to the player's StoreComponent. CsRoundControllerSystem manages auto-open/close of the StoreUI during phase transitions.

**Tech Stack:** C#, Robust ECS, SS14 Store system, YAML prototypes

## Global Constraints

- Language: Russian for in-game UI strings
- TC constants: StartingTC=19, MaxTC=100, WinBonusTC=10, LossBonusTC=5
- Phases: FreezeTime(15s) → ActionPhase(120s) → PostAction(10s)
- Store presets: StorePresetUplinkCT (CT categories), StorePresetUplinkT (T categories)

---

## File Map

| Action | File | Purpose |
|--------|------|---------|
| Modify | `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs` | Add StoreComponent to player, simplify SyncToUplink |
| Modify | `Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs` | Auto-open/close StoreUI on phase transitions |
| Modify | 6x job YAML files | Remove `pocket1: BaseUplinkRadioCT/T` from starting gear |
| Modify | `Resources/Prototypes/Store/presets.yml` | Add `currencyWhitelist` to CT/T presets (missing) |

---

### Task 1: Remove Physical Uplink Items from Starting Gear

**Covers:** Built-in uplink concept (no physical item needed)

**Files:**
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/musician.yml:20`
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/librarian.yml:20`
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/janitor.yml:19`
- Modify: `Resources/Prototypes/Roles/Jobs/Wildcards/reporter.yml:20`
- Modify: `Resources/Prototypes/Roles/Jobs/Wildcards/boxer.yml:22`
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/service_worker.yml:24`
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/mime.yml:23`
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/lawyer.yml:22`
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/chaplain.yml:21`
- Modify: `Resources/Prototypes/Roles/Jobs/Civilian/assistant.yml:19`

- [ ] **Step 1: Remove pocket1 uplink lines from all CT job YAML files**

Remove the `pocket1: BaseUplinkRadioCT` line from:
- `musician.yml` (line 20)
- `librarian.yml` (line 20)
- `janitor.yml` (line 19)
- `reporter.yml` (line 20)
- `boxer.yml` (line 22)

- [ ] **Step 2: Remove pocket1 uplink lines from all T job YAML files**

Remove the `pocket1: BaseUplinkRadioT` line from:
- `service_worker.yml` (line 24)
- `mime.yml` (line 23)
- `lawyer.yml` (line 22)
- `chaplain.yml` (line 21)
- `assistant.yml` (line 19)

- [ ] **Step 3: Verify no compile errors from removed references**

Run: `dotnet build Content.Server/Content.Server.csproj`
Expected: BUILD SUCCESSFUL (no references to removed items in C# code)

---

### Task 2: Add CurrencyWhitelist to CS Store Presets

**Covers:** Store preset configuration for built-in uplink

**Files:**
- Modify: `Resources/Prototypes/Store/presets.yml:40-68`

- [ ] **Step 1: Add currencyWhitelist to StorePresetUplinkCT**

In `presets.yml`, the `StorePresetUplinkCT` block (line 40) is missing `currencyWhitelist`. Add it:

```yaml
- type: entity
  id: StorePresetUplinkCT
  abstract: true
  components:
  - type: Store
    name: ZAKUP CT
    categories:
    - UplinkPrimaryCT
    - UplinkSecondaryCT
    - UplinkEquipmentCSGO
    currencyWhitelist:
    - Telecrystal
    balance:
      Telecrystal: 19
```

- [ ] **Step 2: Add currencyWhitelist to StorePresetUplinkT**

Same for `StorePresetUplinkT` (line 56):

```yaml
- type: entity
  id: StorePresetUplinkT
  abstract: true
  components:
  - type: Store
    name: ZARUP T
    categories:
    - UplinkPrimaryT
    - UplinkSecondaryT
    - UplinkEquipmentCSGO
    currencyWhitelist:
    - Telecrystal
    balance:
      Telecrystal: 19
```

- [ ] **Step 3: Commit**

```bash
git add Resources/Prototypes/Store/presets.yml
git commit -m "fix: add currencyWhitelist to CS store presets"
```

---

### Task 3: Modify CsRoundEconomySystem — Add StoreComponent to Player

**Covers:** Built-in uplink on player entity

**Files:**
- Modify: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

**Dependencies:** Task 2 (presets must have currencyWhitelist)

- [ ] **Step 1: Add required using statements**

Add to the top of `CsRoundEconomySystem.cs`:

```csharp
using Content.Server.Store.Systems;
using Content.Shared.Store;
using Robust.Shared.Player;
```

- [ ] **Step 2: Add StoreSystem dependency**

Add to the class dependencies:

```csharp
[Dependency] private readonly StoreSystem _store = default!;
[Dependency] private readonly UserInterfaceSystem _ui = default!;
[Dependency] private readonly IPlayerManager _playerManager = default!;
```

- [ ] **Step 3: Modify InitializePlayer to add StoreComponent**

Replace the `InitializePlayer` method:

```csharp
public void InitializePlayer(EntityUid bodyUid, string team)
{
    if (!TryComp(bodyUid, out CsRoundEconomyComponent? economy))
        return;

    economy.Telecrystals = CsRoundControllerComponent.StartingTC;
    Dirty(bodyUid, economy);

    // Add built-in uplink StoreComponent directly on the player
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

    Sawmill.Info($"[CS Economy] Initialized {ToPrettyString(bodyUid)} with {economy.Telecrystals} TC (built-in uplink, team={team})");
}
```

- [ ] **Step 4: Simplify SyncToUplink to work with player entity directly**

Replace the `SyncToUplink` method:

```csharp
private void SyncToUplink(EntityUid bodyUid, int tc)
{
    if (!TryComp<StoreComponent>(bodyUid, out var store))
        return;

    if (!store.Balance.ContainsKey("Telecrystal"))
        return;

    store.Balance["Telecrystal"] = FixedPoint2.New(tc);
    Dirty(bodyUid, store);
}
```

- [ ] **Step 5: Remove IsEntityInside helper (no longer needed)**

Delete the `IsEntityInside` method entirely — it was used to find stores inside container hierarchy, which is no longer needed.

- [ ] **Step 6: Update RestorePlayerTc to pass team info**

The `RestorePlayerTc` method needs to also set up the StoreComponent on respawned players. Modify it to call `InitializePlayer` for new bodies:

```csharp
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

        // Ensure built-in uplink exists on respawned player
        if (!HasComp<StoreComponent>(bodyUid))
        {
            var jobId = GetPlayerTeam(mindId);
            if (jobId != null)
                InitializePlayer(bodyUid, jobId);
        }

        SyncToUplink(bodyUid, savedTc);
        _playerTc.Remove(userId);
        Sawmill.Info($"[CS Economy] Restored {savedTc} TC for {ToPrettyString(bodyUid)}");
    }
}

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
```

- [ ] **Step 7: Build and verify**

Run: `dotnet build Content.Server/Content.Server.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 8: Commit**

```bash
git add Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs
git commit -m "feat: built-in uplink - StoreComponent on player entity"
```

---

### Task 4: Update CsRoundControllerSystem Callers

**Covers:** Passing team info to InitializePlayer

**Files:**
- Modify: `Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs`

**Dependencies:** Task 3 (InitializePlayer signature changed)

- [ ] **Step 1: Find all callers of InitializePlayer and update them**

The `InitializePlayer` method signature changed from `InitializePlayer(EntityUid)` to `InitializePlayer(EntityUid, string team)`. Find and update all call sites.

Search for `_economy.InitializePlayer(` in CsRoundControllerSystem.cs and any other files. Each call needs a team parameter derived from the player's job.

If `InitializePlayer` is called from CsRoundControllerSystem, determine the team from the player's job using the same pattern as `IsOnTeam`.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Content.Server/Content.Server.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: Commit**

```bash
git add Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs
git commit -m "fix: update InitializePlayer callers with team parameter"
```

---

### Task 5: Auto-Open/Close StoreUI on Phase Transitions

**Covers:** Auto-open in FreezeTime, block in ActionPhase

**Files:**
- Modify: `Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs`

**Dependencies:** Task 3 (StoreComponent on player), Task 4 (team parameter)

- [ ] **Step 1: Add UserInterfaceSystem dependency**

Add to CsRoundControllerSystem dependencies:

```csharp
using Robust.Server.GameObjects;
using Content.Shared.Store;
using Content.Shared.Store.Components;
```

Add dependency:

```csharp
[Dependency] private readonly UserInterfaceSystem _ui = default!;
```

- [ ] **Step 2: Add OpenAllUplinks method**

Add a new method to CsRoundControllerSystem:

```csharp
private void OpenAllUplinks()
{
    var query = EntityQueryEnumerator<StoreComponent, ActorComponent, HumanoidAppearanceComponent, MindContainerComponent>();
    while (query.MoveNext(out var uid, out var store, out var actor, out _, out var mindContainer))
    {
        if (!mindContainer.HasMind)
            continue;

        if (!store.Balance.ContainsKey("Telecrystal"))
            continue;

        _ui.TryOpenUi(uid, StoreUiKey.Key, actor.PlayerSession);
        _store.UpdateUserInterface(uid, uid, store);
    }
}
```

- [ ] **Step 3: Add CloseAllUplinks method**

```csharp
private void CloseAllUplinks()
{
    var query = EntityQueryEnumerator<StoreComponent>();
    while (query.MoveNext(out var uid, out var store))
    {
        if (!store.Balance.ContainsKey("Telecrystal"))
            continue;

        _ui.CloseUi(uid, StoreUiKey.Key);
    }
}
```

- [ ] **Step 4: Call OpenAllUplinks when entering FreezeTime**

In `OnPhaseEnter`, add FreezeTime handling:

```csharp
private void OnPhaseEnter(EntityUid uid, CsRoundControllerComponent controller, CsRoundPhase phase)
{
    switch (phase)
    {
        case CsRoundPhase.FreezeTime:
            FreezeAllPlayers();
            OpenAllUplinks();
            break;
    }
}
```

- [ ] **Step 5: Call CloseAllUplinks when exiting FreezeTime and entering ActionPhase**

In `OnPhaseExit`, close uplinks when leaving FreezeTime:

```csharp
private void OnPhaseExit(EntityUid uid, CsRoundControllerComponent controller, CsRoundPhase phase)
{
    switch (phase)
    {
        case CsRoundPhase.FreezeTime:
            UnfreezeAllPlayers();
            CloseAllUplinks();
            break;
        case CsRoundPhase.PostAction:
            UnfreezeAllPlayers();
            break;
    }
}
```

- [ ] **Step 6: Block uplink opening during ActionPhase**

Subscribe to `ActivatableUIOpenAttemptEvent` on entities with `StoreComponent` and block during ActionPhase. Add to Initialize:

```csharp
SubscribeLocalEvent<StoreComponent, ActivatableUIOpenAttemptEvent>(OnStoreOpenAttempt);
```

Add handler:

```csharp
private void OnStoreOpenAttempt(EntityUid uid, StoreComponent component, ActivatableUIOpenAttemptEvent args)
{
    if (!component.Balance.ContainsKey("Telecrystal"))
        return;

    var query = EntityQueryEnumerator<CsRoundControllerComponent>();
    while (query.MoveNext(out _, out var controller))
    {
        if (controller.CurrentPhase == CsRoundPhase.ActionPhase)
        {
            args.Cancel();
            return;
        }
    }
}
```

- [ ] **Step 7: Build and verify**

Run: `dotnet build Content.Server/Content.Server.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 8: Commit**

```bash
git add Content.Server/CounterStrike/Systems/CsRoundControllerSystem.cs
git commit -m "feat: auto-open uplinks in FreezeTime, block in ActionPhase"
```

---

### Task 6: Handle StoreComponent on Respawn

**Covers:** Store persists across respawns

**Files:**
- Modify: `Content.Server/CounterStrike/Systems/CsRoundEconomySystem.cs`

**Dependencies:** Task 3, Task 5

- [ ] **Step 1: Ensure StoreComponent is added during RestorePlayerTc**

Already handled in Task 3 Step 6 — the `RestorePlayerTc` method calls `InitializePlayer` for respawned players that don't have a StoreComponent.

- [ ] **Step 2: Verify that StoreComponent is not deleted during CleanupRoundItems**

Check `CsRoundControllerSystem.CleanupRoundItems()` — it deletes corpses and loose items. Since StoreComponent is on the player entity (not a separate item), it will be cleaned up with the body. The TC is saved via `SaveTcForRespawn` before deletion. This is correct.

- [ ] **Step 3: Final build and verify**

Run: `dotnet build Content.Server/Content.Server.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: built-in uplink system complete"
```
