using System.Text;

namespace TestProject.FileBrowser{
    public class IndexService
    {
        internal RadixTree<DirectoryEntry> _pathTree = new();
        internal RadixTree<List<DirectoryEntry>> _searchTree = new();

        public IndexService(List<DirectoryEntry> entries)
        {
            foreach(DirectoryEntry entry in entries)
            {
                Insert(entry);
            }
        }
        public void Insert(DirectoryEntry entry){
            _pathTree.Insert(entry.Path, entry);

            string fileName = ExtractFileName(entry.Path);
            if(_searchTree.TryGet(fileName, out List<DirectoryEntry>? existing)){
                existing!.Add(entry);
            } else {
                _searchTree.Insert(fileName, new List<DirectoryEntry>{ entry });
            }
        }
        public bool TryGetEntry(string path, out DirectoryEntry? directoryEntry){
            return _pathTree.TryGet(path, out directoryEntry);
        }
        public bool Remove(string path){
            bool removed = _pathTree.Remove(path);
            if(!removed) return false;

            string fileName = ExtractFileName(path);
            if(_searchTree.TryGet(fileName, out List<DirectoryEntry>? entries)){
                entries!.RemoveAll(e => e.Path == path);
                if(entries.Count == 0){
                    _searchTree.Remove(fileName);
                }
            }
            return true;
        }
        public List<DirectoryEntry> Search(string query, string? folderPath = null){
            List<List<DirectoryEntry>> matches = _searchTree.PrefixSearch(query);
            List<DirectoryEntry> results = new();
            foreach(List<DirectoryEntry> list in matches){
                foreach(DirectoryEntry entry in list){
                    if(folderPath == null || entry.Path.StartsWith(folderPath))
                        results.Add(entry);
                }
            }
            return results;
        }

        private static string ExtractFileName(string path){
            int lastSlash = path.LastIndexOf('\\');
            if(lastSlash < 0) lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
        }
    }

    public class RadixTree<TValue>
    {
        internal RadixNode<TValue> _root = new([]);

        public void Insert(string key, TValue value){
            byte[] pathBytes = Encoding.ASCII.GetBytes(key.ToLowerInvariant());
            RadixNode<TValue> currentNode = _root;
            int elementsFound = 0;

            while (elementsFound < pathBytes.Length){
                byte b = pathBytes[elementsFound];

                // No matching children, create a leaf
                if (!currentNode.Children.TryGetValue(b, out RadixNode<TValue> child)){
                    RadixNode<TValue> leaf = new(pathBytes[elementsFound..]);
                    leaf.Value = value;
                    currentNode.Children[b] = leaf;
                    return;
                }

                // Get shared prefix length between child label and remaining path
                int max = Math.Min(child.Label.Length, pathBytes.Length - elementsFound);
                int prefixLen = 0;
                while (prefixLen < max && child.Label[prefixLen] == pathBytes[elementsFound + prefixLen])
                    prefixLen++;

                // Full label match, descend
                if (prefixLen == child.Label.Length){
                    elementsFound += prefixLen;
                    currentNode = child;
                    continue;
                }

                // Partial label match, split
                RadixNode<TValue> intermediate = new(child.Label[..prefixLen]);

                child.Label = child.Label[prefixLen..];
                intermediate.Children[child.Label[0]] = child;

                currentNode.Children[b] = intermediate;

                byte[] keySuffix = pathBytes[(elementsFound + prefixLen)..];
                if (keySuffix.Length == 0){
                    intermediate.Value = value;
                } else {
                    RadixNode<TValue> leaf = new(keySuffix);
                    leaf.Value = value;
                    intermediate.Children[keySuffix[0]] = leaf;
                }
                return;
            }

            // Set value on current node
            currentNode.Value = value;
        }
        public bool TryGet(string key, out TValue? value){
            byte[] pathBytes = Encoding.ASCII.GetBytes(key.ToLowerInvariant());
            RadixNode<TValue> currentNode = _root;
            int elementsFound = 0;

            while(currentNode.Children.Count > 0 && elementsFound < pathBytes.Length){
                if(currentNode.Children.TryGetValue(pathBytes[elementsFound], out RadixNode<TValue> nextNode))
                {
                    // Verify every byte of the label matches the path
                    for(int i = 0; i < nextNode.Label.Length; i++){
                        if(elementsFound + i >= pathBytes.Length || nextNode.Label[i] != pathBytes[elementsFound + i]){
                            value = default;
                            return false;
                        }
                    }

                    currentNode = nextNode;
                    elementsFound += nextNode.Label.Length;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            if(elementsFound == pathBytes.Length && currentNode.HasValue){
                value = currentNode.Value!;
                return true;
            }

            value = default;
            return false;
        }
        public List<TValue> PrefixSearch(string prefix){
            byte[] pathBytes = Encoding.ASCII.GetBytes(prefix.ToLowerInvariant());
            RadixNode<TValue> currentNode = _root;
            int elementsFound = 0;

            while(elementsFound < pathBytes.Length){
                if(!currentNode.Children.TryGetValue(pathBytes[elementsFound], out RadixNode<TValue> nextNode))
                    return new();

                // Verify label matches
                int max = Math.Min(nextNode.Label.Length, pathBytes.Length - elementsFound);
                int prefixLen = 0;
                while(prefixLen < max && nextNode.Label[prefixLen] == pathBytes[elementsFound + prefixLen])
                    prefixLen++;

                // Prefix exhausted mid-label, subtree still valid
                if(elementsFound + prefixLen >= pathBytes.Length){
                    currentNode = nextNode;
                    break;
                }

                // Label not fully matched
                if(prefixLen != nextNode.Label.Length)
                    return new();

                elementsFound += prefixLen;
                currentNode = nextNode;
            }

            // Collect all values in the subtree
            List<TValue> results = new();
            Stack<RadixNode<TValue>> stack = new();
            stack.Push(currentNode);

            while(stack.Count > 0){
                RadixNode<TValue> node = stack.Pop();
                if(node.HasValue)
                    results.Add(node.Value!);

                foreach(RadixNode<TValue> childNode in node.Children.Values)
                    stack.Push(childNode);
            }

            return results;
        }
        public bool Remove(string key){
            byte[] pathBytes = Encoding.ASCII.GetBytes(key.ToLowerInvariant());
            RadixNode<TValue> currentNode = _root;
            int elementsFound = 0;
            List<(RadixNode<TValue> parent, byte childKey)> ancestors = new();

            // Walk the tree, tracking ancestors
            while(currentNode.Children.Count > 0 && elementsFound < pathBytes.Length){
                byte b = pathBytes[elementsFound];

                if(!currentNode.Children.TryGetValue(b, out RadixNode<TValue> nextNode))
                    return false;

                // Verify full label match
                for(int i = 0; i < nextNode.Label.Length; i++){
                    if(elementsFound + i >= pathBytes.Length || nextNode.Label[i] != pathBytes[elementsFound + i])
                        return false;
                }

                ancestors.Add((currentNode, b));
                elementsFound += nextNode.Label.Length;
                currentNode = nextNode;
            }

            // Validate target
            if(elementsFound != pathBytes.Length || !currentNode.HasValue)
                return false;

            // Clear the value
            currentNode.Value = default;

            // Cleanup, merge if node has exactly one child
            if(currentNode.Children.Count == 1 && currentNode != _root){
                var only = currentNode.Children.First();
                RadixNode<TValue> onlyChild = only.Value;
                byte[] merged = new byte[currentNode.Label.Length + onlyChild.Label.Length];
                currentNode.Label.CopyTo(merged, 0);
                onlyChild.Label.CopyTo(merged, currentNode.Label.Length);
                currentNode.Label = merged;
                currentNode.Value = onlyChild.Value;
                currentNode.Children = onlyChild.Children;
                return true;
            }

            // Cleanup, remove dead leaf and check parent
            if(currentNode.Children.Count == 0 && ancestors.Count > 0){
                var (parent, parentKey) = ancestors[^1];
                parent.Children.Remove(parentKey);

                // Check if parent needs merging
                if(!parent.HasValue && parent.Children.Count == 1 && parent != _root){
                    var only = parent.Children.First();
                    RadixNode<TValue> onlyChild = only.Value;
                    byte[] merged = new byte[parent.Label.Length + onlyChild.Label.Length];
                    parent.Label.CopyTo(merged, 0);
                    onlyChild.Label.CopyTo(merged, parent.Label.Length);
                    parent.Label = merged;
                    parent.Value = onlyChild.Value;
                    parent.Children = onlyChild.Children;
                }
            }

            return true;
        }
    }

    public class RadixNode<TValue>(byte[] label)
    {
        public byte[] Label = label;
        public Dictionary<byte, RadixNode<TValue>> Children = new();
        public TValue? Value;
        public bool HasValue => Value is not null;
    }
    public class DirectoryEntry(string path, List<string> children, long sizeBytes, bool isFolder)
    {
        public string Path => path;
        public List<string> Children => children;
        public long SizeBytes => sizeBytes; 
        public bool IsFolder => isFolder;
    }
}
