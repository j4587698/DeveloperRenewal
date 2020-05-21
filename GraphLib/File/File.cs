using System.Threading.Tasks;
using Microsoft.Graph;

namespace GraphLib.File
{
    public class File
    {
        public static async Task<IDriveItemChildrenCollectionPage> ListFiles(Graph graph, string itemId = "")
        {
            var client = await graph.GetGraph();
            if (itemId != "")
            {
                return await client.Me.Drive.Items[itemId].Children.Request().GetAsync();
            }
            else
            {
                return await client.Me.Drive.Root.Children.Request().GetAsync();
            }
        }
    }
}