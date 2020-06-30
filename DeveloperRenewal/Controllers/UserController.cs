using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
// using AspNetCore.Identity.LiteDB.Models;
using LiteDB.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperRenewal.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly SignInManager<LiteDbUser> _signInManager;
        private readonly UserManager<LiteDbUser> _userManager;

        public UserController(SignInManager<LiteDbUser> signInManager,
            UserManager<LiteDbUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            ViewBag.returnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login(string provider, string returnUrl)
        {
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, Url.Action("LoginCallback", new { returnUrl }));
            return new ChallengeResult(provider, properties);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> LoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ViewBag.ErrorMessage = $"快速登录失败: {remoteError}";
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ViewBag.ErrorMessage = "获取快速登录信息失败";
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                ViewBag.ErrorMessage = "用户已被锁定，无法登录";
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var user = new LiteDbUser { UserName = email, Email = email };
                var result1 = await _userManager.CreateAsync(user);
                if (result1.Succeeded)
                {
                    result1 = await _userManager.AddLoginAsync(user, info);
                    if (result1.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }
                ViewBag.ErrorMessage = string.Join("<br />", result1.Errors.Select(x => x.Description).ToArray());
            }
            else
            {
                ViewBag.ErrorMessage = "登录信息非邮箱，无法登录";
            }
            return RedirectToAction("Login", new { ReturnUrl = returnUrl });
        }
    }
}