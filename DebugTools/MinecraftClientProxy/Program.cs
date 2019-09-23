using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClientProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Waiting for client on port 25565...");
            TcpListener listener = new TcpListener(IPAddress.Any, 25565);
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();
            
            Console.WriteLine("Connecting to server on port 25566...");
            TcpClient server = new TcpClient("localhost", 25566);

            Console.WriteLine("Starting proxy...\n");
            new PacketProxy(client, server).Run();

            Console.ReadLine();
        }
    }
}
