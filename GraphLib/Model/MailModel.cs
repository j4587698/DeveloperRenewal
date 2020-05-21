using Microsoft.Graph;

namespace GraphLib.Model
{
    public class MailModel
    {
        public string Subject { get; set; }

        public string Body { get; set; }

        public EmailAddress Sender { get; set; }


    }
}