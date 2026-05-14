namespace TestProject.FileBrowser {

    public class BrowseSystem {

        private readonly string _homeDirectory;

        public BrowseSystem(string homeDirectory) {
            _homeDirectory = Path.GetFullPath(homeDirectory);
            if (!Directory.Exists(_homeDirectory)) {
                Directory.CreateDirectory(_homeDirectory);
            }
        }

        public DirectoryListing GetDirectoryListing(string relativePath) {
            string fullPath = ResolvePath(relativePath);
            DirectoryInfo dirInfo = new(fullPath);

            if (!dirInfo.Exists) {
                throw new DirectoryNotFoundException($"Directory not found: {relativePath}");
            }

            List<FolderEntry> folders = dirInfo.GetDirectories()
                .Select(d => new FolderEntry(d.Name))
                .ToList();

            List<FileEntry> files = dirInfo.GetFiles()
                .Select(f => new FileEntry(f.Name, f.Length))
                .ToList();

            return new DirectoryListing(
                Path: NormalizeRelativePath(relativePath),
                Folders: folders,
                Files: files
            );
        }

        public SearchResults Search(string query, string? relativePath) {
            string basePath = relativePath is not null
                ? ResolvePath(relativePath)
                : _homeDirectory;

            if (!Directory.Exists(basePath)) {
                throw new DirectoryNotFoundException($"Directory not found: {relativePath}");
            }

            string pattern = $"*{query}*";
            List<SearchResultEntry> entries = new();

            try {
                foreach (string dir in Directory.EnumerateDirectories(basePath, pattern, SearchOption.AllDirectories)) {
                    string name = Path.GetFileName(dir);
                    string rel = Path.GetRelativePath(_homeDirectory, dir).Replace('\\', '/');
                    entries.Add(new SearchResultEntry(name, rel, IsFolder: true));
                }

                foreach (string file in Directory.EnumerateFiles(basePath, pattern, SearchOption.AllDirectories)) {
                    string name = Path.GetFileName(file);
                    string rel = Path.GetRelativePath(_homeDirectory, file).Replace('\\', '/');
                    entries.Add(new SearchResultEntry(name, rel, IsFolder: false));
                }
            }
            catch (UnauthorizedAccessException) {
                // Skip directories we can't access.
            }

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
        }

        public void Move(string sourcePath, string destPath) {
            string fullSource = ResolvePath(sourcePath);
            string fullDest = ResolvePath(destPath);

            if (Directory.Exists(fullSource)) {
                Directory.Move(fullSource, fullDest);
            }
            else if (File.Exists(fullSource)) {
                File.Move(fullSource, fullDest, overwrite: false);
            }
            else {
                throw new FileNotFoundException($"Source not found: {sourcePath}");
            }
        }

        public void Copy(string sourcePath, string destPath) {
            string fullSource = ResolvePath(sourcePath);
            string fullDest = ResolvePath(destPath);

            if (Directory.Exists(fullSource)) {
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
        }

        //--- Helpers ---

        private string ResolvePath(string relativePath) {
            string combined = Path.Combine(_homeDirectory, relativePath ?? "");
            string fullPath = Path.GetFullPath(combined);
            ValidatePath(fullPath);
            return fullPath;
        }

        private void ValidatePath(string fullPath) {
            if (!fullPath.StartsWith(_homeDirectory, StringComparison.OrdinalIgnoreCase)) {
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