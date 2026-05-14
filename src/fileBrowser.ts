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

type FileOperationResult = {
  message: string;
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

function getPathFromHash(): string {
  const hash = location.hash.replace(/^#\/?/, "");
  return decodeURIComponent(hash);
}

function joinPath(base: string, name: string): string {
  if (base === "") return name;
  return base + "/" + name;
}


//Export-------------------------------
export function createFileBrowser(): { element: HTMLDialogElement; open(): void } {

  //Construct--------------------------
  const dialog = document.createElement("dialog");
  dialog.style.width = "80vw";
  dialog.style.maxHeight = "80vh";
  dialog.innerHTML = `
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
      <span id="operation-result-label"></span>
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
  `;

  //Query elements---------------------
  const closeButton = requireSelector<HTMLButtonElement>("#close-file-browser-button", dialog);
  const upButton = requireSelector<HTMLButtonElement>("#up-button", dialog);
  const searchForm = requireSelector<HTMLFormElement>("#search-form", dialog);
  const searchInput = requireSelector<HTMLInputElement>("#search-input", dialog);
  const uploadInput = requireSelector<HTMLInputElement>("#upload-input", dialog);
  const operationResultLabel = requireSelector<HTMLSpanElement>("#operation-result-label", dialog);
  const folderInfoLabel = requireSelector<HTMLSpanElement>("#folder-info-label", dialog);
  const folderContentsBody = requireSelector<HTMLTableSectionElement>("#folder-contents-body", dialog);


  //Attach Events----------------------
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

    const message = await uploadFile(file);
    operationResultLabel.textContent = message;
    uploadInput.value = "";
    loadDirectory(getPathFromHash());
  });

  window.addEventListener("hashchange", () => {
    if (!dialog.open) return;
    loadDirectory(getPathFromHash());
  });

  dialog.addEventListener("click", async (event) => {
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
        const message = await deleteItem(path);
        operationResultLabel.textContent = message;
        loadDirectory(getPathFromHash());
      }
    }
    else if (action === "move") {
      const dest = prompt("Move to (relative path):", path);
      if (dest !== null && dest !== path) {
        const message = await moveItem(path, dest);
        operationResultLabel.textContent = message;
        loadDirectory(getPathFromHash());
      }
    }
    else if (action === "copy") {
      const dest = prompt("Copy to (relative path):", path);
      if (dest !== null && dest !== path) {
        const message = await copyItem(path, dest);
        operationResultLabel.textContent = message;
        loadDirectory(getPathFromHash());
      }
    }
  });


  //API--------------------------------
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

  async function uploadFile(file: File): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch(`api/files/upload?path=${encodeURIComponent(getPathFromHash())}`, {
      method: "POST",
      body: formData,
    });

    const result: FileOperationResult = await response.json();
    return result.message;
  }

  async function deleteItem(path: string): Promise<string> {
    const response = await fetch(`api/files?path=${encodeURIComponent(path)}`, {
      method: "DELETE",
    });
    const result: FileOperationResult = await response.json();
    return result.message;
  }

  async function moveItem(source: string, dest: string): Promise<string> {
    const response = await fetch("api/files/move", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ source, dest }),
    });
    const result: FileOperationResult = await response.json();
    return result.message;
  }

  async function copyItem(source: string, dest: string): Promise<string> {
    const response = await fetch("api/files/copy", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ source, dest }),
    });
    const result: FileOperationResult = await response.json();
    return result.message;
  }


  //Render-----------------------------
  let currentRenderToken = 0;

  function makeButton(text: string, action: string, path: string): HTMLButtonElement {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.textContent = text;
    btn.dataset["action"] = action;
    btn.dataset["path"] = path;
    return btn;
  }

  function buildFolderRow(name: string, path: string): HTMLTableRowElement {
    const row = document.createElement("tr");
    const nameCell = document.createElement("td");
    nameCell.textContent = name;
    const typeCell = document.createElement("td");
    typeCell.textContent = "Folder";
    const sizeCell = document.createElement("td");
    const actionsCell = document.createElement("td");
    actionsCell.append(
      makeButton("Open", "open", path),
      makeButton("Delete", "delete", path),
      makeButton("Move", "move", path),
      makeButton("Copy", "copy", path),
    );
    row.append(nameCell, typeCell, sizeCell, actionsCell);
    return row;
  }

  function buildFileRow(name: string, sizeBytes: number, path: string): HTMLTableRowElement {
    const row = document.createElement("tr");
    const nameCell = document.createElement("td");
    nameCell.textContent = name;
    const typeCell = document.createElement("td");
    typeCell.textContent = "File";
    const sizeCell = document.createElement("td");
    sizeCell.textContent = formatBytes(sizeBytes);
    const actionsCell = document.createElement("td");
    const downloadLink = document.createElement("a");
    downloadLink.href = `/api/files/download?path=${encodeURIComponent(path)}`;
    downloadLink.textContent = "Download";
    actionsCell.append(
      downloadLink,
      makeButton("Delete", "delete", path),
      makeButton("Move", "move", path),
      makeButton("Copy", "copy", path),
    );
    row.append(nameCell, typeCell, sizeCell, actionsCell);
    return row;
  }

  function buildSearchRow(entry: SearchResultEntry): HTMLTableRowElement {
    const row = document.createElement("tr");
    const nameCell = document.createElement("td");
    nameCell.textContent = entry.name;
    const typeCell = document.createElement("td");
    typeCell.textContent = entry.isFolder ? "Folder" : "File";
    const pathCell = document.createElement("td");
    pathCell.textContent = entry.path;
    const actionsCell = document.createElement("td");
    if (entry.isFolder) {
      actionsCell.appendChild(makeButton("Open", "open", entry.path));
    } else {
      const downloadLink = document.createElement("a");
      downloadLink.href = `/api/files/download?path=${encodeURIComponent(entry.path)}`;
      downloadLink.textContent = "Download";
      actionsCell.appendChild(downloadLink);
    }
    row.append(nameCell, typeCell, pathCell, actionsCell);
    return row;
  }

  async function renderBatched(rows: HTMLTableRowElement[]): Promise<void> {
    folderContentsBody.innerHTML = "";
    const token = ++currentRenderToken;
    let index = 0;

    while (index < rows.length) {
      if (token !== currentRenderToken) return;
      const end = Math.min(index + 100, rows.length);
      for (let i = index; i < end; i++) {
        folderContentsBody.appendChild(rows[i]!);
      }
      index = end;
      await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
    }
  }

  function render(listing: DirectoryListing): void {
    const folderCount = listing.folders.length;
    const fileCount = listing.files.length;
    const totalSize = listing.files.reduce((sum, file) => sum + file.sizeBytes, 0);
    folderInfoLabel.textContent = `${folderCount} folders, ${fileCount} files \u2014 ${formatBytes(totalSize)}`;
    searchInput.placeholder = listing.path;
    searchInput.value = "";

    const currentPath = getPathFromHash();
    const rows: HTMLTableRowElement[] = [
      ...listing.folders.map(folder => buildFolderRow(folder.name, joinPath(currentPath, folder.name))),
      ...listing.files.map(file => buildFileRow(file.name, file.sizeBytes, joinPath(currentPath, file.name))),
    ];

    renderBatched(rows);
  }

  function renderSearchResults(results: SearchResults): void {
    folderInfoLabel.textContent = `Search: "${results.query}" \u2014 ${results.entries.length} results`;
    renderBatched(results.entries.map(buildSearchRow));
  }


  //Return------------------------------
  function open(): void {
    dialog.showModal();
    loadDirectory(getPathFromHash());
  }

  return { element: dialog, open };
}
