using System;
using System.Linq;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;

namespace ClientLibraryConsoleAppSample
{
    class Program
    {
        //============= Config [Edit these with your settings] =====================
        internal const string azureDevOpsOrganizationUrl = "https://dev.azure.com/octono"; //change to the URL of your Azure DevOps account; NOTE: This must use HTTPS
        // internal const string vstsCollectioUrl = "http://myserver:8080/tfs/DefaultCollection" alternate URL for a TFS collection
        //==========================================================================

        //Console application to execute a user defined work item query
        static void Main(string[] args)
        {
            //Prompt user for credential
            VssConnection connection = new VssConnection(new Uri(azureDevOpsOrganizationUrl), new VssClientCredentials());

            GetReleases(connection);
            Console.ReadLine();

            //create http client and query for resutls
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();
            //Wiql query = new Wiql() { Query = "SELECT [Id], [Title], [State] FROM workitems WHERE [Work Item Type] = 'Bug' AND [Assigned To] = @Me" };
            Wiql query = new Wiql() { Query = "SELECT * FROM workitems WHERE [Work Item Type] = 'Bug' AND [Assigned To] = @Me" };
            WorkItemQueryResult queryResults = witClient.QueryByWiqlAsync(query).Result;

            //Display reults in console
            if (queryResults == null || queryResults.WorkItems.Count() == 0)
            {
                Console.WriteLine("Query did not find any results");
            }
            else
            {
                foreach (var column in queryResults.Columns)
                {
                    Console.WriteLine(column.Name);
                }

                foreach (var item in queryResults.WorkItems)
                {
                    Console.WriteLine(item.Id);
                }
            }

            Console.ReadLine();
        }

        private static void GetReleases(VssConnection connection)
        {
            string projectName = "formbuilder";

            var releaseClient = connection.GetClient<ReleaseHttpClient>();
            var buildClient = connection.GetClient<BuildHttpClient>();
            var wiclient = connection.GetClient<WorkItemTrackingHttpClient>();

            var releaseList = releaseClient.GetReleasesAsync(project: projectName, 
                minCreatedTime: DateTime.Now.AddDays(-10), maxCreatedTime: DateTime.Now).Result;

            if (releaseList.Count > 0)
            {
                foreach (var releaseItem in releaseList)
                {
                    var release = releaseClient.GetReleaseAsync(project: projectName, releaseId: releaseItem.Id).Result;

                    Console.WriteLine($"Deployment Report for {release.Name}");
                    var deploymentArtifact = release.Artifacts.FirstOrDefault();

                    if (deploymentArtifact != null)
                    {
                        var buildRunId = Convert.ToInt32(deploymentArtifact.DefinitionReference["version"].Id);
                        var buildVersion = deploymentArtifact.DefinitionReference["version"].Name;

                        var workItemList = buildClient.GetBuildWorkItemsRefsAsync(project: projectName, buildRunId).Result;
                        Console.WriteLine($"{String.Empty.PadLeft(5)} List of workitems associated to Build {buildVersion} ");

                        foreach (var workitem in workItemList)
                        {
                            var wi = wiclient.GetWorkItemAsync(id: Convert.ToInt32(workitem.Id), expand: Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemExpand.Relations).Result;
                            Console.WriteLine($"{String.Empty.PadLeft(10)} {wi.Id} - {wi.Fields["System.Title"]} ");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unable to Locate Artifact for this deployment");
                    }
                    Console.WriteLine(" ");
                }
            }
            else
            {
                Console.WriteLine("No Deployments found for this period.");
            }
        }
    }
}
