using System;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;

namespace TfsBuildResultPublisher
{
    public class BuildInvoker : IBuildInvoker
    {
        /// <summary>
        /// Queues a build, then waits for it to completed
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="project"></param>
        /// <param name="buildDefinition"></param>
        /// <returns>Build number generated</returns>
        public string TriggerBuildAndWaitForCompletion(string collection, string project, string buildDefinition)
        {
            // Get the TeamFoundation Server
            var tfsCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collection));

            // Get the Build Server
            var buildServer = (IBuildServer)tfsCollection.GetService(typeof(IBuildServer));

            var tfsBuildDefinition = buildServer.GetBuildDefinition(project, buildDefinition);

            var queuedBuild = buildServer.QueueBuild(tfsBuildDefinition);

            queuedBuild.WaitForBuildCompletion(TimeSpan.FromSeconds(1), TimeSpan.FromHours(1));

            return queuedBuild.Build.BuildNumber;
        }
    }
}