using System;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;
using Tomlet.Attributes;
using static MinecraftClient.ChatBots.ScriptScheduler.Configs;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Trigger scripts on specific events
    /// </summary>

    public class ScriptScheduler : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "ScriptScheduler";

            public bool Enabled = false;

            public TaskConfig[] TaskList = new TaskConfig[] {
                new TaskConfig(
                    Task_Name: "Task Name 1",
                    Trigger_On_First_Login: false,
                    Trigger_On_Login: false,
                    Trigger_On_Times: new(true, new TimeSpan[] { new(14, 00, 00) }),
                    Trigger_On_Interval: new(true, 3.6, 4.8),
                    Action: "send /hello"
                ),
                new TaskConfig(
                    Task_Name: "Task Name 2",
                    Trigger_On_First_Login: false,
                    Trigger_On_Login: true,
                    Trigger_On_Times: new(false, Array.Empty<TimeSpan>() ),
                    Trigger_On_Interval: new(false, 1, 10),
                    Action: "send /login pass"
                ),
            };

            public void OnSettingUpdate()
            {
                foreach (TaskConfig task in TaskList)
                {
                    task.Trigger_On_Interval.MinTime = Math.Max(0.1, task.Trigger_On_Interval.MinTime);
                    task.Trigger_On_Interval.MaxTime = Math.Max(0.1, task.Trigger_On_Interval.MaxTime);
                    if (task.Trigger_On_Interval.MinTime > task.Trigger_On_Interval.MaxTime)
                        (task.Trigger_On_Interval.MinTime, task.Trigger_On_Interval.MaxTime) = (task.Trigger_On_Interval.MaxTime, task.Trigger_On_Interval.MinTime);

                    //Look for a valid action
                    if (!String.IsNullOrWhiteSpace(task.Action))
                    {
                        //Look for a valid trigger
                        if (task.Trigger_On_Login
                            || task.Trigger_On_First_Login
                            || (task.Trigger_On_Times.Enable && task.Trigger_On_Times.Times.Length > 0)
                            || (task.Trigger_On_Interval.Enable && task.Trigger_On_Interval.MinTime > 0))
                        {
                            if (Settings.Config.Logging.DebugMessages)
                                LogToConsole(BotName, string.Format(Translations.bot_scriptScheduler_loaded_task, Task2String(task)));
                            task.Trigger_On_Interval_Countdown = Settings.DoubleToTick(task.Trigger_On_Interval.MinTime); //Init countdown for interval
                        }
                        else
                        {
                            if (Settings.Config.Logging.DebugMessages)
                                LogToConsole(BotName, string.Format(Translations.bot_scriptScheduler_no_trigger, Task2String(task)));
                        }
                    }
                    else
                    {
                        task.Action ??= string.Empty;
                        if (Settings.Config.Logging.DebugMessages)
                            LogToConsole(BotName, string.Format(Translations.bot_scriptScheduler_no_action, Task2String(task)));
                    }
                }

                if (Enabled && TaskList.Length == 0)
                {
                    LogToConsole(BotName, Translations.general_bot_unload);
                    Enabled = false;
                }
            }

            public class TaskConfig
            {
                public string Task_Name = "Task Name (Can be empty)";
                public bool Trigger_On_First_Login = false;
                public bool Trigger_On_Login = false;
                public TriggerOnTimeConfig Trigger_On_Times = new(false, new TimeSpan[] { new(23, 59, 59) });
                public TriggerOnIntervalConfig Trigger_On_Interval = new(false, 1, 10);
                public string Action = "send /hello";

                [NonSerialized]
                public bool Trigger_On_Time_Already_Triggered = false;

                [NonSerialized]
                public int Trigger_On_Interval_Countdown = 0;

                public TaskConfig() { }

                public TaskConfig(string Task_Name, bool Trigger_On_First_Login, bool Trigger_On_Login, TriggerOnTimeConfig Trigger_On_Times, TriggerOnIntervalConfig Trigger_On_Interval, string Action)
                {
                    this.Task_Name = Task_Name;
                    this.Trigger_On_First_Login = Trigger_On_First_Login;
                    this.Trigger_On_Login = Trigger_On_Login;
                    this.Trigger_On_Times = Trigger_On_Times;
                    this.Trigger_On_Interval = Trigger_On_Interval;
                    this.Action = Action;
                }

                public struct TriggerOnTimeConfig
                {
                    public bool Enable = false;
                    public TimeSpan[] Times;

                    public TriggerOnTimeConfig(bool Enable, TimeSpan[] Time)
                    {
                        this.Enable = Enable;
                        this.Times = Time;
                    }

                    public TriggerOnTimeConfig(TimeSpan[] Time)
                    {
                        this.Enable = true;
                        this.Times = Time;
                    }
                }

                public struct TriggerOnIntervalConfig
                {
                    public bool Enable = false;
                    public double MinTime, MaxTime;

                    public TriggerOnIntervalConfig(double value)
                    {
                        this.Enable = true;
                        MinTime = MaxTime = value;
                    }

                    public TriggerOnIntervalConfig(bool Enable, double value)
                    {
                        this.Enable = Enable;
                        MinTime = MaxTime = value;
                    }

                    public TriggerOnIntervalConfig(double min, double max)
                    {
                        this.MinTime = min;
                        this.MaxTime = max;
                    }

                    public TriggerOnIntervalConfig(bool Enable, double min, double max)
                    {
                        this.Enable = Enable;
                        this.MinTime = min;
                        this.MaxTime = max;
                    }
                }
            }
        }

        private Random random = new();

        private static bool firstlogin_done = false;

        private bool serverlogin_done = false;
        private int verifytasks_timeleft = 10;
        private readonly int verifytasks_delay = 10;

        public override void Update()
        {
            if (verifytasks_timeleft <= 0)
            {
                verifytasks_timeleft = verifytasks_delay;
                if (serverlogin_done)
                {
                    foreach (TaskConfig task in Config.TaskList)
                    {
                        if (task.Trigger_On_Times.Enable)
                        {
                            bool matching_time_found = false;

                            foreach (TimeSpan time in task.Trigger_On_Times.Times)
                            {
                                if (time.Hours == DateTime.Now.Hour && time.Minutes == DateTime.Now.Minute)
                                {
                                    matching_time_found = true;
                                    if (!task.Trigger_On_Time_Already_Triggered)
                                    {
                                        task.Trigger_On_Time_Already_Triggered = true;
                                        LogDebugToConsole(string.Format(Translations.bot_scriptScheduler_running_time, task.Action));
                                        CmdResult response = new();
                                        PerformInternalCommand(task.Action, ref response);
                                        if (response.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(response.result))
                                            LogToConsole(response);
                                    }
                                }
                            }

                            if (!matching_time_found)
                                task.Trigger_On_Time_Already_Triggered = false;
                        }

                    }
                }
                else
                {
                    foreach (TaskConfig task in Config.TaskList)
                    {
                        if (task.Trigger_On_Login || (firstlogin_done == false && task.Trigger_On_First_Login))
                        {
                            LogDebugToConsole(string.Format(Translations.bot_scriptScheduler_running_login, task.Action));
                            CmdResult response = new();
                            PerformInternalCommand(task.Action, ref response);
                            if (response.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(response.result))
                                LogToConsole(response);
                        }
                    }

                    firstlogin_done = true;
                    serverlogin_done = true;
                }
            }
            else verifytasks_timeleft--;

            foreach (TaskConfig task in Config.TaskList)
            {
                if (task.Trigger_On_Interval.Enable)
                {
                    if (task.Trigger_On_Interval_Countdown == 0)
                    {
                        task.Trigger_On_Interval_Countdown = random.Next(
                            Settings.DoubleToTick(task.Trigger_On_Interval.MinTime), Settings.DoubleToTick(task.Trigger_On_Interval.MaxTime)
                        );
                        LogDebugToConsole(string.Format(Translations.bot_scriptScheduler_running_inverval, task.Action));
                        CmdResult response = new();
                        PerformInternalCommand(task.Action, ref response);
                        if (response.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(response.result))
                            LogToConsole(response);
                    }
                    else task.Trigger_On_Interval_Countdown--;
                }
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            serverlogin_done = false;
            return false;
        }

        private static string Task2String(TaskConfig task)
        {
            return string.Format(
                Translations.bot_scriptScheduler_task,
                task.Trigger_On_First_Login,
                task.Trigger_On_Login,
                task.Trigger_On_Times.Enable,
                task.Trigger_On_Interval.Enable,
                string.Join(", ", Array.ConvertAll(task.Trigger_On_Times.Times, time => time.ToString())),
                task.Trigger_On_Interval.MinTime + '-' + task.Trigger_On_Interval.MaxTime,
                task.Action
            );
        }
    }
}
