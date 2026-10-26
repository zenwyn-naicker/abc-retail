using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABCRetailWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService; 
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserService userService, ILogger<AccountController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userService.AuthenticateUserAsync(model.Email, model.Password);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    _logger.LogWarning("Failed login attempt for email: {Email}", model.Email);
                    return View(model);
                }

                // Create claims for the authenticated user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", user.FullName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                _logger.LogInformation("User logged in successfully: {Email}, Role: {Role}", model.Email, user.Role);
                TempData["Success"] = $"Welcome back, {user.FullName}!";

                // Redirect logic
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirect based on role
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "AdminOrders");
                }

                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var username = User.Identity?.Name;

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("User logged out: {Username}", username);
                TempData["Success"] = "You have been logged out successfully.";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("User ID claim not found in profile request");
                    return RedirectToAction(nameof(Login));
                }

                int userId = int.Parse(userIdClaim);
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User not found: UserId={UserId}", userId);
                    TempData["Error"] = "User profile not found.";
                    return RedirectToAction(nameof(Login));
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["Error"] = "Failed to load profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            TempData["Error"] = "You do not have permission to access this page.";
            return View();
        }
    }
}