using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace QueryWorkitemsUsingTFSApi
{
    public static class MyExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int maxItems)
        {
            return items.Select((item, inx) => new { item, inx })
                .GroupBy(x => x.inx / maxItems)
                .Select(g => g.Select(x => x.item));
        }
    }


    internal class Program
    {
        internal const string VstsCollectionUrl = "https://yourtfsserver/tfs/DefaultCollection"; 

        private static void Main()
        {
            var connection = new VssConnection(new Uri(VstsCollectionUrl), new VssClientCredentials());
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            var queryResults = witClient.QueryByWiqlAsync(new Wiql
            {
                Query = "SELECT [Id] FROM workitems WHERE [Team Project] = '<theproject>' "
            }).Result;

            var batches = queryResults
                .WorkItems
                .Select(wi => wi.Id)
                .Batch(100)         // Too many workitems can make the url too long, so we split ut up
                .Select(b => b.ToArray());

            const string descriptionField = "Microsoft.VSTS.Common.DescriptionHtml";
            const string systemIdField = "System.Id";

            foreach (var batch in batches)
            {
                var workItems = witClient
                    .GetWorkItemsAsync(
                        batch,
                        new string[]
                        {
                            systemIdField,
                            descriptionField
                        },
                        queryResults.AsOf
                    )
                    .Result
                    .Where(wi => wi.Fields.ContainsKey(descriptionField));

                foreach (WorkItem wi in workItems)
                {
                     Console.WriteLine($"{wi.Fields[systemIdField]}, {wi.Fields[descriptionField]}");
                }
            }
        }
    }
}
