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
    [Route("api/jellysentia/similarity")]
    public class SimilarityController : ControllerBase
    {
        private readonly SimilarityService _similarityService;
        private readonly ILogger<SimilarityController> _logger;

        public SimilarityController(
            SimilarityService similarityService,
            ILogger<SimilarityController> logger)
        {
            _similarityService = similarityService;
            _logger = logger;
        }

        [HttpGet("track/{id}")]
        public async Task<IActionResult> GetSimilarTracks(
            [Required] Guid id,
            [FromQuery] int count = 20)
        {
            try
            {
                var similarTracks = await _similarityService.GetSimilarTracks(id, count);
                
                var response = similarTracks.Select(t => new
                {
                    id = t.Item.Id,
                    name = t.Item.Name,
                    artist = t.Item.GetArtists()?.FirstOrDefault(),
                    album = t.Item.Album,
                    similarity = t.Similarity,
                    distance = t.Distance
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting similar tracks for {id}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("index/build")]
        public async Task<IActionResult> BuildSimilarityIndex()
        {
            try
            {
                await _similarityService.BuildSimilarityIndex();
                return Ok(new { message = "Similarity index built successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building similarity index");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}