using DeveloperRenewal.Entity;
using FluentScheduler;
using GraphLib;
using GraphLib.Utils;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphLib.File;
using GraphLib.Mail;

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
            var schedule = new Schedule(async () =>
            {
                Graph graph = new Graph(application.ClientId, application.ClientSecret, "http://localhost", Constants.Scopes);
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

            }, run => run.OnceIn(random.Next(application.MinExecInterval, application.MaxExecInterval)).Seconds());
            schedule.JobEnded += (sender, e) =>
            {
                schedule.Stop();
                application = LiteDbHelper.Instance.GetDataById<ApplicationEntity>(nameof(ApplicationEntity), id);
                if (application.AuthorizationStatus && application.IsEnable)
                {
                    schedule.SetScheduling(run => run.OnceIn(random.Next(application.MinExecInterval, application.MaxExecInterval)).Seconds());
                    schedule.Start();
                }
                
            };
            schedule.Start();
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
