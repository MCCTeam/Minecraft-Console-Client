namespace MinecraftClient.Scripting;

public class BotMovementLock
{
    private static BotMovementLock? InstancePrivate;
    private string _heldBy = string.Empty;

    private BotMovementLock()
    {
        InstancePrivate = this;
    }

    public static BotMovementLock? Instance => InstancePrivate ??= new BotMovementLock();

    public bool Lock(string owner)
    {
        if (owner.Trim().Length == 0 || _heldBy.Length > 0)
            return false;

        _heldBy = owner.Trim();
        return true;
    }

    public bool UnLock(string owner)
    {
        if (owner.Trim().Length == 0 || _heldBy.Length == 0 || !_heldBy.ToLower().Equals(owner.ToLower().Trim()))
            return false;

        _heldBy = string.Empty;
        return true;
    }

    public bool IsLocked => _heldBy.Length > 0;
    public string LockedBy => _heldBy;
}