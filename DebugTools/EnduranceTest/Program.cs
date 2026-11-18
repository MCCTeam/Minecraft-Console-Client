using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Physics;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.PacketPalettes;

namespace EnduranceTest;

internal static class Program
{
    private static void Main(string[] args)
    {
        int durationSeconds = 3600; // Default 1 hour
        bool quickMode = false;
        string csvPath = "memory_leak_test_1hour.csv";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--duration" && i + 1 < args.Length && int.TryParse(args[i + 1], out int d))
                durationSeconds = d;
            else if (args[i] == "--csv" && i + 1 < args.Length)
                csvPath = args[i + 1];
            else if (args[i] == "--quick")
                quickMode = true;
        }

        Console.WriteLine($"=== MCC Memory Leak & Endurance Test Runner ===");
        Console.WriteLine($"Mode: {(quickMode ? "Quick High-Volume (50,000 iterations)" : $"Real-Time Endurance ({durationSeconds} seconds / {durationSeconds / 60} minutes)")}");
        Console.WriteLine($"CSV Log: {Path.GetFullPath(csvPath)}");
        Console.WriteLine();

        // Initialize CSV header
        File.WriteAllText(csvPath, "Timestamp,ElapsedSeconds,WorkingSetMB,PrivateMemoryMB,GCTotalMemoryMB,Gen0,Gen1,Gen2,Iterations\n");

        Process currentProcess = Process.GetCurrentProcess();
        Stopwatch sw = Stopwatch.StartNew();
        Stopwatch logSw = Stopwatch.StartNew();

        World world = new();
        Block.Palette = new Palette120();
        Random rng = new(42);

        // Pre-create reusable mock chunk blocks
        Block stoneBlock = new(1);
        Block dirtBlock = new(2);

        long iterations = 0;
        int maxIterations = quickMode ? 50000 : int.MaxValue;

        Console.WriteLine($"{"Elapsed",-10} | {"WorkingSet",-12} | {"PrivateMem",-12} | {"GCTotalMem",-12} | {"Gen0/1/2",-14} | {"Iterations",-12}");
        Console.WriteLine(new string('-', 82));

        void LogMetrics()
        {
            currentProcess.Refresh();
            double workingSetMb = currentProcess.WorkingSet64 / (1024.0 * 1024.0);
            double privateMemoryMb = currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
            double gcMemoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            int g0 = GC.CollectionCount(0);
            int g1 = GC.CollectionCount(1);
            int g2 = GC.CollectionCount(2);
            double elapsedSec = sw.Elapsed.TotalSeconds;

            string line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss},{elapsedSec:F1},{workingSetMb:F2},{privateMemoryMb:F2},{gcMemoryMb:F2},{g0},{g1},{g2},{iterations}";
            File.AppendAllText(csvPath, line + "\n");

            Console.WriteLine($"{sw.Elapsed:hh\\:mm\\:ss}   | {workingSetMb,8:F2} MB | {privateMemoryMb,8:F2} MB | {gcMemoryMb,8:F2} MB | {g0,4}/{g1,3}/{g2,2}     | {iterations,12:N0}");
        }

        // Initial log
        LogMetrics();

        // Mock test payload for Zlib compression/decompression
        byte[] samplePacket = new byte[1024];
        rng.NextBytes(samplePacket);
        byte[] compressedPacket = ZlibUtils.Compress(samplePacket);

        while (sw.Elapsed.TotalSeconds < durationSeconds && iterations < maxIterations)
        {
            iterations++;

            // 1. World & Chunk Operations (Map Painting / Terrain Churn)
            int chunkX = (int)(iterations % 16) - 8;
            int chunkZ = (int)((iterations / 16) % 16) - 8;
            Chunk chunk = new();
            chunk.FillWithBlock(stoneBlock);
            chunk.SetWithoutCheck(2, 5, 2, dirtBlock);
            world.StoreChunk(chunkX, 0, chunkZ, 16, chunk, true);

            Block b = world.GetBlock(new Location(chunkX * 16 + 2, 5, chunkZ * 16 + 2));
            _ = b.BlockId;

            // Occasional World.FindBlock scan (simulating map art bot block finding)
            if (iterations % 50 == 0)
            {
                var found = world.FindBlock(new Location(0, 0, 0), Material.Stone, 16);
                _ = found.Count;
            }

            // Periodic chunk cleanup to simulate moving across world
            if (iterations % 500 == 0)
            {
                world.Clear();
            }

            // 2. Inventory & Container Operations
            Container container = new(1, ContainerType.Generic_9x3, "Chest");
            for (int slot = 0; slot < 27; slot++)
            {
                ItemType it = (slot % 2 == 0) ? ItemType.Stone : ItemType.Egg;
                container.Items[slot] = new Item(it, 16, null);
            }
            _ = container.GetEmpytSlots();
            foreach (var item in container.Items.Values)
            {
                _ = item.Type.IsFood();
                _ = item.Type.IsStackable();
                _ = item.Type.StackCount();
            }

            // 3. Tool Lookups & Material Classification
            _ = Material2Tool.GetCorrectToolForBlock(Material.Stone);
            _ = Material2Tool.GetCorrectToolForBlock(Material.OakLog);
            _ = Material2Tool.IsUnbreakable(Material.Bedrock);

            // 4. Palette Reverse Mapping
            var itemPal = new ItemPalette120();
            int stoneId = itemPal.ToId(ItemType.Stone);
            _ = itemPal.FromId(stoneId);

            var packetPal = new PacketPalette1202();
            _ = packetPal.GetType();

            // 5. Packet Decompression Simulation
            byte[] decompressed = ZlibUtils.Decompress(compressedPacket, samplePacket.Length);
            _ = decompressed.Length;

            // 6. Logging cadence
            if (quickMode)
            {
                if (iterations % 5000 == 0)
                    LogMetrics();
            }
            else
            {
                // In endurance mode, throttle to 20 TPS (50ms per tick)
                Thread.Sleep(50);

                if (logSw.Elapsed.TotalSeconds >= 30)
                {
                    logSw.Restart();
                    LogMetrics();
                }
            }
        }

        // Final log
        LogMetrics();

        Console.WriteLine(new string('-', 82));
        Console.WriteLine($"Test Completed Successfully! Total iterations: {iterations:N0}, Elapsed: {sw.Elapsed:hh\\:mm\\:ss}");
    }
}
