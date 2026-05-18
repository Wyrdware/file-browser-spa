# File Browser

A single-page web application for browsing and managing files on a server. ASP.NET Core (.NET 8) backend, vanilla TypeScript frontend.

## Features

- Browse files and folders
- Upload, download, delete, move, and copy files/folders
- Deep-linkable folder state
- File browser built inside a `<dialog>` element
- Batched rendering with `requestAnimationFrame` for larger directories

## Performance Notes
The backend uses a Radix Tree for the path, and a second one containing only the entry names for the search. Browsing and searching use this structure instead of reading from disk every time.

Batching entry rendering helped with the initial page load, but large directories still eventually render every table element. This can create a notable slowdown when leaving the page, likely from the cleanup/garbage collection cost of many elements at once.

A future improvement could be range-based directory loading paired with virtualized table rendering, as well as utilizing Adaptive Radix Trees.

## Configuration

Set the root directory in `appsettings.json`:

```json
{
  "FileBrowser": {
    "HomeDirectory": "C:\\FileBrowserHome"
  }
}
```

## Building and Running

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download) and [Node.js](https://nodejs.org/).

From the project root:

```bash
npm install
npm run build
dotnet run
```

Use `npm run watch` during development to recompile TypeScript on save.
