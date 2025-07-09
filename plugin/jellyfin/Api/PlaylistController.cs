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
    [Route("api/jellysentia/playlists")]
    public class PlaylistController : ControllerBase
    {
        private readonly PlaylistService _playlistService;
        private readonly IUserManager _userManager;
        private readonly ILogger<PlaylistController> _logger;

        public PlaylistController(
            PlaylistService playlistService,
            IUserManager userManager,
            ILogger<PlaylistController> logger)
        {
            _playlistService = playlistService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("smart")]
        public async Task<IActionResult> CreateSmartPlaylist(
            [FromBody][Required] SmartPlaylistRequest request)
        {
            try
            {
                var userId = _userManager.GetUserById(User.GetUserId());
                if (userId == null)
                {
                    return Unauthorized();
                }

                var playlist = await _playlistService.CreateSmartPlaylist(request, userId.Id);
                
                return Ok(new
                {
                    id = playlist.Id,
                    name = playlist.Name,
                    itemCount = playlist.GetChildren().Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating smart playlist");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("smart/preview")]
        public async Task<IActionResult> PreviewSmartPlaylist(
            [FromQuery] float? minTempo,
            [FromQuery] float? maxTempo,
            [FromQuery] float? minEnergy,
            [FromQuery] float? maxEnergy,
            [FromQuery] string? key,
            [FromQuery] int limit = 20)
        {
            try
            {
                var request = new SmartPlaylistRequest
                {
                    MinTempo = minTempo,
                    MaxTempo = maxTempo,
                    MinEnergy = minEnergy,
                    MaxEnergy = maxEnergy,
                    Key = key,
                    Limit = limit
                };

                // Preview without creating playlist
                // TODO: Implement preview logic
                
                return Ok(new { tracks = new[] { "preview not implemented" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing smart playlist");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}