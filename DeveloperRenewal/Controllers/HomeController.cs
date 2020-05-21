using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCore.Identity.LiteDB.Data;
using AspNetCore.Identity.LiteDB.Models;
using DeveloperRenewal.Entity;
using DeveloperRenewal.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeveloperRenewal.Models;
using DeveloperRenewal.Utils;
using GraphLib;
using GraphLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace DeveloperRenewal.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ILiteDbContext _liteDbContext;

        public HomeController(ILogger<HomeController> logger, ILiteDbContext liteDbContext)
        {
            _logger = logger;
            _liteDbContext = liteDbContext;
        }

        public IActionResult Index(string message = null)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier);
            if (id == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (message != null)
            {
                ViewBag.Message = message;
            }

            ViewBag.Applictions = LiteDbHelper.Instance.GetAllData<ApplicationEntity>(nameof(ApplicationEntity)).Where(x => x.UserId == id.Value);

            return View();
        }

        [HttpGet]
        public IActionResult AddApplication(int id)
        {
            ApplicationEntity application;
            if (id == 0)
            {
                ViewBag.Title = "添加新应用";
                application = new ApplicationEntity();
            }
            else
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return RedirectToAction("Login", "User");
                }
                application = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity))
                    .FindOne(x => x.Id == id && x.UserId == userId.Value);
                if (application == null)
                {
                    return RedirectToAction("Index", new {message = "当前应用不存在或当前应用不属于你"});
                }
                ViewBag.Title = "修改当前应用";
            }
            ViewBag.CreateUrl = Graph.GetCreateAppUrl("DeveloperRenewal", Request.GetRegisterUrl());
            return View(application);
        }

        [HttpPost]
        public IActionResult AddApplication(ApplicationEntity application)
        {
            ViewBag.Title = application.Id == 0 ? "添加新应用" : "修改当前应用";
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var application1 = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity))
                .FindOne(x => x.Id == application.Id && x.UserId == userId.Value);
            if (application1 == null)
            {
                return RedirectToAction("Index", new { message = "当前应用不存在或当前应用不属于你" });
            }
            if (application.ClientId.IsNullOrEmpty() || application.ClientSecret.IsNullOrEmpty())
            {
                ViewBag.ErrorMessage = "Client Id与Client Secret不能为空";
                return View(application);
            }

            if (application.MinExecInterval < 600)
            {
                ViewBag.ErrorMessage = "最小时间不能小于600秒";
                return View(application);
            }

            if (application.MaxExecInterval < application.MinExecInterval)
            {
                ViewBag.ErrorMessage = "最大时间不能小于最小时间";
                return View(application);
            }

            application.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            LiteDbHelper.Instance.InsertOrUpdate(nameof(ApplicationEntity), application);

            Graph graph = new Graph(application.ClientId, application.ClientSecret, Request.GetRegisterUrl(), Constants.Scopes);
            return Redirect(graph.GetAuthUrl(application.Id.ToString()));
        }

        public IActionResult ReAuth(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index");
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var application = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity))
                .FindOne(x => x.Id == id && x.UserId == userId.Value);
            if (application == null)
            {
                return RedirectToAction("Index", new { message = "当前应用不存在或当前应用不属于你" });
            }
            Graph graph = new Graph(application.ClientId, application.ClientSecret, Request.GetRegisterUrl(), Constants.Scopes);
            return Redirect(graph.GetAuthUrl(application.Id.ToString()));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Register(string code, string state)
        {
            string message = "";
            if (code.IsNullOrEmpty() || state.IsNullOrEmpty())
            {
                message = "返回信息有误";
                return RedirectToAction("Index", new { message = message });
            }

            if (!int.TryParse(state, out int id))
            {
                message = "Id信息不正确";
                return RedirectToAction("Index", new { message = message });
            }

            var application = LiteDbHelper.Instance.GetDataById<ApplicationEntity>(nameof(ApplicationEntity), id);

            if (application == null)
            {
                message = "未找到对应的应用信息";
                return RedirectToAction("Index", new { message = message });
            }

            Graph graph = new Graph(application.ClientId, application.ClientSecret, Request.GetRegisterUrl(), Constants.Scopes);
            var result = await graph.GetGraphWithCode(code);
            if (result == null)
            {
                return RedirectToAction("Index", new { message = "添加失败" });
            }

            application.AuthorizationStatus = true;
            application.IsEnable = true;
            LiteDbHelper.Instance.InsertOrUpdate(nameof(ApplicationEntity), application);
            SchedulerUtil.AddScheduler(application.Id);
            return RedirectToAction("Index", new { message = "添加成功" });
        }

        public IActionResult ShowLog(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index");
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var application = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity))
                .FindOne(x => x.Id == id && x.UserId == userId.Value);
            if (application == null)
            {
                return RedirectToAction("Index", new { message = "当前应用不存在或当前应用不属于你" });
            }
            var model = LiteDbHelper.Instance.GetAllData<LogEntity>(nameof(LogEntity)).Where(x => x.ApplicationId == id).Reverse().ToList();
            return View(model);
        }

        public IActionResult DeleteApplication(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var application = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity))
                .FindOne(x => x.Id == id && x.UserId == userId.Value);
            if (application == null)
            {
                return RedirectToAction("Index", new { message = "当前应用不存在或当前应用不属于你" });
            }
            LiteDbHelper.Instance.Delete(nameof(LogEntity), id);
            LiteDbHelper.Instance.Delete(nameof(ApplicationEntity), application);
            return RedirectToAction("Index");
        }

        public IActionResult DeleteApplications(string[] ids)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var applications = LiteDbHelper.Instance.GetAllData<ApplicationEntity>(nameof(ApplicationEntity))
                .Where(x => x.UserId == userId.Value && ids.Contains(x.Id.ToString())).ToList();
            LiteDbHelper.Instance.GetCollection<LogEntity>(nameof(LogEntity)).DeleteMany(x => applications.Select(y => y.Id).Contains(x.ApplicationId));
            LiteDbHelper.Instance.Delete(nameof(ApplicationEntity), applications);
            return RedirectToAction("Index");
        }

        public IActionResult EnableApplications(string[] ids)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var applications = LiteDbHelper.Instance.GetAllData<ApplicationEntity>(nameof(ApplicationEntity))
                .Where(x => x.UserId == userId.Value && ids.Contains(x.Id.ToString())).ToList();
            foreach (var applicationEntity in applications)
            {
                applicationEntity.IsEnable = true;
            }
            LiteDbHelper.Instance.InsertOrUpdateBatch(nameof(ApplicationEntity), applications);
            return RedirectToAction("Index");
        }

        public IActionResult DisableApplications(string[] ids)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var applications = LiteDbHelper.Instance.GetAllData<ApplicationEntity>(nameof(ApplicationEntity))
                .Where(x => x.UserId == userId.Value && ids.Contains(x.Id.ToString())).ToList();
            foreach (var applicationEntity in applications)
            {
                applicationEntity.IsEnable = false;
            }
            LiteDbHelper.Instance.InsertOrUpdateBatch(nameof(ApplicationEntity), applications);
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
