using System;
using System.Collections.Generic;
using System.Linq;
using static MinecraftClient.Protocol.Message.LastSeenMessageList;

namespace MinecraftClient.Protocol.Message
{
    /// <summary>
    /// A list of messages a client has seen.
    /// </summary>
    public class LastSeenMessageList
    {
        public static readonly LastSeenMessageList EMPTY = new(Array.Empty<AcknowledgedMessage>());
        public static readonly int MAX_ENTRIES = 5;

        public AcknowledgedMessage[] entries;

        public LastSeenMessageList(AcknowledgedMessage[] list)
        {
            entries = list;
        }

        public void WriteForSign(List<byte> data)
        {
            foreach (AcknowledgedMessage entry in entries)
            {
                data.Add(70);
                data.AddRange(entry.profileId.ToBigEndianBytes());
                data.AddRange(entry.signature);
            }
        }

        /// <summary>
        ///  A pair of a player's UUID and the signature of the last message they saw, used as an entry of LastSeenMessageList.
        /// </summary>
        public record AcknowledgedMessage
        {
            public bool pending;
            public Guid profileId;
            public byte[] signature;

            public AcknowledgedMessage(Guid profileId, byte[] lastSignature, bool pending)
            {
                this.profileId = profileId;
                this.signature = lastSignature;
                this.pending = pending;
            }

            public AcknowledgedMessage UnmarkAsPending()
            {
                return this.pending ? new AcknowledgedMessage(profileId, signature, false) : this;
            }
        }

        /// <summary>
        /// A record of messages acknowledged by a client.
        /// This holds the messages the client has recently seen, as well as the last message they received, if any.
        /// </summary>
        public class Acknowledgment
        {
            public LastSeenMessageList lastSeen;
            public AcknowledgedMessage? lastReceived;

            public Acknowledgment(LastSeenMessageList lastSeenMessageList, AcknowledgedMessage? lastReceivedMessage)
            {
                lastSeen = lastSeenMessageList;
                lastReceived = lastReceivedMessage;
            }
        }
    }

    /// <summary>
    /// Collects the message that are last seen by a client.
    /// The message, along with the "last received" message, forms an "acknowledgment" of received messages.
    /// They are sent to the server when the client has enough messages received or when they send a message.
    /// The maximum amount of message entries are specified in the constructor.The vanilla clients collect 5 entries.
    /// Calling add adds the message to the beginning of the entries list, and evicts the oldest message.
    /// If there are entries with the same sender profile ID, the older entry will be replaced with null instead of filling the hole.
    /// </summary>
    public class LastSeenMessagesCollector
    {
        private readonly LastSeenMessageList.AcknowledgedMessage?[] acknowledgedMessages;
        private int nextIndex = 0;
        internal int messageCount { private set; get; } = 0;
        private LastSeenMessageList.AcknowledgedMessage? lastEntry = null;
        private LastSeenMessageList lastSeenMessages;

        public LastSeenMessagesCollector(int size)
        {
            lastSeenMessages = LastSeenMessageList.EMPTY;
            acknowledgedMessages = new LastSeenMessageList.AcknowledgedMessage[size];
        }

        public void Add_1_19_2(LastSeenMessageList.AcknowledgedMessage entry)
        {
            LastSeenMessageList.AcknowledgedMessage? lastEntry = entry;

            for (int i = 0; i < messageCount; ++i)
            {
                LastSeenMessageList.AcknowledgedMessage curEntry = acknowledgedMessages[i]!;
                acknowledgedMessages[i] = lastEntry;
                lastEntry = curEntry;
                if (curEntry.profileId == entry.profileId)
                {
                    lastEntry = null;
                    break;
                }
            }

            if (lastEntry != null && messageCount < acknowledgedMessages.Length)
                acknowledgedMessages[messageCount++] = lastEntry;

            LastSeenMessageList.AcknowledgedMessage[] msgList = new LastSeenMessageList.AcknowledgedMessage[messageCount];
            for (int i = 0; i < messageCount; ++i)
                msgList[i] = acknowledgedMessages[i]!;
            lastSeenMessages = new LastSeenMessageList(msgList);
        }

        public bool Add_1_19_3(LastSeenMessageList.AcknowledgedMessage entry, bool displayed)
        {
            // net.minecraft.network.message.LastSeenMessagesCollector#add(net.minecraft.network.message.MessageSignatureData, boolean)
            // net.minecraft.network.message.LastSeenMessagesCollector#add(net.minecraft.network.message.AcknowledgedMessage)
            if (lastEntry != null && entry.signature.SequenceEqual(lastEntry.signature))
                return false;
            lastEntry = entry;

            int index = nextIndex;
            nextIndex = (index + 1) % acknowledgedMessages.Length;

            ++messageCount;
            acknowledgedMessages[index] = displayed ? entry : null;

            return true;
        }

        public Tuple<AcknowledgedMessage[], byte[], int> Collect_1_19_3()
        {
            // net.minecraft.network.message.LastSeenMessagesCollector#collect
            int count = ResetMessageCount();
            byte[] bitset = new byte[3]; // new Bitset(20); Todo: Use a complete bitset implementation.
            List<AcknowledgedMessage> objectList = new(acknowledgedMessages.Length);
            for (int j = 0; j < acknowledgedMessages.Length; ++j)
            {
                int k = (nextIndex + j) % acknowledgedMessages.Length;
                AcknowledgedMessage? acknowledgedMessage = acknowledgedMessages[k];
                if (acknowledgedMessage == null)
                    continue;
                bitset[j / 8] |= (byte)(1 << (j % 8)); // bitSet.set(j, true);
                objectList.Add(acknowledgedMessage);
                acknowledgedMessages[k] = acknowledgedMessage.UnmarkAsPending();
            }
            return new(objectList.ToArray(), bitset, count);
        }

        public LastSeenMessageList GetLastSeenMessages()
        {
            return lastSeenMessages;
        }

        public int ResetMessageCount()
        {
            // net.minecraft.network.message.LastSeenMessagesCollector#resetMessageCount
            int cnt = messageCount;
            messageCount = 0;
            return cnt;
        }
    }
}
