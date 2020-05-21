using System.Threading.Tasks;

namespace GraphLib.Calendar
{
    public class Calendar
    {
        /// <summary>
        /// 获取日历信息
        /// </summary>
        /// <param name="graph"></param>
        public static async Task<Microsoft.Graph.Calendar> GetCalendar(Graph graph)
        {
            var client = await graph.GetGraph();
            var calendar = await client.Me.Calendar
                .Request()
                .GetAsync();
            return calendar;
        }
    }
}