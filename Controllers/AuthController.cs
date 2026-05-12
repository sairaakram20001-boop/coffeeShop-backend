using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoffeeShop.Data;
using CoffeeShop.DTOs.Auth;
using CoffeeShop.Models;
using CoffeeShop.Services;
using System.Security.Cryptography;
using System.Text;

namespace CoffeeShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        public AuthController(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // ---------------- REGISTER ----------------
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            // check if user exists
            var userExists = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (userExists != null)
                return BadRequest(new { message = "User already exists" });

            // create user
            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        // ---------------- LOGIN ----------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (user == null)
                return BadRequest(new { message = "Invalid credentials" });

            if (!VerifyPassword(dto.Password, user.Password))
                return BadRequest(new { message = "Invalid credentials" });

            var token = _authService.GenerateToken(user.Id.ToString(), user.Email);

            return Ok(new
            {
                message = "Login successful",
                token = token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email
                }
            });
        }

        // ---------------- PASSWORD HASH ----------------
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }
    }
}