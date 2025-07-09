using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using JellySentia.Services;

namespace JellySentia.Api
{
    [ApiController]
    [Authorize(Policy = "DefaultAuthorization")]
    [Route("api/jellysentia/analyze")]
    public class AnalysisController : ControllerBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly AnalysisService _analysisService;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            ILibraryManager libraryManager,
            AnalysisService analysisService,
            ILogger<AnalysisController> logger)
        {
            _libraryManager = libraryManager;
            _analysisService = analysisService;
            _logger = logger;
        }

        [HttpGet("track/{id}")]
        public async Task<IActionResult> AnalyzeTrack([Required] Guid id)
        {
            try
            {
                var item = _libraryManager.GetItemById(id);
                if (item == null)
                {
                    return NotFound($"Item {id} not found");
                }

                // Trigger analysis for single track
                await Task.Run(() => _analysisService.Execute(
                    HttpContext.RequestAborted,
                    new Progress<double>()
                ));

                return Ok(new { message = "Analysis started", itemId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing track {id}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("library")]
        public async Task<IActionResult> AnalyzeLibrary()
        {
            try
            {
                // Start library analysis in background
                _ = Task.Run(() => _analysisService.Execute(
                    HttpContext.RequestAborted,
                    new Progress<double>()
                ));

                return Ok(new { message = "Library analysis started" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting library analysis");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetAnalysisStatus()
        {
            // TODO: Implement analysis status tracking
            return Ok(new
            {
                isRunning = false,
                progress = 0,
                analyzed = 0,
                total = 0,
                errors = 0
            });
        }
    }
}