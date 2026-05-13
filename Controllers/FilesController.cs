using Microsoft.AspNetCore.Mvc;
using TestProject.FileBrowser;

namespace TestProject.Controllers {
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase {

        private readonly FileSystem _fileSystem;

        public FilesController(FileSystem fileSystem) {
            _fileSystem = fileSystem;
        }

        [HttpGet("directory")]
        public ActionResult<DirectoryListing> GetDirectory([FromQuery] string path = "") {
            try {
                return _fileSystem.GetDirectoryListing(path);
            }
            catch (DirectoryNotFoundException) {
                return NotFound(new FileOperationResult(false, "Directory not found."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult(false, "Invalid path."));
            }
        }

        [HttpGet("search")]
        public ActionResult<SearchResults> Search([FromQuery] string query, [FromQuery] string? path = null) {
            try {
                return _fileSystem.Search(query, path);
            }
            catch (DirectoryNotFoundException) {
                return NotFound(new FileOperationResult(false, "Directory not found."));
            }
        }

        [HttpGet("download")]
        public IActionResult Download([FromQuery] string path) {
            try {
                FileStream stream = _fileSystem.GetFileStream(path);
                string fileName = Path.GetFileName(path);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult(false, "File not found."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult(false, "Invalid path."));
            }
        }

        [HttpPost("upload")]
        public ActionResult<FileOperationResult> Upload([FromQuery] string path = "", [FromForm] IFormFile? file = null) {
            if (file is null) {
                return BadRequest(new FileOperationResult(false, "No file provided."));
            }
            try {
                using Stream stream = file.OpenReadStream();
                _fileSystem.Upload(path, stream, file.FileName);
                return Ok(new FileOperationResult(true, "File uploaded."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult(false, "Invalid path."));
            }
        }

        [HttpDelete]
        public ActionResult<FileOperationResult> Delete([FromQuery] string path) {
            try {
                _fileSystem.Delete(path);
                return Ok(new FileOperationResult(true, "Deleted."));
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult(false, "Path not found."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult(false, "Invalid path."));
            }
        }

        [HttpPost("move")]
        public ActionResult<FileOperationResult> Move([FromBody] MoveRequest request) {
            try {
                _fileSystem.Move(request.Source, request.Dest);
                return Ok(new FileOperationResult(true, "Moved."));
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult(false, "Source not found."));
            }
            catch (IOException exception) {
                return Conflict(new FileOperationResult(false, exception.Message));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult(false, "Invalid path."));
            }
        }

        [HttpPost("copy")]
        public ActionResult<FileOperationResult> Copy([FromBody] CopyRequest request) {
            try {
                _fileSystem.Copy(request.Source, request.Dest);
                return Ok(new FileOperationResult(true, "Copied."));
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult(false, "Source not found."));
            }
            catch (IOException exception) {
                return Conflict(new FileOperationResult(false, exception.Message));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult(false, "Invalid path."));
            }
        }
    }
}
