using Content.Client.Fov.Systems;
using Content.Shared.Administration;
using Content.Shared.Fov.Components;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class ToggleFovCommand : LocalizedCommands
{
    public override string Command => "togglefov";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player?.AttachedEntity;
        if (player == null)
        {
            shell.WriteLine("No player entity found.");
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();

        // Toggle FovLimiterComponent (controls visual dark overlay)
        if (entManager.TryGetComponent<FovLimiterComponent>(player.Value, out var fov))
        {
            fov.Enabled = !fov.Enabled;
        }

        // Toggle sprite hiding system
        var fovSystem = entManager.System<SimpleFovSystem>();
        fovSystem.Enabled = !fovSystem.Enabled;

        if (!fovSystem.Enabled)
            fovSystem.RestoreAll();

        var enabled = fov?.Enabled ?? fovSystem.Enabled;
        shell.WriteLine($"FOV {(enabled ? "enabled" : "disabled")}.");
    }
}
