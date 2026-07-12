using System.Security.Cryptography;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using GitUIPluginInterfaces;

namespace GitExtensions.Extensibility.Git;

public interface IGitUICommands : IServiceProvider
{
    event EventHandler<GitUIEventArgs>? PostBrowseInitialize;
    event EventHandler<GitUIPostActionEventArgs>? PostCheckoutBranch;
    event EventHandler<GitUIPostActionEventArgs>? PostCheckoutRevision;
    event EventHandler<GitUIPostActionEventArgs>? PostCommit;
    event EventHandler<GitUIPostActionEventArgs>? PostEditGitIgnore;
    event EventHandler<GitUIEventArgs>? PostRegisterPlugin;
    event EventHandler<GitUIEventArgs>? PostRepositoryChanged;
    event EventHandler<GitUIPostActionEventArgs>? PostSettings;
    event EventHandler<GitUIPostActionEventArgs>? PostUpdateSubmodules;
    event EventHandler<GitUIEventArgs>? PreCheckoutBranch;
    event EventHandler<GitUIEventArgs>? PreCheckoutRevision;
    event EventHandler<GitUIEventArgs>? PreCommit;

    IBrowseRepo? BrowseRepo { get; set; }

    IGitModule Module { get; }

    /// <summary>
    /// RepoChangedNotifier.Notify() should be called after each action that changes repo state
    /// </summary>
    ILockableNotifier RepoChangedNotifier { get; }

    void AddCommitTemplate(string key, Func<string> addingText, byte[]? iconData, bool isRegex = false);
    void AddUpstreamRemote(IWindow? owner, IRepositoryHostPlugin gitHoster);
    IGitRemoteCommand CreateRemoteCommand();
    bool DoActionOnRepo(Func<bool> action);
    void OpenWithDifftool(IWindow? owner, IReadOnlyList<GitRevision?> revisions, string fileName, string? oldFileName, RevisionDiffKind diffKind, bool isTracked, string? customTool = null);
    void RaisePostBrowseInitialize(IWindow? owner);
    void RaisePostRegisterPlugin(IWindow? owner);
    void RemoveCommitTemplate(string key);
    bool RunCommand(IReadOnlyList<string> args);
    bool StartAddFilesDialog(IWindow? owner, string? addFiles = null);
    bool StartAddToGitIgnoreDialog(IWindow? owner, bool localExclude, params string[] filePattern);
    bool StartAmendCommitDialog(IWindow? owner, GitRevision revision);
    bool StartApplyPatchDialog(IWindow? owner, string? patchFile = null);
    bool StartArchiveDialog(IWindow? owner = null, GitRevision? revision = null, GitRevision? revision2 = null, string? path = null);
    void StartBatchFileProcessDialog(string batchFile);
    bool StartBrowseDialog(IWindow? owner, BrowseArguments? args = null);
    bool StartCheckoutBranch(IWindow? owner, IReadOnlyList<ObjectId>? containObjectIds);
    bool StartCheckoutBranch(IWindow? owner, string branch = "", bool remote = false, IReadOnlyList<ObjectId>? containObjectIds = null);
    bool StartCheckoutRemoteBranch(IWindow? owner, string branch);
    bool StartCheckoutRevisionDialog(IWindow? owner, string? revision = null);
    bool StartCherryPickDialog(IWindow? owner = null, GitRevision? revision = null);
    bool StartCherryPickDialog(IWindow? owner, IEnumerable<GitRevision> revisions);
    bool StartCleanupRepositoryDialog(IWindow? owner = null, string? path = null);
    bool StartCloneDialog(IWindow? owner, string url, EventHandler<GitModuleEventArgs> gitModuleChanged);
    bool StartCloneDialog(IWindow? owner, string? url = null, bool openedFromProtocolHandler = false, EventHandler<GitModuleEventArgs>? gitModuleChanged = null);
    void StartCloneForkFromHoster(IWindow? owner, IRepositoryHostPlugin gitHoster, EventHandler<GitModuleEventArgs>? gitModuleChanged);
    bool StartCommandLineProcessDialog(IWindow? owner, IGitCommand command);
    bool StartCommandLineProcessDialog(IWindow? owner, string? command, ArgumentString arguments);
    bool StartCommitDialog(IWindow? owner, string? commitMessage = null, bool showOnlyWhenChanges = false);
    bool StartCompareRevisionsDialog(IWindow? owner = null);
    bool StartCreateBranchDialog(IWindow? owner = null, ObjectId objectId = default, string? newBranchNamePrefix = null);
    bool StartCreateBranchDialog(IWindow? owner, string? branch);
    void StartCreatePullRequest(IWindow? owner);
    void StartCreatePullRequest(IWindow? owner, IRepositoryHostPlugin gitHoster, string? chooseRemote = null, string? chooseBranch = null);
    bool StartCreateTagDialog(IWindow? owner = null, GitRevision? revision = null);
    bool StartDeleteBranchDialog(IWindow? owner, IEnumerable<string> branches);
    bool StartDeleteBranchDialog(IWindow? owner, string branch);
    bool StartDeleteRemoteBranchDialog(IWindow? owner, string remoteBranch);
    bool StartDeleteTagDialog(IWindow? owner, string? tag);
    bool StartEditGitAttributesDialog(IWindow? owner = null);
    bool StartEditGitIgnoreDialog(IWindow? owner, bool localExcludes);
    bool StartFileEditorDialog(string? filename, bool showWarning = false, int? lineNumber = null);
    void StartFileHistoryDialog(IWindow? owner, string fileName, GitRevision? revision = null, bool filterByRevision = false, bool showBlame = false);
    bool StartFixupCommitDialog(IWindow? owner, GitRevision revision);
    bool StartFormCommitDiff(ObjectId objectId);
    bool StartFormatPatchDialog(IWindow? owner = null);
    bool StartGeneralSettingsDialog(IWindow? owner);
    bool StartGitCommandProcessDialog(IWindow? owner, ArgumentString arguments);
    bool StartInitializeDialog(IWindow? owner = null, string? dir = null, EventHandler<GitModuleEventArgs>? gitModuleChanged = null);
    bool StartInteractiveRebase(IWindow? owner, string onto);
    bool StartMailMapDialog(IWindow? owner = null);
    bool StartMergeBranchDialog(IWindow? owner, string? branch);
    bool StartPluginSettingsDialog(IWindow? owner);
    bool StartPullDialog(IWindow? owner = null, string? remoteBranch = null, string? remote = null, GitPullAction pullAction = GitPullAction.None);
    bool StartPullDialogAndPullImmediately(IWindow? owner = null, string? remoteBranch = null, string? remote = null, GitPullAction pullAction = GitPullAction.None);
    bool StartPullDialogAndPullImmediately(out bool pullCompleted, IWindow? owner = null, string? remoteBranch = null, string? remote = null, GitPullAction pullAction = GitPullAction.None);
    void StartPullRequestsDialog(IWindow? owner, IRepositoryHostPlugin gitHoster);
    bool StartPushDialog(IWindow? owner, bool pushOnShow);
    bool StartPushDialog(IWindow? owner, bool pushOnShow, bool forceWithLease, out bool pushCompleted, string? branchName = null);
    bool StartRebase(IWindow? owner, string onto);
    bool StartRebaseDialog(IWindow? owner, string? from, string? to, string? onto, bool interactive = false, bool startRebaseImmediately = true);
    bool StartRebaseDialog(IWindow? owner, string? onto);
    bool StartRebaseDialogWithAdvOptions(IWindow? owner, string onto, string from = "");

    /// <summary>
    /// Opens the FormRemotes.
    /// </summary>
    /// <param name="preselectRemote">Makes the FormRemotes initially select the given remote.</param>
    /// <param name="preselectLocal">Makes the FormRemotes initially show the tab "Default push behavior" and select the given local.</param>
    bool StartRemotesDialog(IWindow? owner, string? preselectRemote = null, string? preselectLocal = null);
    bool StartRenameDialog(IWindow? owner, string branch);
    bool StartRepoSettingsDialog(IWindow? owner);
    bool StartResetChangesDialog(IWindow? owner, IReadOnlyCollection<GitItemStatus> workTreeFiles, bool onlyWorkTree);
    bool StartResetCurrentBranchDialog(IWindow? owner, string branch);
    bool StartResolveConflictsDialog(IWindow? owner = null, bool offerCommit = true);
    bool StartRevertCommitDialog(IWindow? owner, GitRevision revision);
    bool StartSettingsDialog(IGitPlugin gitPlugin);
    bool StartSettingsDialog(IWindow? owner, SettingsPageReference? initialPage = null);
    bool StartSettingsDialog(Type pageType);
    bool StartSparseWorkingCopyDialog(IWindow? owner);
    bool StartSquashCommitDialog(IWindow? owner, GitRevision revision);
    bool StartStashDialog(IWindow? owner = null, bool manageStashes = true, string? initialStash = null);
    bool StartSubmodulesDialog(IWindow? owner);
    bool StartSyncSubmodulesDialog(IWindow? owner);
    bool StartTheContinueRebaseDialog(IWindow? owner);
    bool StartUpdateSubmoduleDialog(IWindow? owner, string submoduleLocalPath, string submoduleParentPath);
    bool StartUpdateSubmodulesDialog(IWindow? owner, string submoduleLocalPath = "");
    bool StartVerifyDatabaseDialog(IWindow? owner = null);
    bool StartViewPatchDialog(IWindow? owner, string? patchFile = null);
    bool StartViewPatchDialog(string patchFile);
    bool StashApply(IWindow? owner, string stashName);
    bool StashDrop(IWindow? owner, string stashName);
    bool StashPop(IWindow? owner, string stashName = "");
    bool StashSave(IWindow? owner, bool includeUntrackedFiles, bool keepIndex = false, string message = "", IReadOnlyList<string>? selectedFiles = null);
    bool StashStaged(IWindow? owner);
    void UpdateSubmodules(IWindow? owner);
    IGitUICommands WithGitModule(IGitModule module);
    IGitUICommands WithWorkingDirectory(string? workingDirectory);

    /// <summary>
    ///  Shows the create worktree dialog and optionally switches to the new worktree.
    /// </summary>
    /// <param name="owner">Owner window for dialogs.</param>
    /// <param name="mainWorktreePath">Path of the main worktree (used as the base directory).</param>
    /// <returns><see langword="true"/> if a worktree was created.</returns>
    bool WorktreeCreate(IWindow? owner, string mainWorktreePath);

    /// <summary>
    ///  Confirms with the user, deletes a worktree directory on disk, and runs <c>git worktree prune</c>.
    /// </summary>
    /// <param name="owner">Owner window for dialogs.</param>
    /// <param name="worktreePath">Absolute path of the worktree to delete.</param>
    /// <returns><see langword="true"/> if the user confirmed and the directory was successfully removed.</returns>
    bool WorktreeDelete(IWindow? owner, string worktreePath);

    /// <summary>
    ///  Optionally confirms with the user, then switches the current browse window to the specified worktree.
    /// </summary>
    /// <param name="owner">Owner window for the confirmation dialog. Must be or have <see cref="IBrowseRepo"/> as the owning <c>FormBrowse</c>.</param>
    /// <param name="worktreePath">Absolute path of the worktree to switch to.</param>
    /// <returns><see langword="true"/> if the switch was performed.</returns>
    bool WorktreeSwitch(IWindow? owner, string worktreePath);
}
