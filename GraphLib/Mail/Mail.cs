using System.Collections.Generic;
using System.Threading.Tasks;
using GraphLib.Model;
using Microsoft.Graph;

namespace GraphLib.Mail
{
    public class Mail
    {
        /// <summary>
        /// 获取邮件信息
        /// </summary>
        /// <param name="graph">Graph实例</param>
        /// <param name="max">获取前多少条，全部为0</param>
        public static async Task<List<MailModel>> GetMailInfo(Graph graph, int max = 0)
        {
            var client = await graph.GetGraph();
            IUserMessagesCollectionPage request;
            if (max > 0)
            {
                request = await client.Me.Messages.Request().Top(max).GetAsync();
            }
            else
            {
                request = await client.Me.Messages.Request().GetAsync();
            }
            List<MailModel> mails = new List<MailModel>();
            foreach (var messageInfo in request)
            {
                var message = await client.Me.Messages[messageInfo.Id].Request().GetAsync();
                MailModel mailModel = new MailModel
                {
                    Subject = message.Subject, Sender = message.Sender.EmailAddress, Body = message.Body.Content
                };
                mails.Add(mailModel);
            }
            return mails;
        }
    }
}