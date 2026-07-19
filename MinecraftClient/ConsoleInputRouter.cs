using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftClient
{
    internal static class ConsoleInputRouter
    {
        private static readonly Lock StateLock = new();
        private static readonly CancellationTokenSource Shutdown = new();
        private static bool started;
        private static Action<string>? messageRoute;
        private static EventHandler<ConsoleInputBuffer>? inputChangeRoute;
        private static TaskCompletionSource<string>? pendingRead;

        internal static bool IsStarted
        {
            get
            {
                lock (StateLock)
                    return started;
            }
        }

        internal static void EnsureStarted()
        {
            lock (StateLock)
            {
                if (started)
                    return;

                started = true;
                if (ConsoleIO.BasicIO || ConsoleIO.Backend is null)
                {
                    var readThread = new Thread(() => ReadBasicInput(Shutdown.Token))
                    {
                        Name = "MCC console input router",
                    };
                    readThread.Start();
                    return;
                }

                try
                {
                    ConsoleIO.Backend.MessageReceived += OnMessageReceived;
                    ConsoleIO.Backend.OnInputChange += OnInputChanged;
                    ConsoleIO.Backend.BeginReadThread();
                }
                catch
                {
                    ConsoleIO.Backend.MessageReceived -= OnMessageReceived;
                    ConsoleIO.Backend.OnInputChange -= OnInputChanged;
                    started = false;
                    throw;
                }
            }
        }

        internal static void RouteToClient(McClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            EnsureStarted();

            lock (StateLock)
            {
                messageRoute = client.RouteConsoleInput;
                inputChangeRoute = ConsoleIO.AutocompleteHandler;
            }
        }

        internal static void ClearClient(McClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            lock (StateLock)
            {
                if (messageRoute == client.RouteConsoleInput)
                {
                    messageRoute = null;
                    inputChangeRoute = null;
                }
            }
        }

        internal static void RouteOffline(Action<string> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            EnsureStarted();

            lock (StateLock)
            {
                messageRoute = handler;
                inputChangeRoute = ConsoleIO.OfflineAutocompleteHandler;
            }
        }

        internal static void ClearOfflineRoute(Action<string> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            lock (StateLock)
            {
                if (messageRoute == handler)
                {
                    messageRoute = null;
                    inputChangeRoute = null;
                }
            }
        }

        internal static string ReadLine()
        {
            EnsureStarted();

            TaskCompletionSource<string> readCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (StateLock)
            {
                if (pendingRead is not null)
                    throw new InvalidOperationException();

                pendingRead = readCompletion;
            }

            return readCompletion.Task.GetAwaiter().GetResult();
        }

        internal static void ShutdownRouter()
        {
            lock (StateLock)
            {
                messageRoute = null;
                inputChangeRoute = null;
                pendingRead?.TrySetCanceled();
                pendingRead = null;
                Shutdown.Cancel();
            }
        }

        private static void ReadBasicInput(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? input = Console.ReadLine();
                if (input is null)
                    return;

                DispatchMessage(input);
            }
        }

        private static void OnMessageReceived(object? sender, string message)
        {
            DispatchMessage(message);
        }

        private static void OnInputChanged(object? sender, ConsoleInputBuffer input)
        {
            EventHandler<ConsoleInputBuffer>? route;
            lock (StateLock)
                route = inputChangeRoute;

            route?.Invoke(sender, input);
        }

        private static void DispatchMessage(string message)
        {
            TaskCompletionSource<string>? readCompletion;
            Action<string>? route;

            lock (StateLock)
            {
                readCompletion = pendingRead;
                if (readCompletion is not null)
                    pendingRead = null;
                route = messageRoute;
            }

            if (readCompletion is not null)
            {
                readCompletion.TrySetResult(message);
                return;
            }

            route?.Invoke(message);
        }
    }
}
