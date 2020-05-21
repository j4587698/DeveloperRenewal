using System.Threading.Tasks;
using Microsoft.Graph;

namespace GraphLib.Mail
{
    public class Events
    {
        public static async Task<IUserEventsCollectionPage> GetEvents(Graph graph)
        {
            var client = await graph.GetGraph();
            var events = await client.Me.Events.Request().GetAsync();
            return events;
        }
    }
}