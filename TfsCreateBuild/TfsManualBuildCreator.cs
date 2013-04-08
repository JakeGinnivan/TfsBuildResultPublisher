using System;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;

namespace TfsCreateBuild
{
    public class TfsManualBuildCreator : ITfsManualBuildCreator
    {
        public void CreateManualBuild(string buildStatus, string collection, string buildLog, string dropPath, string buildFlavour, string localPath, string buildPlatform, string buildTarget, string project, string buildDefinition, bool createBuildDefinitionIfNotExists, string buildController, string buildNumber, string serverPath)
        {
            // Get the TeamFoundation Server
            var tfsCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(collection));

            // Get the Build Server
            var buildServer = (IBuildServer)tfsCollection.GetService(typeof(IBuildServer));

            // Create a fake definition
            var definition = CreateOrGetBuildDefinition(buildServer, project, buildDefinition, createBuildDefinitionIfNotExists,
                buildController, dropPath);

            // Create the build detail object
            var buildDetail = definition.CreateManualBuild(buildNumber);

            // Create platform/flavor information against which test results can be published
            var buildProjectNode = buildDetail.Information.AddBuildProjectNode(buildFlavour, localPath, buildPlatform, serverPath, DateTime.Now, buildTarget);

            if (!string.IsNullOrEmpty(dropPath))
                buildDetail.DropLocation = dropPath;

            if (!string.IsNullOrEmpty(buildLog))
                buildDetail.LogLocation = buildLog;

            buildProjectNode.Save();

            // Complete the build by setting the status to succeeded
            var buildStatusEnum = (BuildStatus)Enum.Parse(typeof(BuildStatus), buildStatus);
            buildDetail.FinalizeStatus(buildStatusEnum);
        }

        private IBuildDefinition CreateOrGetBuildDefinition(
            IBuildServer buildServer, 
            string project, string buildDefinition,
            bool createBuildDefinitionIfNotExists, 
            string buildController, string dropLocation)
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
            return CreateBuildDefinition(buildServer, buildController, project, dropLocation, buildDefinition);
        }

        private IBuildDefinition CreateBuildDefinition(IBuildServer buildServer, string buildController, string project, string dropLocation, string buildDefinition)
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
            definition.QueueStatus = DefinitionQueueStatus.Enabled;
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