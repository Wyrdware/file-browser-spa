# File Browser

A single-page web application for browsing and managing files on a server. ASP.NET Core (.NET 8) backend, vanilla TypeScript frontend.

## Features

- Browse files and folders
- Upload, download, delete, move, and copy files/folders
- Deep-linkable folder state
- File browser built inside a `<dialog>` element
- Batched rendering with `requestAnimationFrame` for larger directories

## Performance Notes

I tested with a folder containing 10,000 files and profiled the page during use. The current implementation works, and batched rendering helped with the initial page load, but large directories still eventually render every table element. This creates a notable slowdown when leaving the page, likely from the cleanup/garbage collection cost of many elements at once.

A future improvement would be range-based directory loading paired with virtualized table rendering.

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
