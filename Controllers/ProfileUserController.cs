using Microsoft.AspNetCore.Mvc;
using HUIT_Library.DTOs;
using HUIT_Library.DTOs.Request;
using HUIT_Library.Services;
using Microsoft.AspNetCore.Authorization;
using HUIT_Library.Services.IServices;

namespace HUIT_Library.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileUserController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly IConfiguration _configuration;

        public ProfileUserController(IProfileService profileService, IConfiguration configuration)
        {
            _profileService = profileService;
            _configuration = configuration;
        }


        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _profileService.UpdateProfileAsync(request);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("get-profile")]
        public async Task<IActionResult> GetProfile([FromBody] GetProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var profile = await _profileService.GetUserProfileAsync(request.UserId);
            if (profile == null)
            {
                return NotFound();
            }

            return Ok(profile);
        }
    }
}
