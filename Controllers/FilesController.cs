using Microsoft.AspNetCore.Mvc;
using TestProject.FileBrowser;

namespace TestProject.Controllers {
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase {

        private readonly BrowseSystem _browseSystem;
        private readonly ILogger<FilesController> _logger;

        public FilesController(BrowseSystem browseSystem, ILogger<FilesController> logger) {
            _browseSystem = browseSystem;
            _logger = logger;
        }

        [HttpGet("directory")]
        public ActionResult<DirectoryListing> GetDirectory([FromQuery] string path = "") {
            try {
                return _browseSystem.GetDirectoryListing(path);
            }
            catch (DirectoryNotFoundException) {
                return NotFound(new FileOperationResult("Directory not found."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult("Invalid path."));
            }
        }

        [HttpGet("search")]
        public ActionResult<SearchResults> Search([FromQuery] string query, [FromQuery] string? path = null) {
            try {
                return _browseSystem.Search(query, path);
            }
            catch (DirectoryNotFoundException) {
                return NotFound(new FileOperationResult("Directory not found."));
            }
        }

        [HttpGet("download")]
        public IActionResult Download([FromQuery] string path) {
            try {
                FileStream stream = _browseSystem.GetFileStream(path);
                string fileName = Path.GetFileName(path);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult("File not found."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult("Invalid path."));
            }
        }

        [HttpPost("upload")]
        public ActionResult<FileOperationResult> Upload([FromQuery] string path = "", [FromForm] IFormFile? file = null) {
            if (file is null) {
                return BadRequest(new FileOperationResult("No file provided."));
            }
            try {
                using Stream stream = file.OpenReadStream();
                _browseSystem.Upload(path, stream, file.FileName);
                string displayPath = string.IsNullOrEmpty(path) ? "/" : $"/{path}";
                return Ok(new FileOperationResult($"Uploaded '{file.FileName}' to {displayPath}."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult("Invalid path."));
            }
        }

        [HttpDelete]
        public ActionResult<FileOperationResult> Delete([FromQuery] string path) {
            try {
                _browseSystem.Delete(path);
                return Ok(new FileOperationResult($"Deleted '{path}'."));
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult("Path not found."));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult("Invalid path."));
            }
        }

        [HttpPost("move")]
        public ActionResult<FileOperationResult> Move([FromBody] MoveRequest request) {
            try {
                _browseSystem.Move(request.Source, request.Dest);
                return Ok(new FileOperationResult($"Moved '{request.Source}' to '{request.Dest}'."));
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult("Source not found."));
            }
            catch (IOException exception) {
                return Conflict(new FileOperationResult(exception.Message));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult("Invalid path."));
            }
        }

        [HttpPost("copy")]
        public ActionResult<FileOperationResult> Copy([FromBody] CopyRequest request) {
            try {
                _browseSystem.Copy(request.Source, request.Dest);
                return Ok(new FileOperationResult($"Copied '{request.Source}' to '{request.Dest}'."));
            }
            catch (FileNotFoundException) {
                return NotFound(new FileOperationResult("Source not found."));
            }
            catch (IOException exception) {
                return Conflict(new FileOperationResult(exception.Message));
            }
            catch (UnauthorizedAccessException) {
                return BadRequest(new FileOperationResult("Invalid path."));
            }
        }
    }
}
