//MCCScript 1.0

MCC.LoadBot(new PacketCadenceCaptureBot());

//MCCScript Extensions

public class PacketCadenceCaptureBot : ChatBot
{
    private const int CaptureDurationSeconds = 5;
    private const int CaptureDurationTicks = CaptureDurationSeconds * 20;

    private readonly object _countsLock = new();
    private readonly Dictionary<string, int> _counts = new()
    {
        { "PlayerMovement", 0 },
        { "PlayerPosition", 0 },
        { "PlayerPositionAndRotation", 0 },
        { "PlayerRotation", 0 }
    };
    private readonly Dictionary<int, int> _rawOutgoingCounts = new();

    private bool _captureStarted;
    private bool _captureSupported;
    private bool _networkPacketEventEnabled;
    private int _ticksRemaining;
    private int _playerMovementPacketId = -1;
    private int _playerPositionPacketId = -1;
    private int _playerPositionAndRotationPacketId = -1;
    private int _playerRotationPacketId = -1;
    private string _profileName = "unsupported";

    public override void AfterGameJoined()
    {
        int protocolVersion = GetProtocolVersion();
        _captureSupported = TryConfigureProfile(protocolVersion);

        LogToConsole($"Packet cadence profile: {_profileName} (protocol v{protocolVersion})");

        if (!_captureSupported)
        {
            LogToConsole("Packet cadence capture does not know the outgoing movement IDs for this protocol.");
            UnloadBot();
            return;
        }

        SetNetworkPacketEventEnabled(true);
        _networkPacketEventEnabled = true;

        _captureStarted = true;
        _ticksRemaining = CaptureDurationTicks;
        LogToConsole($"Capturing outgoing movement packets for {CaptureDurationSeconds} seconds.");
    }

    public override void Update()
    {
        if (!_captureStarted)
            return;

        if (--_ticksRemaining > 0)
            return;

        FinishCapture();
    }

    private void FinishCapture()
    {
        if (!_captureStarted)
            return;

        _captureStarted = false;

        int movementCount;
        int positionCount;
        int positionAndRotationCount;
        int rotationCount;

        lock (_countsLock)
        {
            movementCount = _counts["PlayerMovement"];
            positionCount = _counts["PlayerPosition"];
            positionAndRotationCount = _counts["PlayerPositionAndRotation"];
            rotationCount = _counts["PlayerRotation"];
        }

        int totalPackets = movementCount + positionCount + positionAndRotationCount + rotationCount;

        LogToConsole($"Packet cadence summary ({_profileName}): total={totalPackets}, " +
                     $"movement={movementCount}, position={positionCount}, " +
                     $"posrot={positionAndRotationCount}, rotation={rotationCount}");

        if (totalPackets == 0)
        {
            string rawSummary;
            lock (_countsLock)
            {
                var entries = new List<string>();
                foreach (var entry in _rawOutgoingCounts.OrderBy(entry => entry.Key))
                    entries.Add($"0x{entry.Key:X2}={entry.Value}");
                rawSummary = entries.Count > 0 ? string.Join(", ", entries) : "none";
            }

            LogToConsole($"Packet cadence raw outbound IDs: {rawSummary}");
        }

        UnloadBot();
    }

    public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
    {
        if (!_captureStarted || isLogin || isInbound)
            return;

        lock (_countsLock)
        {
            _rawOutgoingCounts.TryGetValue(packetID, out int rawCount);
            _rawOutgoingCounts[packetID] = rawCount + 1;

            if (packetID == _playerMovementPacketId)
                _counts["PlayerMovement"]++;
            else if (packetID == _playerPositionPacketId)
                _counts["PlayerPosition"]++;
            else if (packetID == _playerPositionAndRotationPacketId)
                _counts["PlayerPositionAndRotation"]++;
            else if (packetID == _playerRotationPacketId)
                _counts["PlayerRotation"]++;
        }
    }

    public override void OnUnload()
    {
        if (!_networkPacketEventEnabled)
            return;

        SetNetworkPacketEventEnabled(false);
        _networkPacketEventEnabled = false;
    }

    private bool TryConfigureProfile(int protocolVersion)
    {
        switch (protocolVersion)
        {
            case 47:
                _profileName = "1.8/1.8.9";
                _playerMovementPacketId = 0x03;
                _playerPositionPacketId = 0x04;
                _playerPositionAndRotationPacketId = 0x06;
                _playerRotationPacketId = 0x05;
                return true;

            case 766:
            case 767:
            case 768:
            case 769:
            case 770:
            case 771:
            case 772:
                _profileName = "1.20.6-1.21.8";
                _playerMovementPacketId = 0x1D;
                _playerPositionPacketId = 0x1A;
                _playerPositionAndRotationPacketId = 0x1B;
                _playerRotationPacketId = 0x1C;
                return true;

            case 773:
            case 774:
            case 775:
                _profileName = "1.21.9+";
                _playerMovementPacketId = 0x20;
                _playerPositionPacketId = 0x1D;
                _playerPositionAndRotationPacketId = 0x1E;
                _playerRotationPacketId = 0x1F;
                return true;

            default:
                return false;
        }
    }
}
