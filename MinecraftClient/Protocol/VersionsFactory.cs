using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MinecraftClient.Protocol.Packets;
using MinecraftClient.Protocol.Packets.Inbound;
using MinecraftClient.Protocol.Packets.Outbound;
using MinecraftClient.Protocol.WorldProcessors;

namespace MinecraftClient.Protocol
{
    internal static class VersionsFactory
    {
        public static Dictionary<int, IInboundGamePacketHandler> InboundHandlers(int protocolVersion)
        {
            return Instantiate<IInboundGamePacketHandler>(protocolVersion);
        }

        public static Dictionary<OutboundTypes, IOutboundGamePacket> OutboundHandlers(int protocolVersion)
        {
            var handlers = Instantiate<IOutboundGamePacket>(protocolVersion);
            var res = new Dictionary<OutboundTypes, IOutboundGamePacket>();
            foreach (var gamePacket in handlers)
            {
                res.Add(gamePacket.Value.Type(), gamePacket.Value);
            }

            return res;
        }

        private static Dictionary<int, T> Instantiate<T>(int protocolVersion) where T : IGamePacketHandler
        {
            var allHandlers = new Dictionary<int, SortedDictionary<int, T>>();

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(T)) && !x.IsAbstract))
            {
                var i = (T) Activator.CreateInstance(t);
                if (!allHandlers.TryGetValue(i.PacketIntType(), out var typeHandlers))
                {
                    typeHandlers = new SortedDictionary<int, T>();
                    allHandlers.Add(i.PacketIntType(), typeHandlers);
                }

                typeHandlers.Add(i.MinVersion(), i);
            }

            var latestHandlers = new Dictionary<int, T>();
            foreach (var handler in allHandlers)
            {
                foreach (var ins in handler.Value.OrderByDescending(x => x.Key))
                {
                    if (protocolVersion >= ins.Key)
                    {
                        latestHandlers.Add(ins.Value.PacketId(), ins.Value);
                        break;
                    }
                }
            }

            return latestHandlers;
        }

        public static T WorldProcessor<T>(int protocolVersion) where T : IWorldProcessor
        {
            var allHandlers = new SortedList<int, T>();

            foreach (var t in Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(T)) && !x.IsAbstract))
            {
                var i = (T) Activator.CreateInstance(t);
                allHandlers.Add(i.MinVersion(), i);
            }

            foreach (var handler in allHandlers.OrderByDescending(x => x.Key))
            {
                if (protocolVersion >= handler.Key)
                {
                    return handler.Value;
                }
            }

            return default(T);
        }
    }
}