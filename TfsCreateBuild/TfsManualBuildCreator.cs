using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TfsBuildResultPublisher
{
    public class TfsManualBuildCreator : ITfsManualBuildCreator
    {
        public void CreateManualBuild(string buildStatus, string collection, string buildLog, string dropPath, string buildFlavour, string localPath, string buildPlatform, string buildTarget, string project, string buildDefinition, bool createBuildDefinitionIfNotExists, string buildController, string buildNumber, string serverPath, bool keepForever, int[] associatedChangesetIds, int[] associatedWorkitemIds, bool autoIncludeChangesetWorkItems, bool buildQueueDisabled)
        {
            // Get the TeamFoundation Server
            var tfsCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collection));

            // Get the Build Server
            var buildServer = (IBuildServer)tfsCollection.GetService(typeof(IBuildServer));

            // Create a fake definition
            var definition = CreateOrGetBuildDefinition(buildServer, project, buildDefinition, createBuildDefinitionIfNotExists,
                buildController, dropPath, buildQueueDisabled);

            // Create the build detail object
            var buildDetail = definition.CreateManualBuild(buildNumber);
            buildDetail.KeepForever = keepForever;

            // Create platform/flavor information against which test results can be published
            var buildProjectNode = buildDetail.Information.AddBuildProjectNode(buildFlavour, localPath, buildPlatform, serverPath, DateTime.Now, buildTarget);

            if (!string.IsNullOrEmpty(dropPath))
                buildDetail.DropLocation = dropPath;

            if (!string.IsNullOrEmpty(buildLog))
                buildDetail.LogLocation = buildLog;

            WorkItem[] workItems = {};
            if (associatedChangesetIds != null)
            {
                var service = tfsCollection.GetService<VersionControlServer>();
                var changesets = associatedChangesetIds.Select(changesetId => service.GetChangeset(changesetId, false, false)).ToArray();
                buildProjectNode.Node.Children.AddAssociatedChangesets(changesets);

                if (autoIncludeChangesetWorkItems)
                    workItems = workItems.Concat(changesets.SelectMany(c => c.WorkItems)).ToArray();
            }

            if (associatedWorkitemIds != null)
            {
                var service = tfsCollection.GetService<WorkItemStore>();
                workItems = workItems.Concat(associatedWorkitemIds.Select(id => service.GetWorkItem(id))).ToArray();
            }

            buildProjectNode.Node.Children.AddAssociatedWorkItems(workItems);
            const string integrationBuild = "Integration Build";
            foreach (var wi in workItems.Where(wi => wi.Fields.Contains(integrationBuild)))
            {
                wi.Fields[integrationBuild].Value = definition.Name + "/" + buildDetail.BuildNumber;
                wi.Save();
            }

            buildProjectNode.Save();

            // Complete the build by setting the status to succeeded
            var buildStatusEnum = (BuildStatus)Enum.Parse(typeof(BuildStatus), buildStatus);
            buildDetail.FinalizeStatus(buildStatusEnum);
        }

        private IBuildDefinition CreateOrGetBuildDefinition(
            IBuildServer buildServer, 
            string project, string buildDefinition,
            bool createBuildDefinitionIfNotExists,
            string buildController, string dropLocation, bool buildQueueDisabled)
        {
            try
            {
                return buildServer.GetBuildDefinition(project, buildDefinition);
            }
            catch (BuildDefinitionNotFoundException)
            {
                if (!createBuildDefinitionIfNotExists)
                    throw;
            }

            Console.WriteLine("'{0}' does not exist, trying to create build definition", buildDefinition);
            return CreateBuildDefinition(buildServer, buildController, project, dropLocation, buildDefinition, buildQueueDisabled);
        }

        private IBuildDefinition CreateBuildDefinition(IBuildServer buildServer, string buildController, string project, string dropLocation, string buildDefinition, bool buildQueueDisabled)
        {
            var controller = GetBuildController(buildServer, buildController);

            // Get the Upgrade template to use as the process template
            var processTemplate = buildServer.QueryProcessTemplates(project, new[] { ProcessTemplateType.Upgrade })[0];

            var definition = buildServer.CreateBuildDefinition(project);
            definition.Name = buildDefinition;
            definition.ContinuousIntegrationType = ContinuousIntegrationType.None;
            definition.BuildController = controller;
            definition.DefaultDropLocation = dropLocation;
            definition.Description = "Fake build definition used to create fake builds.";
            definition.QueueStatus = buildQueueDisabled ? DefinitionQueueStatus.Disabled : DefinitionQueueStatus.Enabled;
            definition.Workspace.AddMapping("$/", "c:\\fake", WorkspaceMappingType.Map);
            definition.Process = processTemplate;
            definition.Save();

            return definition;
        }

        private IBuildController GetBuildController(IBuildServer buildServer, string buildController)
        {
            if (string.IsNullOrEmpty(buildController))
                return buildServer.QueryBuildControllers(false).First();

            return buildServer.GetBuildController(buildController);
        }
    }
}