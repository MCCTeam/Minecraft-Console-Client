using System.Threading;
using ConsoleInteractive;

namespace ConsoleInteractiveDemo {
    class Program {
        static void Main(string[] args) {
            CancellationTokenSource cts = new();
            ConsoleWriter.Init();
            
            ConsoleWriter.WriteLine("type cancel to exit the application.");
            var t1 = new Thread(new ThreadStart(() => {
                ConsoleWriter.WriteLine("[T1] Hello World!");
                Thread.Sleep(5000);
                ConsoleWriter.WriteLine("[T1] Hello World after 5 seconds!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T1] Hello World after 8 seconds!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T1] Hello World after 11 seconds!");
                Thread.Sleep(5000);
                ConsoleWriter.WriteLine("[T1] Hello World after 16 seconds!");
            })) {IsBackground = true};
            var t2 = new Thread(new ThreadStart(() => {
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2!");
                Thread.Sleep(3000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 3 seconds!");
                Thread.Sleep(1000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 4 seconds!");
                Thread.Sleep(2000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 6 seconds!");
                Thread.Sleep(4000);
                ConsoleWriter.WriteLine("[T2] Hello from Thread 2 after 10 seconds!");
                Thread.Sleep(1000);
                ConsoleWriter.WriteLine("[T2] 1 Filler...");
                ConsoleWriter.WriteLine("[T2] 2 Filler...");
                ConsoleWriter.WriteLine("[T2] 3 Filler...");
                ConsoleWriter.WriteLine("[T2] 4 Filler...");
                ConsoleWriter.WriteLine("[T2] 5 Filler...");
                ConsoleWriter.WriteLine("[T2] 6 Filler...");
                ConsoleWriter.WriteLine("[T2] 7 Filler...");
            })) {IsBackground = true};

            var tF = new Thread(new ThreadStart(() => {
                ConsoleWriter.WriteLineFormatted("[T3] ---");
                ConsoleWriter.WriteLineFormatted("[T3] §0Black Text");
                ConsoleWriter.WriteLineFormatted("[T3] 1 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §1Dark Blue Text");
                ConsoleWriter.WriteLineFormatted("[T3] 2 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §2Dark Green Text");
                ConsoleWriter.WriteLineFormatted("[T3] 3 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §3Dark Aqua Text");
                ConsoleWriter.WriteLineFormatted("[T3] 4 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §4Dark Red Text");
                ConsoleWriter.WriteLineFormatted("[T3] 5 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §5Dark Purple Text");
                ConsoleWriter.WriteLineFormatted("[T3] 6 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §6Gold Text");
                ConsoleWriter.WriteLineFormatted("[T3] 7 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §7Gray Text");
                ConsoleWriter.WriteLineFormatted("[T3] 8 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §8Dark Gray Text");
                ConsoleWriter.WriteLineFormatted("[T3] 9 ---");
                ConsoleWriter.WriteLineFormatted("[T3] §9Blue Text");
                ConsoleWriter.WriteLineFormatted("[T3] a ---");
                ConsoleWriter.WriteLineFormatted("[T3] §aGreen Text");
                ConsoleWriter.WriteLineFormatted("[T3] b ---");
                ConsoleWriter.WriteLineFormatted("[T3] §bAqua Text");
                ConsoleWriter.WriteLineFormatted("[T3] c ---");
                ConsoleWriter.WriteLineFormatted("[T3] §cRed Text");
                ConsoleWriter.WriteLineFormatted("[T3] d ---");
                ConsoleWriter.WriteLineFormatted("[T3] §dLight Purple Text");
                ConsoleWriter.WriteLineFormatted("[T3] e ---");
                ConsoleWriter.WriteLineFormatted("[T3] §eYellow Text");
                ConsoleWriter.WriteLineFormatted("[T3] f ---");
                ConsoleWriter.WriteLineFormatted("[T3] §fWhite Text");
                ConsoleWriter.WriteLineFormatted("[T3] k ---");
                ConsoleWriter.WriteLineFormatted("[T3] §kObfuscated Text§r(Obfuscated)");
                ConsoleWriter.WriteLineFormatted("[T3] l ---");
                ConsoleWriter.WriteLineFormatted("[T3] §lBold Text");
                ConsoleWriter.WriteLineFormatted("[T3] m ---");
                ConsoleWriter.WriteLineFormatted("[T3] §mStrikethrough Text");
                ConsoleWriter.WriteLineFormatted("[T3] n ---");
                ConsoleWriter.WriteLineFormatted("[T3] §nUnderline Text");
                ConsoleWriter.WriteLineFormatted("[T3] o ---");
                ConsoleWriter.WriteLineFormatted("[T3] §oItalic Text");
                ConsoleWriter.WriteLineFormatted("[T3] r ---");
                ConsoleWriter.WriteLineFormatted("[T3] §k§rTesting§b§rThis text should not have effects!");
                ConsoleWriter.WriteLineFormatted("[T3] mixed ---");
                ConsoleWriter.WriteLineFormatted("[T3] §aText §cwith §bMixed §1C§2o§3l§4o§5r§6s§a!");
                ConsoleWriter.WriteLine         ("[T3] Truth:  §ktesting1§ktesting2§rtesting3§k§r");
                ConsoleWriter.WriteLineFormatted("[T3] Actual: §ktesting1§ktesting2§rtesting3§k§r");
                ConsoleWriter.WriteLine         ("[T3] Truth:  testing4§ktesting5§rtesting6§k§rtestafter §ka§ke§ri§ro§ku§r  textafter");
                ConsoleWriter.WriteLineFormatted("[T3] Actual: testing4§k§m§etesting5§rtesting6§k§rtestafter §ka§ke§ri§ro§ku§r  textafter");
            })) {IsBackground = true};
            
            t1.Start();
            t2.Start();
            tF.Start();
            
            ConsoleReader.BeginReadThread();
            ConsoleReader.MessageReceived += (sender, s) => {
                if (s.Equals("cancel"))
                    ConsoleReader.StopReadThread();
                else {
                    ConsoleWriter.WriteLine(s);
                }
            };
        }
    }
}