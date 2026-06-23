using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Server.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.GameObjects;
using Content.Server.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.GameTicking.Rules
{
    public sealed class RoleRevealRuleSystem : GameRuleSystem<RoleRevealRuleComponent>
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly MindSystem _minds = default!;
        [Dependency] private readonly SharedJobSystem _jobs = default!;
        [Dependency] private readonly RoleSystem _roles = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        protected override void Started(EntityUid uid, RoleRevealRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            // Send each connected player's role info and the configured image path.
            foreach (var session in _playerManager.Sessions.ToArray())
            {
                if (session.AttachedEntity is not { } attached)
                    continue;

                if (!_minds.TryGetMind(attached, out var mindId, out var mind))
                    continue;

                // Job name
                _jobs.MindTryGetJobName(mindId, out var jobName);

                // Antagonist role name (first antag role entity name) if any
                string? antagName = null;
                foreach (var roleEnt in mind.MindRoleContainer.ContainedEntities)
                {
                    if (!_entMan.TryGetComponent(roleEnt, out MindRoleComponent? mr))
                        continue;

                    if (!mr.Antag)
                        continue;

                    antagName = _entMan.GetComponent<MetaDataComponent>(roleEnt).EntityName;
                    break;
                }

                RaiseNetworkEvent(new RoleRevealEvent(component.Image, jobName, antagName, component.DisplayTime, component.FadeTime), session);
            }
        }
    }
}
