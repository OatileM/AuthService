using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using AuthService.DTOs;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ITokenService tokenService)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null)
                return BadRequest(new { Message = "Registration model cannot be null" });

            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Invalid model state", Errors = ModelState });

            if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Email))
                return BadRequest(new { Message = "Username and email are required" });

            // Check if user already exists
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
                return BadRequest(new { Message = "Username already exists" });

            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
                return BadRequest(new { Message = "Email already registered" });

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { Message = "User creation failed", Errors = result.Errors });

            // Ensure "User" role exists
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var role = new Role { Name = "User" };
                var roleResult = await _roleManager.CreateAsync(role);
                if (!roleResult.Succeeded)
                    return StatusCode(500, new { Message = "Default role creation failed" });
            }

            // Add default role
            var addToRoleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!addToRoleResult.Succeeded)
                return StatusCode(500, new { Message = "Failed to assign default role" });

            try
            {
                var token = await _tokenService.GenerateToken(user);
                return Ok(new TokenResponse { Token = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error generating token", Error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null)
                return BadRequest(new { Message = "Login model cannot be null" });

            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Invalid model state", Errors = ModelState });

            if (string.IsNullOrEmpty(model.UserName))
                return BadRequest(new { Message = "Username cannot be empty" });

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
                return Unauthorized(new { Message = "Invalid username or password" });

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
                return Unauthorized(new { Message = "Invalid username or password" });

            try
            {
                var token = await _tokenService.GenerateToken(user);
                return Ok(new TokenResponse { Token = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error generating token", Error = ex.Message });
            }
        }

        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            if (request == null)
                return BadRequest(new { Message = "Request cannot be null" });

            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.RoleName))
                return BadRequest(new { Message = "UserId and RoleName are required" });

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            // Check if role exists
            var roleExists = await _roleManager.RoleExistsAsync(request.RoleName);
            if (!roleExists)
            {
                var role = new Role { Name = request.RoleName };
                var createRoleResult = await _roleManager.CreateAsync(role);
                if (!createRoleResult.Succeeded)
                    return BadRequest(new { Message = "Role creation failed", Errors = createRoleResult.Errors });
            }

            // Check if user already has the role
            if (await _userManager.IsInRoleAsync(user, request.RoleName))
                return BadRequest(new { Message = "User already has this role" });

            // Assign role to user
            var result = await _userManager.AddToRoleAsync(user, request.RoleName);
            if (!result.Succeeded)
                return BadRequest(new { Message = "Role assignment failed", Errors = result.Errors });

            try
            {
                var newToken = await _tokenService.GenerateToken(user);
                return Ok(new TokenResponse { Token = newToken });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error generating token", Error = ex.Message });
            }
        }

        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.NormalizedName
                })
                .ToList();

            return Ok(new { Roles = roles });
        }

        [HttpGet("user-roles")]
        [Authorize]
        public async Task<IActionResult> GetUserRoles()
        {
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                return Unauthorized(new { Message = "User not authenticated" });

            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { UserRoles = roles });
        }
    }
}
