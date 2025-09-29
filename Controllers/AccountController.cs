using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [Route("Account/Register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });
            }
            try
            {
                // Ensure Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    if (!roleResult.Succeeded)
                    {
                        return BadRequest(new { message = string.Join("; ", roleResult.Errors.Select(e => e.Description)) });
                    }
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    PhoneNumber = model.PhoneNumber
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");//this add the relation between user and role
                    TempData["ShowSuccessToaster"] = true;
                    return Ok(new { success = true, message = "User created successfully.", redirectUrl = Url.Action("Index", "User") });
                }

                return BadRequest(new { success = false, message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = string.Join("; ",
                        ModelState.Values.SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage))
                });
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                // For toaster notification on redirected page
                TempData["ShowLoginSuccessToaster"] = true;
                // Re-sign in with custom properties
                var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User not found."
                    });
                }
                // Customize authentication properties for RememberMe
                if (model.RememberMe)
                {

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Persist across browser sessions
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // 7-day total lifetime
                    };
                    await _signInManager.SignInAsync(user, authProperties);
                }

                // Check user roles
                var roles = await _signInManager.UserManager.GetRolesAsync(user);
                if (roles.Contains("Client", StringComparer.OrdinalIgnoreCase))
                {
                    var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (profile == null)
                    {
                        return StatusCode(500, new
                        {
                            success = false,
                            message = "User profile not found. Please contact support."
                        });
                    }

                    if (profile.RegistrationStep < 5)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Login successful, redirecting to registration step.",
                            redirectUrl = Url.Action("RegistrationStep", "Clients")
                        });
                    }

                    else
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Login successful, redirecting to client dashboard.",
                            redirectUrl = Url.Action("Dashboard", "Clients")
                        });
                    }
                }
                else
                {
                    // Admin or other roles redirect to Home/Index
                    return Ok(new
                    {
                        success = true,
                        message = "Login successful, redirecting to home.",
                        redirectUrl = Url.Action("Index", "Home")
                    });
                }
            }

            return BadRequest(new
            {
                success = false,
                message = "Invalid login attempt. Please check your email and password."
            });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new
            {
                success = true,
                message = "Logout successful",
                redirectUrl = Url.Action("Login", "Account")
            });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}