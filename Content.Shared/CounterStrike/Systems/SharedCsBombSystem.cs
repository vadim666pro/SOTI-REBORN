using Content.Shared.CounterStrike.Components;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.CounterStrike.Systems;

/// <summary>
/// Shared Counter-Strike bomb logic: team checks and pickup restrictions.
/// </summary>
public abstract class SharedCsBombSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CsBombComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUp);
    }

    private void OnGettingPickedUp(EntityUid uid, CsBombComponent comp, GettingPickedUpAttemptEvent args)
    {
        if (comp.Planted)
        {
            args.Cancel();
            return;
        }

        if (IsTerrorist(args.User))
            return;

        args.Cancel();
        _popup.PopupClient("Только террористы могут подбирать бомбу.", uid, args.User, PopupType.Small);
    }

    protected bool IsTerrorist(EntityUid user)
    {
        return IsOnTeam(user, CounterStrikeTeams.TJobs);
    }

    protected bool IsCounterTerrorist(EntityUid user)
    {
        return IsOnTeam(user, CounterStrikeTeams.CtJobs);
    }

    protected bool IsOnTeam(EntityUid user, HashSet<ProtoId<JobPrototype>> jobs)
    {
        if (!_mind.TryGetMind(user, out var mindId, out _))
            return false;

        if (!_jobs.MindTryGetJobId(mindId, out var jobId) || jobId is null)
            return false;

        return jobs.Contains(jobId.Value);
    }
}
