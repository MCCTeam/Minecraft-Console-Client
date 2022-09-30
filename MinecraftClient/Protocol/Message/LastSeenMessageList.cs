using System;
using System.Collections.Generic;

namespace MinecraftClient.Protocol.Message
{
    /// <summary>
    /// A list of messages a client has seen.
    /// </summary>
    public class LastSeenMessageList
    {
        public static readonly LastSeenMessageList EMPTY = new(Array.Empty<Entry>());
        public static readonly int MAX_ENTRIES = 5;

        public Entry[] entries;

        public LastSeenMessageList(Entry[] list)
        {
            entries = list;
        }

        public void WriteForSign(List<byte> data)
        {
            foreach (Entry entry in entries)
            {
                data.Add(70);
                data.AddRange(entry.profileId.ToBigEndianBytes());
                data.AddRange(entry.lastSignature);
            }
        }

        /// <summary>
        ///  A pair of a player's UUID and the signature of the last message they saw, used as an entry of LastSeenMessageList.
        /// </summary>
        public class Entry
        {
            public Guid profileId;
            public byte[] lastSignature;

            public Entry(Guid profileId, byte[] lastSignature)
            {
                this.profileId = profileId;
                this.lastSignature = lastSignature;
            }
        }

        /// <summary>
        /// A record of messages acknowledged by a client.
        /// This holds the messages the client has recently seen, as well as the last message they received, if any.
        /// </summary>
        public class Acknowledgment
        {
            public LastSeenMessageList lastSeen;
            public Entry? lastReceived;

            public Acknowledgment(LastSeenMessageList lastSeenMessageList, Entry? lastReceivedMessage)
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
        private readonly LastSeenMessageList.Entry[] entries;
        private int size = 0;
        private LastSeenMessageList lastSeenMessages;

        public LastSeenMessagesCollector(int size)
        {
            lastSeenMessages = LastSeenMessageList.EMPTY;
            entries = new LastSeenMessageList.Entry[size];
        }

        public void Add(LastSeenMessageList.Entry entry)
        {
            LastSeenMessageList.Entry? lastEntry = entry;

            for (int i = 0; i < size; ++i)
            {
                LastSeenMessageList.Entry curEntry = entries[i];
                entries[i] = lastEntry;
                lastEntry = curEntry;
                if (curEntry.profileId == entry.profileId)
                {
                    lastEntry = null;
                    break;
                }
            }

            if (lastEntry != null && size < entries.Length)
                entries[size++] = lastEntry;

            LastSeenMessageList.Entry[] msgList = new LastSeenMessageList.Entry[size];
            for (int i = 0; i < size; ++i)
                msgList[i] = entries[i];
            lastSeenMessages = new LastSeenMessageList(msgList);
        }

        public LastSeenMessageList GetLastSeenMessages()
        {
            return lastSeenMessages;
        }

    }
}
