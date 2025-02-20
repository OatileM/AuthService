using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok("This is an admin-only endpoint");
        }

        [HttpGet("user-only")]
        [Authorize(Roles = "User")]
        public IActionResult UserOnly()
        {
            return Ok("This is a user-only endpoint");
        }

        [HttpGet("admin-or-user")]
        [Authorize(Roles = "Admin,User")]
        public IActionResult AdminOrUser()
        {
            return Ok("This endpoint is accessible to both admins and users");
        }
    }
}
