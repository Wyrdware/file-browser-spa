
namespace TestProject.FileBrowser {

    public sealed record DirectoryListing(
        string Path,
        IReadOnlyList<FolderEntry> Folders,
        IReadOnlyList<FileEntry> Files
    );
    public sealed record FolderEntry(
        string Name
    );
    public sealed record FileEntry(
        string Name,
        long SizeBytes
    );
    public sealed record SearchResults(
        string Query,
        IReadOnlyList<SearchResultEntry> Entries
    );
    public sealed record SearchResultEntry(
        string Name,
        string Path,
        bool IsFolder
    );
    public sealed record FileOperationResult(
        bool Success,
        string Message
    );
    public sealed record MoveRequest(
        string Source,
        string Dest
    );
    public sealed record CopyRequest(
        string Source,
        string Dest
    );
}
