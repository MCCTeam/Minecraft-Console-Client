using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Tui;

namespace MinecraftClient.Commands
{
    class InventoryTui : Command
    {
        public override string CmdName => "inventui";
        public override string CmdUsage => "inventui [inventoryId]";
        public override string CmdDesc => "Open interactive TUI inventory viewer";

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
            handler.Log.Info("Opens a full-screen interactive TUI for browsing and managing inventory.");
            handler.Log.Info("Press ESC to exit, click slots to interact, Q to drop items.");
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int Execute(CmdResult r, int inventoryId)
        {
            McClient handler = CmdResult.currentHandler!;

            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (ConsoleIO.BasicIO)
            {
                handler.Log.Warn("Interactive TUI is not available in BasicIO mode. Use '/inventory' instead.");
                return r.SetAndReturn(CmdResult.Status.Fail);
            }

            if (InventoryTuiHost.IsRunning)
            {
                handler.Log.Warn("TUI is already running.");
                return r.SetAndReturn(CmdResult.Status.Fail);
            }

            if (!InventoryTuiHost.CanLaunch)
            {
                handler.Log.Warn("TUI can only be opened once per session. Please restart MCC to use it again.");
                return r.SetAndReturn(CmdResult.Status.Fail);
            }

            var container = handler.GetInventory(inventoryId);
            if (container == null)
            {
                handler.Log.Warn($"Inventory #{inventoryId} not found.");
                return r.SetAndReturn(CmdResult.Status.Fail, $"Inventory #{inventoryId} not found");
            }

            InventoryTuiHost.OnSuspendConsole = () =>
            {
                ConsoleInteractive.ConsoleReader.StopReadThread();
                ConsoleInteractive.ConsoleReader.MessageReceived -= handler.GetConsoleMessageHandler();
                ConsoleInteractive.ConsoleReader.OnInputChange -= ConsoleIO.AutocompleteHandler;
            };

            InventoryTuiHost.OnResumeConsole = () =>
            {
                ConsoleInteractive.ConsoleWriter.Init();
                ConsoleInteractive.ConsoleReader.BeginReadThread();
                ConsoleInteractive.ConsoleReader.MessageReceived += handler.GetConsoleMessageHandler();
                ConsoleInteractive.ConsoleReader.OnInputChange += ConsoleIO.AutocompleteHandler;
            };

            handler.Log.Info($"Opening TUI for Inventory #{inventoryId}...");

            bool success = InventoryTuiHost.Launch(handler, inventoryId);

            if (success)
            {
                handler.Log.Info("TUI closed.");
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
