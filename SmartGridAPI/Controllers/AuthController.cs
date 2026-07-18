using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartGridAPI.Data;
using SmartGridAPI.DTOs;
using SmartGridAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SmartGridAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IMemoryCache _cache;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger, IMemoryCache cache)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>Register a new user</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid input data", errors = ModelState });

                if (dto.Password != dto.ConfirmPassword)
                    return BadRequest(new { success = false, message = "Passwords do not match" });

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email || u.Username == dto.Username);

                if (existingUser != null)
                    return Conflict(new { success = false, message = "Username or email already exists" });

                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered: {Username}", user.Username);

                var (token, expiry) = GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    message = "Registration successful",
                    data = new AuthResponseDTO
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        Token = token,
                        ExpiresAt = expiry
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", dto.Email);
                return StatusCode(500, new { success = false, message = "Registration failed. Please try again." });
            }
        }

        /// <summary>Login with username or email</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid input data" });

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.UsernameOrEmail || u.Username == dto.UsernameOrEmail);

                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                    return Unauthorized(new { success = false, message = "Invalid credentials" });

                if (!user.IsActive)
                    return Forbid();

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User logged in: {Username}", user.Username);

                var (token, expiry) = GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = new AuthResponseDTO
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Role,
                        Token = token,
                        ExpiresAt = expiry
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {UsernameOrEmail}", dto.UsernameOrEmail);
                return StatusCode(500, new { success = false, message = "Login failed. Please try again." });
            }
        }

        /// <summary>Get current user profile</summary>
        [HttpGet("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Unauthorized();

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role,
                        user.IsActive,
                        user.CreatedAt,
                        user.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile");
                return StatusCode(500, new { success = false, message = "Error retrieving profile" });
            }
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] OtpRequestDto request)
        {
            if (string.IsNullOrEmpty(request.CredentialId) || string.IsNullOrEmpty(request.PhoneNumber))
                return BadRequest(new { success = false, message = "Credential ID and Phone Number are required." });

            string cleanCredentialId = request.CredentialId.Trim().ToUpper();
            string cleanPhone = request.PhoneNumber.Replace(" ", "").Replace("-", "").Trim();

            string idStr = cleanCredentialId.Replace("MTM-", "");
            if (!int.TryParse(idStr, out int memberId))
                return BadRequest(new { success = false, message = "Invalid Credential ID format." });

            var member = await _context.MaintenanceTeamMembers.FindAsync(memberId);
            if (member == null || member.PhoneNumber != cleanPhone)
                return NotFound(new { success = false, message = "Invalid Credential ID or Phone Number." });

            string otp = new Random().Next(100000, 999999).ToString();
            _cache.Set($"OTP_{cleanCredentialId}", otp, TimeSpan.FromMinutes(5));

            try
            {
                TwilioClient.Init(Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID"), Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN"));
                string toPhone = member.PhoneNumber;
                if (toPhone.Length == 10 && !toPhone.StartsWith("+"))
                {
                    toPhone = "+91" + toPhone;
                }
                var message = MessageResource.Create(
                    body: $"Your SmartGrid OTP is: {otp}. Valid for 5 minutes.",
                    from: new Twilio.Types.PhoneNumber("+17627012086"),
                    to: new Twilio.Types.PhoneNumber(toPhone)
                );
                _logger.LogInformation($"[SMS SUCCESS] OTP sent to {toPhone}. SID: {message.Sid}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SMS ERROR] Failed to send OTP SMS: {ex.Message}");
                // In a real app we'd fail, but for demo let it succeed (maybe they don't have Twilio setup)
                _logger.LogInformation($"[DEMO FALLBACK] OTP is {otp}");
            }

            return Ok(new { success = true, message = "OTP generated.", otp = otp });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto request)
        {
            if (string.IsNullOrEmpty(request.CredentialId) || string.IsNullOrEmpty(request.Otp))
                return BadRequest(new { success = false, message = "Credential ID and OTP are required." });

            string cleanCredentialId = request.CredentialId.Trim().ToUpper();
            string cleanOtp = request.Otp.Trim();

            if (!_cache.TryGetValue($"OTP_{cleanCredentialId}", out string? storedOtp) || storedOtp != cleanOtp)
            {
                // Accept "123456" as a universal demo backdoor
                if (cleanOtp != "123456")
                    return Unauthorized(new { success = false, message = "Invalid or expired OTP." });
            }

            // Remove OTP after use
            _cache.Remove($"OTP_{cleanCredentialId}");

            string idStr = cleanCredentialId.Replace("MTM-", "");
            int.TryParse(idStr, out int memberId);

            var member = await _context.MaintenanceTeamMembers.FindAsync(memberId);
            if (member == null) return NotFound(new { success = false, message = "Member not found." });

            // Generate token specifically for this Maintenance Member
            var user = new User
            {
                Id = member.UserId ?? memberId + 1000, // Dummy ID if no user record
                Username = cleanCredentialId,
                Email = $"{cleanCredentialId}@smartgrid.local",
                Role = "Maintenance",
            };

            var (token, expiry) = GenerateJwtToken(user, member.Name);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = new AuthResponseDTO
                {
                    Id = user.Id,
                    Username = member.Name, // Pass member name to frontend
                    Email = user.Email,
                    Role = user.Role,
                    Token = token,
                    ExpiresAt = expiry
                }
            });
        }

        private (string token, DateTime expiry) GenerateJwtToken(User user, string memberName = null)
        {
            var secretKey = _configuration["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = _configuration["JwtSettings:Issuer"] ?? "SmartGridAPI";
            var audience = _configuration["JwtSettings:Audience"] ?? "SmartGridClient";
            var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "1440");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, memberName ?? user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiry,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
        }
    }

    public class OtpRequestDto
    {
        public string CredentialId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class OtpVerifyDto
    {
        public string CredentialId { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}
