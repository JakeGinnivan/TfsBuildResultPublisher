using System;
using System.IO;

namespace TfsCreateBuild
{
    public class BuildCreator : IBuildCreator
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ITfsManualBuildCreator _manualBuildCreator;
        private readonly ITestRunPublisher _testRunPublisher;
        private readonly IBuildTestResultPublisher _buildTestResultsPublisher;
        private readonly IBuildInvoker _buildInvoker;

        public BuildCreator(
            IConfigurationProvider configurationProvider, ITfsManualBuildCreator manualBuildCreator, 
            ITestRunPublisher testRunPublisher, IBuildTestResultPublisher buildTestResultsPublisher, IBuildInvoker buildInvoker)
        {
            _configurationProvider = configurationProvider;
            _manualBuildCreator = manualBuildCreator;
            _testRunPublisher = testRunPublisher;
            _buildTestResultsPublisher = buildTestResultsPublisher;
            _buildInvoker = buildInvoker;
        }

        public int Execute(string[] args)
        {
            Configuration configuration;
            if (!_configurationProvider.TryProvide(args, out configuration))
                return 1;

            if (configuration.TriggerBuild)
            {
                Console.WriteLine("Triggering new build of '{0}'", configuration.BuildDefinition);
                configuration.BuildNumber = _buildInvoker.TriggerBuildAndWaitForCompletion(configuration.Collection, configuration.Project, configuration.BuildDefinition);
            }
            else
            {
                Console.WriteLine("Creating manual build '{0}'", configuration.BuildDefinition);
                _manualBuildCreator.CreateManualBuild(
                    configuration.BuildStatus, configuration.Collection, configuration.BuildLog, configuration.DropPath, configuration.BuildFlavor,
                    configuration.LocalPath, configuration.BuildPlatform, configuration.BuildTarget, configuration.Project, configuration.BuildDefinition,
                    configuration.CreateBuildDefinitionIfNotExists, configuration.BuildController, configuration.BuildNumber, configuration.ServerPath,
                    configuration.KeepForever);
            }

            if (!string.IsNullOrEmpty(configuration.TestResults) && File.Exists(configuration.TestResults))
            {
                var success = _buildTestResultsPublisher.PublishTestResultsToBuild(
                    configuration.Collection, configuration.TestResults,
                    configuration.Project, configuration.BuildNumber,
                    configuration.BuildPlatform, configuration.BuildFlavor);

                if (!success)
                    return 1;
            }

            if (configuration.PublishTestRun)
            {
                var success = _testRunPublisher.PublishTestRun(configuration);

                if (!success)
                    return 1;
            }

            Console.WriteLine("Build added.");
            return 0;
        }
    }
}