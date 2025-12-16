using Content.Server.Administration;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ShowApcPowerRecieverLoadCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override string Command => "showapcpowerreceiverload";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("You must be a player to use this command.");
            return;
        }

        var sys = _entMan.System<ApcPowerDebugOverlaySystem>();
        sys.ToggleObserver(player);

        shell.WriteLine(Loc.GetString("cmd-showwallmounts-status", ("status", true)));
    }
}
