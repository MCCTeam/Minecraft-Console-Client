using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Tui;

namespace MinecraftClient.Commands
{
    /// <summary>
    /// Shortcut command that opens the interactive inventory TUI.
    /// Equivalent to "/inventory &lt;id&gt; open".
    /// </summary>
    class InventoryTui : Command
    {
        public override string CmdName => "inventui";
        public override string CmdUsage => "inventui [inventoryId]";
        public override string CmdDesc => "Open interactive TUI inventory viewer (alias for /inventory <id> open)";

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => Execute(r.Source, 0))
                .Then(l => l.Argument("InventoryId", MccArguments.InventoryId())
                    .Executes(r => Execute(r.Source, Arguments.GetInteger(r, "InventoryId"))))
            );
        }

        private int GetUsage(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            handler.Log.Info($"§b{CmdName}§r - {CmdDesc}");
            handler.Log.Info($"Usage: §e{CmdUsage}");
            handler.Log.Info("Equivalent to: /inventory <id> open");
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int Execute(CmdResult r, int inventoryId)
        {
            McClient handler = CmdResult.currentHandler!;

            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (ConsoleIO.Backend is not TuiConsoleBackend)
            {
                handler.Log.Warn("Interactive TUI is only available in TUI console mode. Use '/inventory <id> list' instead.");
                return r.SetAndReturn(CmdResult.Status.Fail);
            }

            if (InventoryTuiHost.IsRunning)
            {
                handler.Log.Warn("TUI is already running.");
                return r.SetAndReturn(CmdResult.Status.Fail);
            }

            var container = handler.GetInventory(inventoryId);
            if (container == null)
            {
                handler.Log.Warn($"Inventory #{inventoryId} not found.");
                return r.SetAndReturn(CmdResult.Status.Fail, $"Inventory #{inventoryId} not found");
            }

            handler.Log.Info($"Opening TUI for Inventory #{inventoryId}...");

            bool success = InventoryTuiHost.Launch(handler, inventoryId);
            if (success)
            {
                handler.Log.Info("Inventory dialog opened.");
                return r.SetAndReturn(CmdResult.Status.Done);
            }
            else
            {
                handler.Log.Warn("Failed to launch TUI.");
                return r.SetAndReturn(CmdResult.Status.Fail);
            }
        }
    }
}
