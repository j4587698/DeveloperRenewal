using DeveloperRenewal.Entity;
using FluentScheduler;
using GraphLib;
using GraphLib.Utils;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeveloperRenewal.Utils
{
    public class SchedulerUtil
    {
        public static bool AddScheduler(int id)
        {
            var appliction = LiteDbHelper.Instance.GetDataById<ApplicationEntity>(nameof(ApplicationEntity), id);
            if (appliction == null)
            {
                return false;
            }
            Random random = new Random();
            var schedule = new Schedule(async () =>
            {
                Graph graph = new Graph(appliction.ClientId, appliction.ClientSecret, new PathString("/Home/Register"), Constants.Scopes);
                var client = await graph.GetGraph();
                var request = await client.Me.Messages.Request().Top(5).GetAsync();
                
            }, run => run.OnceIn(random.Next(appliction.MinExecInterval, appliction.MaxExecInterval)).Seconds());
            schedule.JobEnded += (sender, e) =>
            {
                schedule.Stop();
                appliction = LiteDbHelper.Instance.GetDataById<ApplicationEntity>(nameof(ApplicationEntity), id);
                if (appliction.AuthorizationStatus && appliction.IsEnable)
                {
                    schedule.SetScheduling(run => run.OnceIn(random.Next(appliction.MinExecInterval, appliction.MaxExecInterval)));
                    schedule.Start();
                }
                
            };
            schedule.Start();
            return true;
        }
    }
}
