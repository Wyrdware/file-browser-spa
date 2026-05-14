# File Browser

A single-page web application for browsing and managing files on a server. ASP.NET Core (.NET 8) backend, vanilla TypeScript frontend — no JS frameworks or bundler.

> No authentication — intended for local or trusted-network use only. All paths are sandboxed to the configured home directory.

## Features

- Browse files and folders with recursive name search
- Upload, download, delete, move, and copy files and folders
- Deep-linkable URLs via URL hash — browser back/forward works, links are shareable
- File browser lives in a native HTML `<dialog>` element
- Batched rendering with `requestAnimationFrame` for smooth navigation of large directories

## Configuration

Set the root directory in `appsettings.json`:

```json
{
  "FileBrowser": {
    "HomeDirectory": "C:\\FileBrowserHome"
  }
}
```

For local development, override in `appsettings.Development.json`.

## Building and Running

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download) and [Node.js](https://nodejs.org/).

```bash
git clone <repo-url>
npm install
npm run build
dotnet run
```

The app will be available at `https://localhost:7146` (or `http://localhost:5120`). Use `npm run watch` during development to recompile TypeScript on save.