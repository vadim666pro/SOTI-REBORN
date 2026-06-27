using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.CounterStrike;

/// <summary>
/// Job IDs used to identify Counter-Strike teams.
/// Used by bomb pickup/plant/defuse and team-elimination round end logic.
/// </summary>
/// <remarks>
/// To add a new role to a team, append its job prototype ID (the <c>id</c> field from
/// <c>Resources/Prototypes/Roles/Jobs/...</c>) to the corresponding set below.
/// Example: for a new T role with id <c>Clown</c>, add <c>"Clown"</c> to <see cref="TJobs"/>.
/// </remarks>
public static class CounterStrikeTeams
{
    /// <summary>Counter-Terrorist job prototype IDs.</summary>
    public static readonly HashSet<ProtoId<JobPrototype>> CtJobs =
    [
        "Musician",
        "Janitor",
        "Reporter",
        "Librarian",
    ];

    /// <summary>Terrorist job prototype IDs.</summary>
    public static readonly HashSet<ProtoId<JobPrototype>> TJobs =
    [
        "Passenger",
        "Lawyer",
        "ServiceWorker",
        "Mime",
    ];
}
