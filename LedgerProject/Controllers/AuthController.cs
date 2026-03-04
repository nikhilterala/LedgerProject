using LedgerProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LedgerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        private readonly UserService _userService;

        public AuthController(UserService userService,IConfiguration config)
        {
            _userService = userService;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromServices] UserService userService,string username,string password)
        {
            var user = await userService.ValidateUserAsync(username, password);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username)
            };

            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleName));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromServices] UserService userService, string username, string password,List<string> roles)
        {
            await userService.CreateUserAsync(username, password, roles);
            return Ok("User created successfully.");
        }
    }
}