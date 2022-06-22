using DeveloperRenewal.Entity;
using GraphLib;
using GraphLib.Utils;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeveloperRenewal.Trigger;
using GraphLib.File;
using GraphLib.Mail;
using Longbow.Tasks;

namespace DeveloperRenewal.Utils
{
    public class SchedulerUtil
    {
        public static bool AddScheduler(int id)
        {
            var application = LiteDbHelper.Instance.GetDataById<ApplicationEntity>(nameof(ApplicationEntity), id);
            if (application == null)
            {
                return false;
            }
            Random random = new Random();
            TaskServicesManager.GetOrAdd($"scheduler{id}", async token =>
                {
                    var random = new Random();
                    var interval = random.Next(application.MaxExecInterval - application.MinExecInterval);
                    await Task.Delay(TimeSpan.FromSeconds(interval));
                    Console.WriteLine("开始执行");
                    Graph graph = new Graph(application.ClientId, application.ClientSecret, "http://localhost",
                        Constants.Scopes);
                    var key = random.Next() % 3;
                    switch (key)
                    {
                        case 0:
                            await ReadMail(graph, application);
                            break;
                        case 1:
                            await ReadEvent(graph, application);
                            break;
                        case 2:
                            await ReadFiles(graph, application);
                            break;
                    }

                    application.LastExecTime = DateTime.Now;
                    LiteDbHelper.Instance.InsertOrUpdate(nameof(ApplicationEntity), application);
                },
                TriggerBuilder.Default.WithInterval(TimeSpan.FromSeconds(application.MinExecInterval)).WithRepeatCount(0).Build());
            return true;
        }

        private static async Task ReadMail(Graph graph, ApplicationEntity application)
        {
            try
            {
                var mailList = await Mail.GetMailInfo(graph, 5);
                LogEntity log = new LogEntity
                {
                    ApplicationId = application.Id,
                    Message = $"读取条数:{mailList.Count}",
                    Operation = "读取邮箱",
                    Status = "成功",
                    CreateDate = DateTime.Now
                };
                LiteDbHelper.Instance.InsertOrUpdate(nameof(LogEntity), log);

            }
            catch (Exception e)
            {
                LogEntity log = new LogEntity
                {
                    ApplicationId = application.Id,
                    Message = e.Message,
                    Operation = "读取邮箱",
                    Status = "失败",
                    CreateDate = DateTime.Now
                };
                LiteDbHelper.Instance.InsertOrUpdate(nameof(LogEntity), log);
            }
        }

        private static async Task ReadEvent(Graph graph, ApplicationEntity application)
        {
            try
            {
                var events = await Events.GetEvents(graph);
                LogEntity log = new LogEntity
                {
                    ApplicationId = application.Id,
                    Message = $"读取条数:{events.Count}",
                    Operation = "读取日历事件",
                    Status = "成功",
                    CreateDate = DateTime.Now
                };
                LiteDbHelper.Instance.InsertOrUpdate(nameof(LogEntity), log);
            }
            catch (Exception e)
            {
                LogEntity log = new LogEntity
                {
                    ApplicationId = application.Id,
                    Message = e.Message,
                    Operation = "读取日历事件",
                    Status = "失败",
                    CreateDate = DateTime.Now
                };
                LiteDbHelper.Instance.InsertOrUpdate(nameof(LogEntity), log);
            }
        }

        private static async Task ReadFiles(Graph graph, ApplicationEntity application)
        {
            try
            {
                var files = await File.ListFiles(graph);
                LogEntity log = new LogEntity
                {
                    ApplicationId = application.Id,
                    Message = $"读取条数:{files.Count}",
                    Operation = "读取OneDrive",
                    Status = "成功",
                    CreateDate = DateTime.Now
                };
                LiteDbHelper.Instance.InsertOrUpdate(nameof(LogEntity), log);
            }
            catch (Exception e)
            {
                LogEntity log = new LogEntity
                {
                    ApplicationId = application.Id,
                    Message = e.Message,
                    Operation = "读取OneDrive",
                    Status = "失败",
                    CreateDate = DateTime.Now
                };
                LiteDbHelper.Instance.InsertOrUpdate(nameof(LogEntity), log);
            }
        }
    }
}
