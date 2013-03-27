using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using NDesk.Options;

namespace TfsCreateBuild
{
    public class BuildCreator
    {
        private readonly TeamCityBuildInfoFetcher _teamCityBuildInfoFetcher = new TeamCityBuildInfoFetcher();
        private readonly Configuration _configuration = new Configuration();

        public int CreateBuild(string[] args)
        {
            var p = new OptionSet
                {
                    {"c|collection=", "The collection", v => _configuration.Collection = v},
                    {"p|project=", "The team project", v => _configuration.Project = v},
                    {"b|builddefinition=", "The build definition", v => _configuration.BuildDefinition = v},
                    {"n|buildnumber=", "The build number to assign the build", v => _configuration.BuildNumber = v},
                    {"teamcityBuildId=", "The TeamCity build id to take startTime, endTime, buildnumber from", v => _configuration.TeamCityBuildId = v},
                    {"teamcityServer=", "Url of your teamcity server", v => _configuration.TeamCityServerAddress = v},
                    {"s|status=", "Status of the build  (Succeeded, Failed, Stopped, PartiallySucceeded, default: Succeeded)", v => _configuration.BuildStatus = v},
                    {"f|flavor=", "Flavor of the build (to track test results against, default: Debug)", v => _configuration.BuildFlavor = v},
                    {"l|platform=", "Platform of the build (to track test results against, AnyCPU)", v => _configuration.BuildPlatform = v},
                    {"t|target=", "Target of the build (shown on build report, default: default)", v => _configuration.BuildTarget = v},
                    {"localpath=", "Local path of solution file. (default: Solution.sln)", v => _configuration.LocalPath = v},
                    {"serverpath=", "Version Control path for solution file. (e.g. $/Solution.sln)", v => _configuration.ServerPath = v},
                    {"droplocation=", @"Location where builds are dropped (default: \\server\drops\)", v => _configuration.DropPath = v},
                    {"buildlog=", @"Location of build log file. (e.g. \\server\folder\build.log)", v => _configuration.BuildLog = v },
                    {"startTime=", @"The Start Time of the build. (default: now)", v => _configuration.StartTime = DateTime.Parse(v) },
                    {"finishTime=", @"The Finish Time of the build. (default: now)", v => _configuration.FinishTime = DateTime.Parse(v) },
                    {"testResults=", @"Test results file to publish (*.trx, requires MSTest installed)", v => _configuration.TestResults = v},
                    {"create", "Should the build definition be created if it does not exist", v => _configuration.CreateBuildDefinitionIfNotExists = (v != null)},
                    {"buildController=", @"The name of the build controller to use when creating the build definition (default, first controller)", v => _configuration.BuildController = v},
                    {"publishTestRun", @"Creates a test run in Test Manager (requires tcm.exe installed)", v => _configuration.PublishTestRun1 = (v != null)},
                    {"fixTestIds", @"If the .trx file comes from VSTest.Console.exe, the testId's will not be recognised by Test Runs (for associated automation)", v => _configuration.FixTestIds = (v != null)},
                    {"testSuiteId=", @"The Test Suite to publish the results of the test run to [tcm /suiteId]", v => _configuration.TestSuiteId = int.Parse(v)},
                    {"testConfigid=", @"The Test Configuration to publish the results of the test run to [tcm /configId]", v => _configuration.TestConfigId = int.Parse(v)},
                    {"testRunTitle=", @"The title of the test run [tcm /title]", v => _configuration.TestRunTitle = v},
                    {"testRunResultOwner=", @"The result owner of the test run [tcm /resultOwner]", v => _configuration.TestRunResultOwner = v},
                };

            try
            {
                p.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(p);
                return 1;
            }

            if (!string.IsNullOrEmpty(_configuration.TeamCityBuildId))
                _teamCityBuildInfoFetcher.UpdateConfigurationFromTeamCityBuild(_configuration);

            if (string.IsNullOrEmpty(_configuration.Collection) || string.IsNullOrEmpty(_configuration.Project) || string.IsNullOrEmpty(_configuration.BuildDefinition) || string.IsNullOrEmpty(_configuration.BuildNumber))
            {
                ShowHelp(p);
                return 1;
            }

            AddBuild(_configuration.Collection, _configuration.Project, _configuration.BuildDefinition, _configuration.BuildNumber, _configuration);
            Console.WriteLine("Build added.");
            return 0;
        }

        void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Creates a build in TFS");
            Console.WriteLine("Usage: TfsCreateBuild.exe /collection:(http://tfsserver:8080/tfs/MyCollection) /project:(TeamProject) /builddefinition:(MyBuild) /buildnumber:(MyApplication_Daily_1.0)");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        void AddBuild(string serverName, string teamProject, string buildDefinition, string buildNumber, Configuration configuration)
        {
            // Get the TeamFoundation Server
            var collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(serverName));

            // Get the Build Server
            var buildServer = (IBuildServer)collection.GetService(typeof(IBuildServer));

            // Create a fake definition
            var definition = CreateOrGetBuildDefinition(teamProject, buildDefinition, buildServer);

            // Create the build detail object
            var buildDetail = definition.CreateManualBuild(buildNumber);

            // Create platform/flavor information against which test results can be published
            var startTime = configuration.StartTime ?? DateTime.Now;
            var finishTime = configuration.FinishTime ?? DateTime.Now;
            var flavor = configuration.BuildFlavor ?? "Debug";
            var localPath = configuration.LocalPath ?? "Solution.sln";
            var platform = configuration.BuildPlatform ?? "AnyCPU";
            var serverPath = configuration.ServerPath ?? "$/Solution.sln";
            var buildTarget = configuration.BuildTarget ?? "default";
            var buildProjectNode = buildDetail.Information.AddBuildProjectNode(finishTime, flavor, localPath, platform, serverPath, startTime, buildTarget);

            if (!string.IsNullOrEmpty(configuration.DropPath))
                buildDetail.DropLocation = configuration.DropPath;

            if (!string.IsNullOrEmpty(configuration.BuildLog))
                buildDetail.LogLocation = configuration.BuildLog;

            buildProjectNode.Save();

            // Complete the build by setting the status to succeeded
            var buildStatus = (BuildStatus)Enum.Parse(typeof(BuildStatus), configuration.BuildStatus);
            buildDetail.FinalizeStatus(buildStatus);

            if (!string.IsNullOrEmpty(configuration.TestResults) && File.Exists(configuration.TestResults))
                PublishTestResults();

            if (configuration.PublishTestRun1)
                PublishTestRun();
        }

        private void PublishTestRun()
        {
            if (_configuration.TestSuiteId == null)
                throw new ArgumentException("/testSuiteId must be specified when publishing test results");
            if (_configuration.TestConfigId == null)
                throw new ArgumentException("/testConfigId must be specified when publishing test results");

            if (_configuration.FixTestIds)
                TrxIdCorrector.FixTestIdsInTrx(_configuration.TestResults);

            var paths = new[]
                {
                    @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\TCM.exe",
                    @"C:\Program Files\Microsoft Visual Studio 11.0\Common7\IDE\TCM.exe"
                };
            var tcm = File.Exists(paths[0]) ? paths[0] : paths[1];

            const string argsFormat = "run /publish /suiteid:{0} /configid:{1} " +
                                      "/resultsfile:\"{2}\" " +
                                      "/collection:\"{3}\" /teamproject:\"{4}\" " +
                                      "/build:\"{5}\" /builddefinition:\"{6}\" /resultowner:\"{7}\"";
            var args = string.Format(argsFormat, _configuration.Collection, _configuration.TestSuiteId, _configuration.TestConfigId, _configuration.TestResults, _configuration.Project, _configuration.BuildNumber, _configuration.BuildDefinition, _configuration.TestRunResultOwner ?? Environment.UserName);

            //Optionally override title
            if (!string.IsNullOrEmpty(_configuration.TestRunTitle))
                args += " /title " + _configuration.TestRunTitle;

            Process.Start(tcm, args);
        }

        private void PublishTestResults()
        {
            var paths = new[]
                {
                    @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe",
                    @"C:\Program Files\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe"
                };
            var msTest = File.Exists(paths[0]) ? paths[0] : paths[1];
            const string argsFormat = "/publish:\"{0}\" /publishresultsfile:\"{1}\" /teamproject:\"{2}\" /publishbuild:\"{3}\" /platform:\"{4}\" /flavor:\"{5}\"";
            var args = string.Format(argsFormat, _configuration.Collection, _configuration.TestResults, _configuration.Project, _configuration.BuildNumber, _configuration.BuildPlatform, _configuration.BuildFlavor);

            Process.Start(msTest, args);
        }

        private IBuildDefinition CreateOrGetBuildDefinition(string teamProject, string buildDefinition, IBuildServer buildServer)
        {
            try
            {
                return buildServer.GetBuildDefinition(teamProject, buildDefinition);
            }
            catch (BuildDefinitionNotFoundException)
            {
                if (!_configuration.CreateBuildDefinitionIfNotExists)
                    throw;
            }

            return CreateBuildDefinition(teamProject, buildDefinition, buildServer);
        }

        private IBuildDefinition CreateBuildDefinition(string teamProject, string buildDefinition, IBuildServer buildServer)
        {
            var controller = GetBuildController(buildServer);

            // Get the Upgrade template to use as the process template
            var processTemplate = buildServer.QueryProcessTemplates(teamProject, new[] { ProcessTemplateType.Upgrade })[0];

            var definition = buildServer.CreateBuildDefinition(teamProject);
            definition.Name = buildDefinition;
            definition.ContinuousIntegrationType = ContinuousIntegrationType.None;
            definition.BuildController = controller;
            definition.DefaultDropLocation = string.IsNullOrEmpty(_configuration.DropPath) ? @"\\server\drops\" : _configuration.DropPath;
            definition.Description = "Fake build definition used to create fake builds.";
            definition.QueueStatus = DefinitionQueueStatus.Enabled;
            definition.Workspace.AddMapping("$/", "c:\\fake", WorkspaceMappingType.Map);
            definition.Process = processTemplate;
            definition.Save();

            return definition;
        }

        private IBuildController GetBuildController(IBuildServer buildServer)
        {
            if (string.IsNullOrEmpty(_configuration.BuildController))
                return buildServer.QueryBuildControllers(false).First();

            return buildServer.GetBuildController(_configuration.BuildController);
        } 
    }
}