using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CRM.Models;
using Microsoft.AspNetCore.Authentication;

namespace CRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
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
                // Customize authentication properties for RememberMe
                if (model.RememberMe)
                {
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Persist across browser sessions
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // 7-day total lifetime
                    };
                    // Re-sign in with custom properties
                    var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        await _signInManager.SignInAsync(user, authProperties);
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "User not found."
                        });
                    }
                }
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Login successful",
                        redirectUrl = returnUrl
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    redirectUrl = Url.Action("Index", "Home")
                });
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