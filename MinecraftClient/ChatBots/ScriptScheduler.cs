using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Trigger scripts on specific events
    /// </summary>

    public class ScriptScheduler : ChatBot
    {
        private class TaskDesc
        {
            public string script_file = null;
            public bool triggerOnFirstLogin = false;
            public bool triggerOnLogin = false;
            public bool triggerOnTime = false;
            public bool triggerOnInterval = false;
            public int triggerOnInterval_Interval = 0;
            public int triggerOnInterval_Interval_Countdown = 0;
            public List<DateTime> triggerOnTime_Times = new List<DateTime>();
            public bool triggerOnTime_alreadyTriggered = false;
        }

        private static bool firstlogin_done = false;

        private string tasksfile;
        private bool serverlogin_done;
        private List<TaskDesc> tasks = new List<TaskDesc>();
        private int verifytasks_timeleft = 10;
        private int verifytasks_delay = 10;

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
                if (Settings.DebugMessages)
                    LogToConsole("Loading tasks from '" + tasksfile + "'");
                TaskDesc current_task = null;
                String[] lines = System.IO.File.ReadAllLines(tasksfile);
                foreach (string lineRAW in lines)
                {
                    string line = lineRAW.Split('#')[0].Trim();
                    if (line.Length > 0)
                    {
                        if (line[0] == '[' && line[line.Length - 1] == ']')
                        {
                            switch (line.Substring(1, line.Length - 2).ToLower())
                            {
                                case "task":
                                    checkAddTask(current_task);
                                    current_task = new TaskDesc(); //Create a blank task
                                    break;
                            }
                        }
                        else if (current_task != null)
                        {
                            string argName = line.Split('=')[0];
                            if (line.Length > (argName.Length + 1))
                            {
                                string argValue = line.Substring(argName.Length + 1);
                                switch (argName.ToLower())
                                {
                                    case "triggeronfirstlogin": current_task.triggerOnFirstLogin = Settings.str2bool(argValue); break;
                                    case "triggeronlogin": current_task.triggerOnLogin = Settings.str2bool(argValue); break;
                                    case "triggerontime": current_task.triggerOnTime = Settings.str2bool(argValue); break;
                                    case "triggeroninterval": current_task.triggerOnInterval = Settings.str2bool(argValue); break;
                                    case "timevalue": try { current_task.triggerOnTime_Times.Add(DateTime.ParseExact(argValue, "HH:mm", CultureInfo.InvariantCulture)); } catch { } break;
                                    case "timeinterval": int interval = 1; int.TryParse(argValue, out interval); current_task.triggerOnInterval_Interval = interval; break;
                                    case "script": current_task.script_file = argValue; break;
                                }
                            }
                        }
                    }
                }
                checkAddTask(current_task);
            }
            else
            {
                LogToConsole("File not found: '" + tasksfile + "'");
                UnloadBot(); //No need to keep the bot active
            }
        }

        private void checkAddTask(TaskDesc current_task)
        {
            if (current_task != null)
            {
                //Check if we built a valid task before adding it
                if (Script.LookForScript(ref current_task.script_file)) //Check if file exists
                {
                    if (current_task.script_file != null
                        && (current_task.triggerOnLogin
                        || current_task.triggerOnFirstLogin
                        || (current_task.triggerOnTime && current_task.triggerOnTime_Times.Count > 0))
                        || (current_task.triggerOnInterval && current_task.triggerOnInterval_Interval > 0)) //Look for a valid trigger
                    {
                        if (Settings.DebugMessages)
                            LogToConsole("Loaded task:\n" + Task2String(current_task));
                        current_task.triggerOnInterval_Interval_Countdown = current_task.triggerOnInterval_Interval; //Init countdown for interval
                        tasks.Add(current_task);
                    }
                    else if (Settings.DebugMessages)
                    {
                        LogToConsole("This task will never trigger:\n" + Task2String(current_task));
                    }
                }
                else if (Settings.DebugMessages)
                {
                    LogToConsole("No valid script for task:\n" + Task2String(current_task));
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
                                        if (Settings.DebugMessages)
                                            LogToConsole("Time / Running script: " + task.script_file);
                                        RunScript(task.script_file);
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
                                task.triggerOnInterval_Interval_Countdown = task.triggerOnInterval_Interval;
                                if (Settings.DebugMessages)
                                    LogToConsole("Interval / Running script: " + task.script_file);
                                RunScript(task.script_file);
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
                            if (Settings.DebugMessages)
                                LogToConsole("Login / Running script: " + task.script_file);
                            RunScript(task.script_file);
                        }
                    }

                    firstlogin_done = true;
                    serverlogin_done = true;
                }
            }
            else verifytasks_timeleft--;
        }

        private static string Task2String(TaskDesc task)
        {
            return String.Format(
                " triggeronfirstlogin = {0}\n triggeronlogin = {1}\n triggerontime = {2}\n "
                    + "triggeroninterval = {3}\n timevalue = {4}\n timeinterval = {5}\n script = {6}",
                task.triggerOnFirstLogin,
                task.triggerOnLogin,
                task.triggerOnTime,
                task.triggerOnInterval,
                String.Join(", ", task.triggerOnTime_Times),
                task.triggerOnInterval_Interval,
                task.script_file
            );
        }
    }
}
