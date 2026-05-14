import { createFileBrowser } from "./fileBrowser.js";

function requireSelector<T extends Element>(selector: string, root: ParentNode = document): T {
  const element = root.querySelector<T>(selector);
  if (element === null) {
    throw new Error(`Missing ${selector}.`);
  }
  return element;
}


//Construct----------------------------
const app = requireSelector<HTMLDivElement>("#app");

app.innerHTML = `
  <main>
    <h1>File Browser Test Project</h1>
    <button id="open-file-browser-button" type="button">
      Open File Browser
    </button>
  </main>
`;

const main = requireSelector<HTMLElement>("main", app);
const openButton = requireSelector<HTMLButtonElement>("#open-file-browser-button", app);


//Setup file browser-------------------
const fileBrowser = createFileBrowser();
main.appendChild(fileBrowser.element);

openButton.addEventListener("click", () => {
  fileBrowser.open();
});

if (location.hash.length > 0) {
  fileBrowser.open();
}
