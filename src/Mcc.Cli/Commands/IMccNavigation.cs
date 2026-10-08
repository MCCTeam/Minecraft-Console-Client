namespace Mcc.Cli.Commands;

/// <summary>MCC-specific workspace navigation separate from portable presentation.</summary>
public interface IMccNavigation
{
    /// <summary>Opens MCC management workspaces.</summary>
    bool TryOpenMccMenu();
}
