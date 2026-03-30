using System;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Tui;
using Avalonia.Threading;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    class Minimap : Command
    {
        public override string CmdName => "minimap";
        public override string CmdUsage => "minimap [on|off] | minimap zoom [in|out|<1-16>] | minimap names [players|hostile|neutral|passive] [on|off] | minimap names [all_on|all_off] | minimap position [top_left|top_right|center|bottom_left|bottom_right] | minimap cave [auto|on|off]";
        public override string CmdDesc => Translations.cmd_minimap_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DoToggle(r.Source))
                .Then(l => l.Literal("on")
                    .Executes(r => DoOn(r.Source)))
                .Then(l => l.Literal("off")
                    .Executes(r => DoOff(r.Source)))
                .Then(l => l.Literal("zoom")
                    .Executes(r => DoZoomInfo(r.Source))
                    .Then(l => l.Literal("in")
                        .Executes(r => DoZoomIn(r.Source)))
                    .Then(l => l.Literal("out")
                        .Executes(r => DoZoomOut(r.Source)))
                    .Then(l => l.Argument("level", Arguments.Integer(MinimapControl.MinZoom, MinimapControl.MaxZoom))
                        .Executes(r => DoZoomSet(r.Source, Arguments.GetInteger(r, "level")))))
                .Then(l => l.Literal("names")
                    .Executes(r => DoNamesInfo(r.Source))
                    .Then(l => l.Literal("all_on")
                        .Executes(r => DoNamesAll(r.Source, true)))
                    .Then(l => l.Literal("all_off")
                        .Executes(r => DoNamesAll(r.Source, false)))
                    .Then(l => l.Literal("players")
                        .Executes(r => DoNamesCatInfo(r.Source, MobCategory.Player))
                        .Then(l => l.Literal("on")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Player, true)))
                        .Then(l => l.Literal("off")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Player, false))))
                    .Then(l => l.Literal("hostile")
                        .Executes(r => DoNamesCatInfo(r.Source, MobCategory.Hostile))
                        .Then(l => l.Literal("on")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Hostile, true)))
                        .Then(l => l.Literal("off")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Hostile, false))))
                    .Then(l => l.Literal("neutral")
                        .Executes(r => DoNamesCatInfo(r.Source, MobCategory.Neutral))
                        .Then(l => l.Literal("on")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Neutral, true)))
                        .Then(l => l.Literal("off")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Neutral, false))))
                    .Then(l => l.Literal("passive")
                        .Executes(r => DoNamesCatInfo(r.Source, MobCategory.Passive))
                        .Then(l => l.Literal("on")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Passive, true)))
                        .Then(l => l.Literal("off")
                            .Executes(r => DoNamesCatSet(r.Source, MobCategory.Passive, false)))))
                .Then(l => l.Literal("position")
                    .Executes(r => DoPositionInfo(r.Source))
                    .Then(l => l.Literal("top_left")
                        .Executes(r => DoPositionSet(r.Source, MinimapPosition.top_left)))
                    .Then(l => l.Literal("top_right")
                        .Executes(r => DoPositionSet(r.Source, MinimapPosition.top_right)))
                    .Then(l => l.Literal("center")
                        .Executes(r => DoPositionSet(r.Source, MinimapPosition.center)))
                    .Then(l => l.Literal("bottom_left")
                        .Executes(r => DoPositionSet(r.Source, MinimapPosition.bottom_left)))
                    .Then(l => l.Literal("bottom_right")
                        .Executes(r => DoPositionSet(r.Source, MinimapPosition.bottom_right))))
                .Then(l => l.Literal("cave")
                    .Executes(r => DoCaveInfo(r.Source))
                    .Then(l => l.Literal("auto")
                        .Executes(r => DoCaveSet(r.Source, CaveModeOption.auto)))
                    .Then(l => l.Literal("on")
                        .Executes(r => DoCaveSet(r.Source, CaveModeOption.on)))
                    .Then(l => l.Literal("off")
                        .Executes(r => DoCaveSet(r.Source, CaveModeOption.off))))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help")?.GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string _) =>
            r.SetAndReturn(GetCmdDescTranslated());

        private static MainTuiView? GetTuiView(CmdResult r)
        {
            if (ConsoleIO.Backend is not TuiConsoleBackend)
            {
                r.SetAndReturn(Status.Fail, Translations.cmd_minimap_tui_only);
                return null;
            }
            return TuiConsoleBackend.Instance?.GetView();
        }

        private static int DoToggle(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            bool wasVisible = view.IsMinimapVisible;
            Dispatcher.UIThread.Post(() => view.ToggleMinimap());
            string msg = wasVisible
                ? Translations.cmd_minimap_disabled
                : Translations.cmd_minimap_enabled;
            return r.SetAndReturn(Status.Done, msg);
        }

        private static int DoOn(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            Dispatcher.UIThread.Post(() => view.ShowMinimap());
            return r.SetAndReturn(Status.Done, Translations.cmd_minimap_enabled);
        }

        private static int DoOff(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            Dispatcher.UIThread.Post(() => view.HideMinimap());
            return r.SetAndReturn(Status.Done, Translations.cmd_minimap_disabled);
        }

        private static int DoZoomInfo(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            int current = view.GetMinimapZoom();
            return r.SetAndReturn(Status.Done,
                string.Format(Translations.cmd_minimap_zoom_current, current, MinimapControl.MaxZoom));
        }

        private static int DoZoomIn(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            int newLevel = Math.Max(view.GetMinimapZoom() - 1, MinimapControl.MinZoom);
            Dispatcher.UIThread.Post(() => view.SetMinimapZoom(newLevel));
            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_minimap_zoom_set, newLevel));
        }

        private static int DoZoomOut(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            int newLevel = Math.Min(view.GetMinimapZoom() + 1, MinimapControl.MaxZoom);
            Dispatcher.UIThread.Post(() => view.SetMinimapZoom(newLevel));
            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_minimap_zoom_set, newLevel));
        }

        private static int DoZoomSet(CmdResult r, int level)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            Dispatcher.UIThread.Post(() => view.SetMinimapZoom(level));
            return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_minimap_zoom_set, level));
        }

        private static int DoNamesInfo(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            var nc = view.GetMinimapNameConfig();
            string status = string.Format(Translations.cmd_minimap_names_status,
                BoolStr(nc.Players), BoolStr(nc.Hostile), BoolStr(nc.Neutral), BoolStr(nc.Passive));
            return r.SetAndReturn(Status.Done, status);
        }

        private static int DoNamesAll(CmdResult r, bool on)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            Dispatcher.UIThread.Post(() =>
            {
                view.GetMinimapNameConfig().SetAll(on);
                view.SyncMinimapNameConfig();
            });
            string msg = on ? Translations.cmd_minimap_names_all_on : Translations.cmd_minimap_names_all_off;
            return r.SetAndReturn(Status.Done, msg);
        }

        private static int DoNamesCatInfo(CmdResult r, MobCategory cat)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            var nc = view.GetMinimapNameConfig();
            bool val = cat switch
            {
                MobCategory.Player => nc.Players,
                MobCategory.Hostile => nc.Hostile,
                MobCategory.Neutral => nc.Neutral,
                MobCategory.Passive => nc.Passive,
                _ => false,
            };
            return r.SetAndReturn(Status.Done,
                string.Format(Translations.cmd_minimap_names_cat, cat, BoolStr(val)));
        }

        private static int DoNamesCatSet(CmdResult r, MobCategory cat, bool on)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            Dispatcher.UIThread.Post(() =>
            {
                var nc = view.GetMinimapNameConfig();
                switch (cat)
                {
                    case MobCategory.Player: nc.Players = on; break;
                    case MobCategory.Hostile: nc.Hostile = on; break;
                    case MobCategory.Neutral: nc.Neutral = on; break;
                    case MobCategory.Passive: nc.Passive = on; break;
                }
                view.SyncMinimapNameConfig();
            });
            return r.SetAndReturn(Status.Done,
                string.Format(Translations.cmd_minimap_names_cat_set, cat, BoolStr(on)));
        }

        private static int DoPositionInfo(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            var pos = view.GetMinimapPosition();
            return r.SetAndReturn(Status.Done,
                string.Format(Translations.cmd_minimap_position_current, pos));
        }

        private static int DoPositionSet(CmdResult r, MinimapPosition pos)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            Dispatcher.UIThread.Post(() => view.SetMinimapPosition(pos));
            return r.SetAndReturn(Status.Done,
                string.Format(Translations.cmd_minimap_position_set, pos));
        }

        private static int DoCaveInfo(CmdResult r)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            var mode = view.GetMinimapCaveMode();
            return r.SetAndReturn(Status.Done,
                string.Format(Translations.cmd_minimap_cave_current, mode));
        }

        private static int DoCaveSet(CmdResult r, CaveModeOption mode)
        {
            var view = GetTuiView(r);
            if (view is null) return (int)r.status;

            Dispatcher.UIThread.Post(() => view.SetMinimapCaveMode(mode));
            return r.SetAndReturn(Status.Done,
                string.Format(Translations.cmd_minimap_cave_set, mode));
        }

        private static string BoolStr(bool v) => v ? "ON" : "OFF";
    }
}
