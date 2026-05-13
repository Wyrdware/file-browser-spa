using Microsoft.AspNetCore.Mvc;
using TestProject.FileBrowser;

namespace TestProject.Controllers {
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase {

        private readonly ILogger<FilesController> _logger;

        public FilesController(ILogger<FilesController> logger) {
            _logger = logger;
        }

        [HttpGet("directory")]
        public ActionResult<DirectoryListing> GetDirectory(string path){
            //The following is placeholder, reference FileSystem.cs
            DirectoryListing listing = new(
                Path: "/Test/Listing/",
                Folders: [
                    new FolderEntry("Documents"),
                    new FolderEntry("Images")
                ],
                Files: [
                    new FileEntry("notes.txt", 4096)
               ]
            );
            return listing;
        }
        [HttpGet("search")]
        public ActionResult<SearchResult> Search(string query){

            //The following is placeholder, reference FileSystem.cs 
            return null;
        }

    }
}
