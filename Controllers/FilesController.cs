using Microsoft.AspNetCore.Mvc;

namespace TestProject.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase {

        private readonly ILogger<FilesController> _logger;

        public FilesController(ILogger<FilesController> logger) {
            _logger = logger;
        }

        [HttpGet]
        public string Get() {
            return "API Response";
        }

        

        
    }
}
