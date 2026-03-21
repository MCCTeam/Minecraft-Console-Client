# ConsoleInteractive [![Nuget](https://buildstats.info/nuget/ConsoleInteractivePrompt)](https://www.nuget.org/packages/ConsoleInteractivePrompt/)
A C# library that allows you to write and read from the console simultaneously

## Usage
See [ConsoleInteractiveDemo](https://github.com/breadbyte/ConsoleInteractive/blob/main/ConsoleInteractive/ConsoleInteractiveDemo) for a sample, but the gist of it is
```cs
// Create a CancellationToken so we can cancel the reader later.
CancellationTokenSource cts = new CancellationTokenSource();

// Create a new Thread that will print for us.
Thread PrintingThread = new Thread(new ThreadStart(() => {
    ConsoleWriter.WriteLine("Hello World!");
    Thread.Sleep(5000);
    ConsoleWriter.WriteLine("Hello World after 5 seconds!");
}));

// Create a new Reader Thread. This thread will start listening for console input
ConsoleReader.BeginReadThread(cts.Token);

// Handle incoming messages from the user (Enter key pressed)
ConsoleReader.MessageReceived += (sender, s) => {
    // We got a cancellation command! Let's cancel the CancellationTokenSource.
    if (s.Equals("cancel"))
        cts.Cancel();
};

// Start the printing thread.
PrintingThread.Start();
```

---

For formatted output, use [Minecraft's color and formatting code format.](https://minecraft.fandom.com/wiki/Formatting_codes#Color_codes)
```cs
    ConsoleWriter.WriteLineFormatted("§aText §cwith §bMixed §1C§2o§3l§4o§5r§6s§a!");
```
will show up as the following: ![image](https://user-images.githubusercontent.com/14045257/139871772-a3e4f327-c769-497e-976f-b7231fcc18ff.png)

## Note
It is important that you use the `ConsoleInteractive.ConsoleWriter` and `ConsoleInteractive.ConsoleReader` classes only. 

Mixing and matching with `System.Console` is not supported nor recommended.

## Demo
Check out an asciicast demo here!
[![asciicast](https://asciinema.org/a/XYhksfTOiKKKwoAD5vjQnb6J0.png)](https://asciinema.org/a/XYhksfTOiKKKwoAD5vjQnb6J0)
