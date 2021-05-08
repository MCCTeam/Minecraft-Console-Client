using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Example of message receiving.
    /// </summary>

    public class TestBot : ChatBot
    {
        public override void Initialize()
        {
            new Thread(new ThreadStart(ThreadProc)).Start();
        }

        public void ThreadProc()
        {
            ScheduleTask(new Action(() => 
            { 
                ConsoleIO.WriteLine("I am on the main thread");
                ConsoleIO.WriteLine("I am running on Thread ID: " + Thread.CurrentThread.ManagedThreadId);
            }));
            string result = (string)ScheduleTask(new Func<string>(() => { return "I am a value in main thread"; }));
            ConsoleIO.WriteLine("I got result: " + result);
            ScheduleTaskDelayed(new Action(() => { ConsoleIO.WriteLine("I should be appeared 5 seconds later"); }), 50);
        }

        public override void GetText(string text)
        {
            string message = "";
            string username = "";
            text = GetVerbatim(text);

            if (IsPrivateMessage(text, ref message, ref username))
            {
                LogToConsoleTranslated("bot.testBot.told", username, message);
            }
            else if (IsChatMessage(text, ref message, ref username))
            {
                LogToConsoleTranslated("bot.testBot.said", username, message);
            }
        }
    }
}
