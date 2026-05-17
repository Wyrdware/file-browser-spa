using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TestProject.FileBrowser {

    public class BrowseSystemOptions {
        public string HomeDirectory { get; set; } = "";
    }

    public class BrowseSystem {

        private readonly string _homeDirectory;
        private readonly ILogger<BrowseSystem> _logger;
        private readonly IndexService _indexService;

        public BrowseSystem(IOptions<BrowseSystemOptions> options, ILogger<BrowseSystem> logger) {
            _homeDirectory = Path.GetFullPath(options.Value.HomeDirectory);
            _logger = logger;
            if (!Directory.Exists(_homeDirectory)) {
                Directory.CreateDirectory(_homeDirectory);
            }

            _indexService = new IndexService(new List<DirectoryEntry>());
            PopulateIndex();
        }

        private void PopulateIndex() {
            PopulateDirectory(_homeDirectory);
            _logger.LogInformation("Index populated from {HomeDirectory}", _homeDirectory);
        }

        private void PopulateDirectory(string fullPath) {
            string relativePath = GetRelPath(fullPath, _homeDirectory);

            DirectoryInfo dirInfo = new(fullPath);
            List<string> children = new();

            foreach (DirectoryInfo subDir in dirInfo.GetDirectories()) {
                string childRel = GetRelPath(subDir.FullName, _homeDirectory);
                children.Add(childRel);
                PopulateDirectory(subDir.FullName);
            }

            foreach (FileInfo file in dirInfo.GetFiles()) {
                string childRel = GetRelPath(file.FullName, _homeDirectory);
                children.Add(childRel);
                _indexService.Insert(new DirectoryEntry(childRel, new List<string>(), file.Length, false));
            }

            _indexService.Insert(new DirectoryEntry(relativePath, children, 0, true));
        }

        public DirectoryListing GetDirectoryListing(string relativePath) {
            string normalized = NormalizeRelativePath(relativePath);
            string lookupPath = normalized.TrimStart('/').Replace('\\', '/');

            if (!_indexService.TryGetEntry(lookupPath, out DirectoryEntry? folder) || !folder!.IsFolder) {
                throw new DirectoryNotFoundException($"Directory not found: {relativePath}");
            }

            List<FolderEntry> folders = new();
            List<FileEntry> files = new();

            foreach (string childPath in folder.Children) {
                if (_indexService.TryGetEntry(childPath, out DirectoryEntry? child)) {
                    string name = Path.GetFileName(childPath);
                    if (child!.IsFolder) {
                        folders.Add(new FolderEntry(name));
                    } else {
                        files.Add(new FileEntry(name, child.SizeBytes));
                    }
                }
            }

            return new DirectoryListing(
                Path: normalized,
                Folders: folders,
                Files: files
            );
        }

        public SearchResults Search(string query, string? relativePath) {
            string? folderPath = relativePath?.Replace('\\', '/');
            List<DirectoryEntry> matches = _indexService.Search(query, folderPath);

            List<SearchResultEntry> entries = matches
                .Select(e => new SearchResultEntry(
                    Path.GetFileName(e.Path),
                    e.Path,
                    e.IsFolder
                ))
                .ToList();

            return new SearchResults(Query: query, Entries: entries);
        }

        public FileStream GetFileStream(string relativePath) {
            string fullPath = ResolvePath(relativePath);

            if (!File.Exists(fullPath)) {
                throw new FileNotFoundException($"File not found: {relativePath}");
            }

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void Upload(string relativePath, Stream content, string fileName) {
            string directoryPath = ResolvePath(relativePath);

            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, fileName);
            ValidatePath(filePath);

            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
            content.CopyTo(fileStream);

            string fileRel = GetRelPath(filePath, _homeDirectory);
            string parentRel = GetRelPath(directoryPath, _homeDirectory);
            _indexService.Insert(new DirectoryEntry(fileRel, new List<string>(), new FileInfo(filePath).Length, false));
            AddChildToParent(parentRel, fileRel);
        }

        public void Delete(string relativePath) {
            string fullPath = ResolvePath(relativePath);

            if (Directory.Exists(fullPath)) {
                Directory.Delete(fullPath, recursive: true);
            }
            else if (File.Exists(fullPath)) {
                File.Delete(fullPath);
            }
            else {
                throw new FileNotFoundException($"Path not found: {relativePath}");
            }

            string rel = GetRelPath(fullPath, _homeDirectory);
            string parentRel = GetParentPath(rel);
            RemoveChildFromParent(parentRel, rel);
            RemoveEntryRecursive(rel);
        }

        public void Move(string sourcePath, string destPath) {
            string fullSource = ResolvePath(sourcePath);
            string fullDest = ResolvePath(destPath);
            bool isDir = Directory.Exists(fullSource);

            if (isDir) {
                Directory.Move(fullSource, fullDest);
            }
            else if (File.Exists(fullSource)) {
                File.Move(fullSource, fullDest, overwrite: false);
            }
            else {
                throw new FileNotFoundException($"Source not found: {sourcePath}");
            }

            string sourceRel = GetRelPath(fullSource, _homeDirectory);
            string destRel = GetRelPath(fullDest, _homeDirectory);
            string oldParent = GetParentPath(sourceRel);
            string newParent = GetParentPath(destRel);

            RemoveChildFromParent(oldParent, sourceRel);
            RemoveEntryRecursive(sourceRel);

            if (isDir) {
                IndexDirectoryRecursive(fullDest);
            } else {
                _indexService.Insert(new DirectoryEntry(destRel, new List<string>(), new FileInfo(fullDest).Length, false));
            }
            AddChildToParent(newParent, destRel);
        }

        public void Copy(string sourcePath, string destPath) {
            string fullSource = ResolvePath(sourcePath);
            string fullDest = ResolvePath(destPath);
            bool isDir = Directory.Exists(fullSource);

            if (isDir) {
                CopyDirectory(fullSource, fullDest);
            }
            else if (File.Exists(fullSource)) {
                string? destDir = Path.GetDirectoryName(fullDest);
                if (destDir is not null && !Directory.Exists(destDir)) {
                    Directory.CreateDirectory(destDir);
                }
                File.Copy(fullSource, fullDest, overwrite: false);
            }
            else {
                throw new FileNotFoundException($"Source not found: {sourcePath}");
            }

            string destRel = GetRelPath(fullDest, _homeDirectory);
            string newParent = GetParentPath(destRel);

            if (isDir) {
                IndexDirectoryRecursive(fullDest);
            } else {
                _indexService.Insert(new DirectoryEntry(destRel, new List<string>(), new FileInfo(fullDest).Length, false));
            }
            AddChildToParent(newParent, destRel);
        }

        //--- Index Helpers ---

        private static string GetRelPath(string fullPath, string homeDirectory) {
            string rel = Path.GetRelativePath(homeDirectory, fullPath).Replace('\\', '/');
            return rel == "." ? "" : rel;
        }

        private static string GetParentPath(string relativePath) {
            int lastSlash = relativePath.LastIndexOf('/');
            return lastSlash >= 0 ? relativePath[..lastSlash] : "";
        }

        private void AddChildToParent(string parentPath, string childPath) {
            if (_indexService.TryGetEntry(parentPath, out DirectoryEntry? parent)) {
                if (!parent!.Children.Contains(childPath))
                    parent.Children.Add(childPath);
            }
        }

        private void RemoveChildFromParent(string parentPath, string childPath) {
            if (_indexService.TryGetEntry(parentPath, out DirectoryEntry? parent)) {
                parent!.Children.Remove(childPath);
            }
        }

        private void RemoveEntryRecursive(string relativePath) {
            if (_indexService.TryGetEntry(relativePath, out DirectoryEntry? entry)) {
                if (entry!.IsFolder) {
                    foreach (string child in new List<string>(entry.Children)) {
                        RemoveEntryRecursive(child);
                    }
                }
                _indexService.Remove(relativePath);
            }
        }

        private void IndexDirectoryRecursive(string fullPath) {
            string rel = GetRelPath(fullPath, _homeDirectory);
            DirectoryInfo dirInfo = new(fullPath);
            List<string> children = new();

            foreach (DirectoryInfo subDir in dirInfo.GetDirectories()) {
                string childRel = GetRelPath(subDir.FullName, _homeDirectory);
                children.Add(childRel);
                IndexDirectoryRecursive(subDir.FullName);
            }

            foreach (FileInfo file in dirInfo.GetFiles()) {
                string childRel = GetRelPath(file.FullName, _homeDirectory);
                children.Add(childRel);
                _indexService.Insert(new DirectoryEntry(childRel, new List<string>(), file.Length, false));
            }

            _indexService.Insert(new DirectoryEntry(rel, children, 0, true));
        }

        //--- Helpers ---

        private string ResolvePath(string relativePath) {
            string combined = Path.Combine(_homeDirectory, relativePath ?? "");
            string fullPath = Path.GetFullPath(combined);
            ValidatePath(fullPath);
            return fullPath;
        }

        private void ValidatePath(string fullPath) {
            bool isHome = string.Equals(fullPath, _homeDirectory, StringComparison.OrdinalIgnoreCase);
            bool isInside = fullPath.StartsWith(_homeDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if (!isHome && !isInside) {
                throw new UnauthorizedAccessException("Access denied: path is outside the home directory.");
            }
        }

        private static string NormalizeRelativePath(string? path) {
            if (string.IsNullOrWhiteSpace(path)) return "/";
            string normalized = path.Replace('\\', '/').Trim('/');
            return "/" + normalized;
        }

        private static void CopyDirectory(string sourceDir, string destDir) {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir)) {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: false);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir)) {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}
