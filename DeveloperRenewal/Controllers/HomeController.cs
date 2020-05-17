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
                RedirectToAction("Login", "User");
            }

            if (message != null)
            {
                ViewBag.Message = message;
            }

            ViewBag.Applictions = LiteDbHelper.Instance.GetAllDataToList<ApplicationEntity>(nameof(ApplicationEntity));

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
                application = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity))
                    .FindOne(x => x.Id == id);
                ViewBag.Title = "修改当前应用";
            }
            ViewBag.CreateUrl = Graph.GetCreateAppUrl("DeveloperRenewal", Request.GetRegisterUrl());
            return View(application);
        }

        [HttpPost]
        public IActionResult AddApplication(ApplicationEntity application)
        {
            if (application.ClientId.IsNullOrEmpty() || application.ClientSecret.IsNullOrEmpty())
            {
                ViewBag.ErrorMessage = "Client Id与Client Secret不能为空";
                return View();
            }

            if (application.MinExecInterval < 600)
            {
                ViewBag.ErrorMessage = "最小时间不能小于600秒";
                return View();
            }

            if (application.MaxExecInterval < application.MinExecInterval)
            {
                ViewBag.ErrorMessage = "最大时间不能小于最小时间";
                return View();
            }

            application.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            LiteDbHelper.Instance.InsertOrUpdate(nameof(ApplicationEntity), application);

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
