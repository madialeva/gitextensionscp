namespace GitExtensions.Extensibility.Git;

public class GitUIPostActionEventArgs : GitUIEventArgs
{
    public bool ActionDone { get; }

    public GitUIPostActionEventArgs(IWindow? ownerForm, IGitUICommands gitUICommands, bool actionDone)
        : base(ownerForm, gitUICommands)
    {
        ActionDone = actionDone;
    }
}
