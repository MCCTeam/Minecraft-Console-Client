using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Trigger scripts on specific events
    /// </summary>

    public class ScriptScheduler : ChatBot
    {
        private class TaskDesc
        {
            public string? action = null;
            public bool triggerOnFirstLogin = false;
            public bool triggerOnLogin = false;
            public bool triggerOnTime = false;
            public bool triggerOnInterval = false;
            public int triggerOnInterval_Interval = 0;
            public int triggerOnInterval_Interval_Max = 0;
            public int triggerOnInterval_Interval_Countdown = 0;
            public List<DateTime> triggerOnTime_Times = new();
            public bool triggerOnTime_alreadyTriggered = false;
        }

        private static bool firstlogin_done = false;

        private readonly string tasksfile;
        private bool serverlogin_done;
        private readonly List<TaskDesc> tasks = new();
        private int verifytasks_timeleft = 10;
        private readonly int verifytasks_delay = 10;

        public ScriptScheduler(string tasksfile)
        {
            this.tasksfile = tasksfile;
            serverlogin_done = false;
        }

        public override void Initialize()
        {
            //Load the given file from the startup parameters
            if (System.IO.File.Exists(tasksfile))
            {
                LogDebugToConsoleTranslated("bot.scriptScheduler.loading", System.IO.Path.GetFullPath(tasksfile));
                TaskDesc? current_task = null;
                string[] lines = System.IO.File.ReadAllLines(tasksfile, Encoding.UTF8);
                foreach (string lineRAW in lines)
                {
                    string line = lineRAW.Split('#')[0].Trim();
                    if (line.Length > 0)
                    {
                        if (line[0] == '[' && line[^1] == ']')
                        {
                            switch (line[1..^1].ToLower())
                            {
                                case "task":
                                    CheckAddTask(current_task);
                                    current_task = new TaskDesc(); //Create a blank task
                                    break;
                            }
                        }
                        else if (current_task != null)
                        {
                            string argName = line.Split('=')[0];
                            if (line.Length > (argName.Length + 1))
                            {
                                string argValue = line[(argName.Length + 1)..];
                                switch (argName.ToLower())
                                {
                                    case "triggeronfirstlogin": current_task.triggerOnFirstLogin = Settings.str2bool(argValue); break;
                                    case "triggeronlogin": current_task.triggerOnLogin = Settings.str2bool(argValue); break;
                                    case "triggerontime": current_task.triggerOnTime = Settings.str2bool(argValue); break;
                                    case "triggeroninterval": current_task.triggerOnInterval = Settings.str2bool(argValue); break;
                                    case "timevalue": try { current_task.triggerOnTime_Times.Add(DateTime.ParseExact(argValue, "HH:mm", CultureInfo.InvariantCulture)); } catch { } break;
                                    case "timeinterval":
                                        int interval;
                                        int intervalMax = 0;

                                        if (argValue.Contains('-'))
                                        {
                                            string[] parts = argValue.Split("-");
                                            if (parts.Length == 2)
                                            {
                                                interval = int.Parse(parts[0].Trim());
                                                intervalMax = int.Parse(parts[1].Trim());
                                            }
                                            else
                                            {
                                                interval = 1;
                                            }
                                        }
                                        else
                                        {
                                            interval = int.Parse(argValue);
                                        }

                                        current_task.triggerOnInterval_Interval = interval;
                                        current_task.triggerOnInterval_Interval_Max = intervalMax;

                                        break;
                                    case "script": current_task.action = "script " + argValue; break; //backward compatibility with older tasks.ini
                                    case "action": current_task.action = argValue; break;
                                }
                            }
                        }
                    }
                }
                CheckAddTask(current_task);
            }
            else
            {
                LogToConsoleTranslated("bot.scriptScheduler.not_found", System.IO.Path.GetFullPath(tasksfile));
                UnloadBot(); //No need to keep the bot active
            }
        }

        private void CheckAddTask(TaskDesc? current_task)
        {
            //Check if we built a valid task before adding it
            if (current_task != null)
            {
                //Look for a valid action
                if (!String.IsNullOrWhiteSpace(current_task.action))
                {
                    //Look for a valid trigger
                    if (current_task.triggerOnLogin
                        || current_task.triggerOnFirstLogin
                        || (current_task.triggerOnTime && current_task.triggerOnTime_Times.Count > 0)
                        || (current_task.triggerOnInterval && current_task.triggerOnInterval_Interval > 0))
                    {

                        LogDebugToConsoleTranslated("bot.scriptScheduler.loaded_task", Task2String(current_task));
                        current_task.triggerOnInterval_Interval_Countdown = current_task.triggerOnInterval_Interval; //Init countdown for interval
                        tasks.Add(current_task);
                    }
                    else
                    {
                        LogDebugToConsoleTranslated("bot.scriptScheduler.no_trigger", Task2String(current_task));
                    }
                }
                else
                {
                    LogDebugToConsoleTranslated("bot.scriptScheduler.no_action", Task2String(current_task));
                }
            }
        }

        public override void Update()
        {
            if (verifytasks_timeleft <= 0)
            {
                verifytasks_timeleft = verifytasks_delay;
                if (serverlogin_done)
                {
                    foreach (TaskDesc task in tasks)
                    {
                        if (task.triggerOnTime)
                        {
                            bool matching_time_found = false;

                            foreach (DateTime time in task.triggerOnTime_Times)
                            {
                                if (time.Hour == DateTime.Now.Hour && time.Minute == DateTime.Now.Minute)
                                {
                                    matching_time_found = true;
                                    if (!task.triggerOnTime_alreadyTriggered)
                                    {
                                        task.triggerOnTime_alreadyTriggered = true;
                                        LogDebugToConsoleTranslated("bot.scriptScheduler.running_time", task.action);
                                        PerformInternalCommand(task.action!);
                                    }
                                }
                            }

                            if (!matching_time_found)
                                task.triggerOnTime_alreadyTriggered = false;
                        }

                        if (task.triggerOnInterval)
                        {
                            if (task.triggerOnInterval_Interval_Countdown == 0)
                            {
                                int time = task.triggerOnInterval_Interval;

                                if (task.triggerOnInterval_Interval_Max != 0)
                                    time = new Random().Next(task.triggerOnInterval_Interval, task.triggerOnInterval_Interval_Max);

                                task.triggerOnInterval_Interval_Countdown = time;
                                LogDebugToConsoleTranslated("bot.scriptScheduler.running_inverval", task.action);
                                PerformInternalCommand(task.action!);
                            }
                            else task.triggerOnInterval_Interval_Countdown--;
                        }
                    }
                }
                else
                {
                    foreach (TaskDesc task in tasks)
                    {
                        if (task.triggerOnLogin || (firstlogin_done == false && task.triggerOnFirstLogin))
                        {
                            LogDebugToConsoleTranslated("bot.scriptScheduler.running_login", task.action);
                            PerformInternalCommand(task.action!);
                        }
                    }

                    firstlogin_done = true;
                    serverlogin_done = true;
                }
            }
            else verifytasks_timeleft--;
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            serverlogin_done = false;
            return false;
        }

        private static string Task2String(TaskDesc task)
        {
            return Translations.Get(
                "bot.scriptScheduler.task",
                task.triggerOnFirstLogin,
                task.triggerOnLogin,
                task.triggerOnTime,
                task.triggerOnInterval,
                String.Join(", ", task.triggerOnTime_Times),
                task.triggerOnInterval_Interval,
                task.action
            );
        }
    }
}
