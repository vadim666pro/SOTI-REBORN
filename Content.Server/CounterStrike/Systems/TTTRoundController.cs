using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Server.Mind;
using Content.Server.Mobs;
using Content.Shared.CounterStrike.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Map; // для MapCoordinates, если нужно
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Content.Server.GameTicking.Commands;
using Robust.Shared.Console;

namespace Content.Server.CounterStrike.Systems;

public sealed class TTTRoundController : GameRuleSystem<TTTRuleComponent>
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("ttt-round");

    protected override void Added(EntityUid uid, TTTRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        // Инициализация
        ResetTimers(component);
        component.PoliceSpawned = false;
        component.PolicePhaseActive = false;
        component.Phase = TTTPhase.FreePhase;

        Sawmill.Info("[TTT] Правило активировано. Свободная фаза 2:30.");
    }
    private void ResetTimers(TTTRuleComponent component)
    {
        component.Timer = TTTRuleComponent.FreePhaseDuration;
        component.PolicePhaseTimer = TTTRuleComponent.PolicePhaseDuration;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TTTRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var component, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (!component.PoliceSpawned && component.Phase == TTTPhase.FreePhase)
            {
                component.Timer -= frameTime;
                if (component.Timer <= 0)
                {
                    SpawnPolice(component);
                    component.PoliceSpawned = true;
                    component.Phase = TTTPhase.PolicePhase;
                    component.PolicePhaseActive = true;
                    component.PolicePhaseTimer = TTTRuleComponent.PolicePhaseDuration;
                    _chat.DispatchGlobalAnnouncement("Полиция приехала. До конца раунда 90 секунд", sender: "ПОЛИЦИЯ");
                }
                continue;
            }
            if (component.PolicePhaseActive)
            {
                component.PolicePhaseTimer -= frameTime;
                if (component.PolicePhaseTimer <= 0 && _gameTicker.RunLevel == GameRunLevel.InRound)
                {
                    _gameTicker.EndRound($"Раунд завершен. Победа гражданских");
                }

            }
        }
    }

    private void SpawnPolice(TTTRuleComponent component)
    {
        // Собираем всех мёртвых игроков (кто в спектаторах), исключая маньяка.
        // Но маньяк не хранится в компоненте, поэтому определяем его по наличию роли Assassin.
        // Для этого нужно проверить Mind игрока на наличие антагониста.
        // Если у вас есть способ получить маньяка, например, через свойство компонента, используйте его.
        // Так как у нас нет поля Maniac в компоненте, мы будем искать игрока с ролью Assassin.
        // Для простоты предположим, что у нас есть метод GetManiac().
        // Или можно вообще не исключать маньяка, если он жив (вряд ли он мёртв, но на всякий случай).
        var deadCandidates = new List<ICommonSession>();

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

            if (!_playerManager.TryGetSessionById(userId, out var session))
                continue;

            // Проверяем, мёртв ли игрок
            if (TryComp(bodyUid, out MobStateComponent? mobState) && _mobState.IsAlive(bodyUid, mobState))
                continue; // живых не берём

            // Проверяем, не является ли игрок маньяком (Assassin)
            // Для этого проверяем наличие роли Assassin в Mind
            if (HasComp<AssassinRoleComponent>(mindId) || HasComp<AssassinRoleComponent>(mindId))
                continue; // пропускаем маньяка

            deadCandidates.Add(session);
        }

        if (deadCandidates.Count == 0)
        {
            Sawmill.Warning("[TTT] Нет мёртвых игроков для спавна полиции.");
            return;
        }
        _random.Shuffle(deadCandidates);
        var selected = deadCandidates.Take(TTTRuleComponent.PoliceCount).ToList();
        Sawmill.Info($"[TTT] Выбрано {selected.Count} полицейских из {deadCandidates.Count} мёртвых.");

        var station = _station.GetStations().FirstOrDefault();
        if (station == default)
        {
            Sawmill.Error("[TTT] Станция не найдена.");
            return;
        }

        foreach (var session in selected)
        {
            // Удаляем старое тело
            if (session.AttachedEntity is { } oldEntity)
                Del(oldEntity);

            // Спавним за утилизатора
            _gameTicker.MakeJoinGame(session, station, "SalvageSpecialist", silent: true);
        }

        _chat.DispatchGlobalAnnouncement(
            $"Полиция приехала. {selected.Count} офицеров откликнулись на вызов.",
            sender: "Полиция",
            playSound: false
        );
    }
}
