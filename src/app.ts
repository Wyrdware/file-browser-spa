const response = await fetch("api/files/directory?path=/test/path");
const listing = await response.json();
console.log(listing);


const app = document.querySelector<HTMLDivElement>("#app");

if (app === null) {
  throw new Error("Missing #app.");
}


//Contruct------------------------
app.innerHTML = `
  <main>
    <h1>File Brower Test Project</h1>
    <button id="open-file-browser-button" type="button">
      Open File Browser
    </button>

    <dialog id="file-browser-dialog" style="width: 80vw; max-height: 80vh;">
      <h2>File Browser</h2>

      <div>
        <button type="button">Up</button>
        <form id = "search-form" style="display: inline;">
          <input id = "search-input" type="search"/>
          <button type="submit">Search</button>
        </form>
      </div>

      <p>
         Upload file:
      </p>
      <input type="file"> 
      
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

          <tbody id = "folder-contents-body">
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


//Query elements--------------------
const openButton = document.querySelector<HTMLButtonElement>("#open-file-browser-button");
const closeButton = document.querySelector<HTMLButtonElement>("#close-file-browser-button");
const dialog = document.querySelector<HTMLDialogElement>("#file-browser-dialog");
const searchForm = app.querySelector<HTMLFormElement>("#search-form");
const searchInput = app.querySelector<HTMLInputElement>("#search-input");
const folderInfoLabel = app.querySelector<HTMLSpanElement>("#folder-info-label");
const folderContentsBody = app.querySelector<HTMLTableSectionElement>("#folder-contents-body");

if (openButton === null) {
  throw new Error("Missing #open-file-browser-button.");
}

if (closeButton === null) {
  throw new Error("Missing #close-file-browser-button.");
}

if (dialog === null) {
  throw new Error("Missing #file-browser-dialog.");
}

if (searchForm === null) {
  throw new Error("Missing #search-form.");
}

if (searchInput === null) {
  throw new Error("missing #search-input.");
}

if (folderInfoLabel === null) {
  throw new Error("missing #folder-info-label");
}

if (folderContentsBody === null) {
  throw new Error("missing #folder-contents-body");
}

//Testing------------------------------
searchInput.placeholder = "Path: /Test/Path";

type FolderEntry = {
  name: string;
  relativePath: string;
};

type FileEntry = {
  name: string;
  relativePath: string;
  sizeBytes: number;
};

const testFolders: FolderEntry[] = [
  {
    name: "Documents",
    relativePath: "Documents"
  },
  {
    name: "Images",
    relativePath: "Images"
  },
  {
    name: "Projects",
    relativePath: "Projects"
  }
];

const testFiles: FileEntry[] = [
  {
    name: "notes.txt",
    relativePath: "notes.txt",
    sizeBytes: 4096
  },
  {
    name: "resume.pdf",
    relativePath: "resume.pdf",
    sizeBytes: 248_000
  },
  {
    name: "photo.png",
    relativePath: "photo.png",
    sizeBytes: 1_542_000
  }
];
const folderRowsHtml = testFolders
  .map(folder => `
    <tr>
      <td>${escapeHtml(folder.name)}</td>
      <td>Folder</td>
      <td></td>
      <td>
        <button class="open-folder-button" data-path="${escapeHtml(folder.relativePath)}" type="button">
          Open
        </button>
      </td>
    </tr>
  `)
  .join("");

const fileRowsHtml = testFiles
  .map(file => `
    <tr>
      <td>${escapeHtml(file.name)}</td>
      <td>File</td>
      <td>${formatBytes(file.sizeBytes)}</td>
      <td>
        <a href="/api/files/download?path=${encodeURIComponent(file.relativePath)}">
          Download
        </a>
      </td>
    </tr>
  `)
  .join("");

folderContentsBody.innerHTML = folderRowsHtml + fileRowsHtml;



//Attatch Events-----------------------
openButton.addEventListener("click", () => {
  dialog.showModal();
});

closeButton.addEventListener("click", () => {
  dialog.close();
});

searchForm.addEventListener("submit", event => {
  event.preventDefault();

  // Read selected file.
  // Send FormData to server.
  // Reload current folder.
});



//Functions
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


