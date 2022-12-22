using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;

namespace MinecraftClient
{
    internal static class AsyncTaskHandler
    {
        private static readonly List<Task> Tasks = new();

        internal static Task CheckUpdate = Task.CompletedTask;

        internal static Task CacheSessionReader = Task.CompletedTask;

        internal static Task SaveSessionToDisk = Task.CompletedTask;

        internal static Task WritebackSettingFile = Task.CompletedTask;

        internal static async void ExitCleanUp()
        {
            await WritebackSettingFile;

            await CacheSessionReader;

            await SaveSessionToDisk;

            foreach (var task in Tasks)
            {
                await task;
            }

            Tasks.Clear();
        }
    }
}
