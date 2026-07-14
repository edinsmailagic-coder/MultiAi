using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Users> userManager;
        private readonly SignInManager<Users> signInManager;
        private readonly IJwtService jwtService;

        public AuthController(UserManager<Users> userManager,SignInManager<Users> signInManager, IJwtService jwtService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            if(user == null)
            {
                return Unauthorized();
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

            if (!result.Succeeded)
                return Unauthorized();

            var token = jwtService.CreateToken(user);

            return Ok(new { token });
        }
    }
}
