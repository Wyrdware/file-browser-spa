
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
    public sealed record SearchResult(
        string Name,
        string Path
    );
}
