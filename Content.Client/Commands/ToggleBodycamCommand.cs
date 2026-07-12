using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[AnyCommand]
public sealed class ToggleBodycamCommand : LocalizedCommands
{
    public override string Command => "togglebodycam";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.WriteLine("Bodycam is always enabled. Use the Graphics settings to adjust fisheye and grain intensity.");
    }
}
