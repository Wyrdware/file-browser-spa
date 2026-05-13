//Types--------------------------------
type FolderEntry = {
  name: string;
};

type FileEntry = {
  name: string;
  sizeBytes: number;
};

type DirectoryListing = {
  path: string;
  folders: FolderEntry[];
  files: FileEntry[];
};

type SearchResultEntry = {
  name: string;
  path: string;
  isFolder: boolean;
};

type SearchResults = {
  query: string;
  entries: SearchResultEntry[];
};


//Functions----------------------------
function requireSelector<T extends Element>(selector: string, root: ParentNode = document): T {
  const element = root.querySelector<T>(selector);
  if (element === null) {
    throw new Error(`Missing ${selector}.`);
  }
  return element;
}

function formatBytes(bytes: number): string {
  if (bytes === 0) {
    return "0 B";
  }

  const units = ["B", "KB", "MB", "GB"];
  const unitIndex = Math.min(
    Math.floor(Math.log(bytes) / Math.log(1024)),
    units.length - 1
  );

  const value = bytes / Math.pow(1024, unitIndex);

  return `${value.toFixed(1)} ${units[unitIndex]}`;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#039;");
}

function getPathFromHash(): string {
  const hash = location.hash.replace(/^#\/?/, "");
  return decodeURIComponent(hash);
}

function joinPath(base: string, name: string): string {
  if (base === "") return name;
  return base.replace(/\/$/, "") + "/" + name;
}


//Construct----------------------------
const app = requireSelector<HTMLDivElement>("#app");

app.innerHTML = `
  <main>
    <h1>File Browser Test Project</h1>
    <button id="open-file-browser-button" type="button">
      Open File Browser
    </button>

    <dialog id="file-browser-dialog" style="width: 80vw; max-height: 80vh;">
      <h2>File Browser</h2>

      <div>
        <button id="up-button" type="button">Up</button>
        <form id="search-form" style="display: inline;">
          <input id="search-input" type="search"/>
          <button type="submit">Search</button>
        </form>
      </div>

      <p>
        Upload file:
        <input id="upload-input" type="file">
      </p>

      <p><span id="folder-info-label"></span></p>

      <div style="max-height: 50vh; overflow: auto;">
        <table style="width: 100%;">
          <thead>
            <tr>
              <th align="left">Name</th>
              <th align="left">Type</th>
              <th align="left">Size</th>
              <th align="left">Actions</th>
            </tr>
          </thead>

          <tbody id="folder-contents-body">
          </tbody>
        </table>
      </div>

      <p>
        <button id="close-file-browser-button" type="button">
          Close
        </button>
      </p>
    </dialog>
  </main>
`;


//Query elements-----------------------
const openButton = requireSelector<HTMLButtonElement>("#open-file-browser-button");
const closeButton = requireSelector<HTMLButtonElement>("#close-file-browser-button");
const dialog = requireSelector<HTMLDialogElement>("#file-browser-dialog");
const upButton = requireSelector<HTMLButtonElement>("#up-button", app);
const searchForm = requireSelector<HTMLFormElement>("#search-form", app);
const searchInput = requireSelector<HTMLInputElement>("#search-input", app);
const uploadInput = requireSelector<HTMLInputElement>("#upload-input", app);
const folderInfoLabel = requireSelector<HTMLSpanElement>("#folder-info-label", app);
const folderContentsBody = requireSelector<HTMLTableSectionElement>("#folder-contents-body", app);


//Attach Events------------------------
openButton.addEventListener("click", () => {
  dialog.showModal();
  loadDirectory(getPathFromHash());
});

// Deep link: auto-open dialog if URL has a path hash on page load.
if (location.hash.length > 0) {
  dialog.showModal();
  loadDirectory(getPathFromHash());
}

closeButton.addEventListener("click", () => {
  dialog.close();
});

upButton.addEventListener("click", () => {
  const parentPath = getPathFromHash().replace(/\/?[^\/]+\/?$/, "");
  location.hash = parentPath;
});

searchForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const query = searchInput.value.trim();
  if (query === "") {
    loadDirectory(getPathFromHash());
    return;
  }
  await searchFiles(query);
});

uploadInput.addEventListener("change", async () => {
  const file = uploadInput.files?.[0];
  if (file === undefined) return;

  await uploadFile(file);
  uploadInput.value = "";
  loadDirectory(getPathFromHash());
});

window.addEventListener("hashchange", () => {
  loadDirectory(getPathFromHash());
});

folderContentsBody.addEventListener("click", async (event) => {
  const target = event.target;
  if (!(target instanceof HTMLButtonElement)) return;

  const action = target.dataset["action"];
  const path = target.dataset["path"];
  if (action === undefined || path === undefined) return;

  if (action === "open") {
    location.hash = path;
  }
  else if (action === "delete") {
    if (confirm(`Delete "${path}"?`)) {
      await deleteItem(path);
      loadDirectory(getPathFromHash());
    }
  }
  else if (action === "move") {
    const dest = prompt("Move to (relative path):", path);
    if (dest !== null && dest !== path) {
      await moveItem(path, dest);
      loadDirectory(getPathFromHash());
    }
  }
  else if (action === "copy") {
    const dest = prompt("Copy to (relative path):", path);
    if (dest !== null && dest !== path) {
      await copyItem(path, dest);
      loadDirectory(getPathFromHash());
    }
  }
});


//API----------------------------------
async function loadDirectory(path: string): Promise<void> {
  const response = await fetch(`api/files/directory?path=${encodeURIComponent(path)}`);
  if (!response.ok) {
    folderContentsBody.innerHTML = `<tr><td colspan="4">Error loading directory.</td></tr>`;
    folderInfoLabel.textContent = "";
    return;
  }

  const listing: DirectoryListing = await response.json();
  render(listing);
}

async function searchFiles(query: string): Promise<void> {
  const response = await fetch(
    `api/files/search?query=${encodeURIComponent(query)}&path=${encodeURIComponent(getPathFromHash())}`
  );
  if (!response.ok) {
    folderContentsBody.innerHTML = `<tr><td colspan="4">Search failed.</td></tr>`;
    return;
  }

  const results: SearchResults = await response.json();
  renderSearchResults(results);
}

async function uploadFile(file: File): Promise<void> {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(`api/files/upload?path=${encodeURIComponent(getPathFromHash())}`, {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    alert(`Upload failed: ${response.statusText}`);
  }
}

async function deleteItem(path: string): Promise<void> {
  await fetch(`api/files?path=${encodeURIComponent(path)}`, {
    method: "DELETE",
  });
}

async function moveItem(source: string, dest: string): Promise<void> {
  await fetch("api/files/move", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ source, dest }),
  });
}

async function copyItem(source: string, dest: string): Promise<void> {
  await fetch("api/files/copy", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ source, dest }),
  });
}


//Render-------------------------------
function render(listing: DirectoryListing): void {
  const folderCount = listing.folders.length;
  const fileCount = listing.files.length;
  const totalSize = listing.files.reduce((sum, file) => sum + file.sizeBytes, 0);
  folderInfoLabel.textContent = `${folderCount} folders, ${fileCount} files \u2014 ${formatBytes(totalSize)}`;
  searchInput.placeholder = listing.path;
  searchInput.value = "";

  const folderRowsHtml = listing.folders
    .map(folder => {
      const folderPath = joinPath(getPathFromHash(), folder.name);
      return `
        <tr>
          <td>${escapeHtml(folder.name)}</td>
          <td>Folder</td>
          <td></td>
          <td>
            <button data-action="open" data-path="${escapeHtml(folderPath)}" type="button">Open</button>
            <button data-action="delete" data-path="${escapeHtml(folderPath)}" type="button">Delete</button>
            <button data-action="move" data-path="${escapeHtml(folderPath)}" type="button">Move</button>
            <button data-action="copy" data-path="${escapeHtml(folderPath)}" type="button">Copy</button>
          </td>
        </tr>`;
    })
    .join("");

  const fileRowsHtml = listing.files
    .map(file => {
      const filePath = joinPath(getPathFromHash(), file.name);
      return `
        <tr>
          <td>${escapeHtml(file.name)}</td>
          <td>File</td>
          <td>${formatBytes(file.sizeBytes)}</td>
          <td>
            <a href="/api/files/download?path=${encodeURIComponent(filePath)}">Download</a>
            <button data-action="delete" data-path="${escapeHtml(filePath)}" type="button">Delete</button>
            <button data-action="move" data-path="${escapeHtml(filePath)}" type="button">Move</button>
            <button data-action="copy" data-path="${escapeHtml(filePath)}" type="button">Copy</button>
          </td>
        </tr>`;
    })
    .join("");

  folderContentsBody.innerHTML = folderRowsHtml + fileRowsHtml;
}

function renderSearchResults(results: SearchResults): void {
  folderInfoLabel.textContent = `Search: "${results.query}" \u2014 ${results.entries.length} results`;

  const rowsHtml = results.entries
    .map(entry => {
      const typeLabel = entry.isFolder ? "Folder" : "File";
      const actionHtml = entry.isFolder
        ? `<button data-action="open" data-path="${escapeHtml(entry.path)}" type="button">Open</button>`
        : `<a href="/api/files/download?path=${encodeURIComponent(entry.path)}">Download</a>`;

      return `
        <tr>
          <td>${escapeHtml(entry.name)}</td>
          <td>${typeLabel}</td>
          <td>${escapeHtml(entry.path)}</td>
          <td>${actionHtml}</td>
        </tr>`;
    })
    .join("");

  folderContentsBody.innerHTML = rowsHtml;
}




