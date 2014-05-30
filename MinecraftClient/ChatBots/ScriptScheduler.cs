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
            public List<DateTime> triggerOnTime_Times = new List<DateTime>();
            public bool alreadyTriggered = false;
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
                                    case "timevalue": try { current_task.triggerOnTime_Times.Add(DateTime.ParseExact(argValue, "HH:mm", CultureInfo.InvariantCulture)); }
                                        catch { } break;
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
                if (current_task.script_file != null && Script.lookForScript(ref current_task.script_file) //Check if file exists
                    && (current_task.triggerOnLogin || (current_task.triggerOnTime && current_task.triggerOnTime_Times.Count > 0))) //Look for a valid trigger
                {
                    tasks.Add(current_task);
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
                            foreach (DateTime time in task.triggerOnTime_Times)
                            {
                                if (time.Hour == DateTime.Now.Hour && time.Minute == DateTime.Now.Minute)
                                {
                                    if (!task.alreadyTriggered)
                                    {
                                        task.alreadyTriggered = true;
                                        RunScript(task.script_file);
                                    }
                                }
                            }
                        }
                        else task.alreadyTriggered = false;
                    }
                }
                else
                {
                    foreach (TaskDesc task in tasks)
                    {
                        if (task.triggerOnLogin || (firstlogin_done == false && task.triggerOnFirstLogin))
                            RunScript(task.script_file);
                    }

                    firstlogin_done = true;
                    serverlogin_done = true;
                }
            }
            else verifytasks_timeleft--;
        }
    }
}
