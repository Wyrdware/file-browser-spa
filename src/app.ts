//const response = await fetch('/test');
//const data = await response.text();

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

      <p>Path: /</p>

      <p>
        <button type="button">Up</button>
        <input type="search" placeholder="Search files and folders" />
        <input type="file" />
      </p>

      <p>0 folders | 0 files | 0 B</p>

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

          <tbody>
            <tr>
              <td>Documents</td>
              <td>Folder</td>
              <td></td>
              <td><button type="button">Open</button></td>
            </tr>

            <tr>
              <td>notes.txt</td>
              <td>File</td>
              <td>4 KB</td>
              <td><button type="button">Download</button></td>
            </tr>
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

if (openButton === null) {
  throw new Error("Missing #open-file-browser-button.");
}

if (closeButton === null) {
  throw new Error("Missing #close-file-browser-button.");
}

if (dialog === null) {
  throw new Error("Missing #file-browser-dialog.");
}


//Attatch Events-----------------------
openButton.addEventListener("click", () => {
  dialog.showModal();
});

closeButton.addEventListener("click", () => {
  dialog.close();
});
