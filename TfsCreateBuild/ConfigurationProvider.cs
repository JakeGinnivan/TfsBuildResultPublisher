using System;
using NDesk.Options;

namespace TfsCreateBuild
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private readonly ITeamCityBuildInfoFetch _teamCityBuildInfoFetcher;

        public ConfigurationProvider(ITeamCityBuildInfoFetch teamCityBuildInfoFetcher)
        {
            _teamCityBuildInfoFetcher = teamCityBuildInfoFetcher;
        }

        public bool TryProvide(string[] args, out Configuration configuration)
        {
            var localConfiguration = new Configuration();

            var p = new OptionSet
                {
                    {"c|collection=", "The collection", v => localConfiguration.Collection = v},
                    {"p|project=", "The team project", v => localConfiguration.Project = v},
                    {"b|builddefinition=", "The build definition", v => localConfiguration.BuildDefinition = v},
                    {"n|buildnumber=", "The build number to assign the build", v => localConfiguration.BuildNumber = v},
                    {"teamcityBuildId=", "The TeamCity build id to take startTime, endTime, buildnumber from", v => localConfiguration.TeamCityBuildId = v},
                    {"teamcityServer=", "Url of your teamcity server", v => localConfiguration.TeamCityServerAddress = v},
                    {"teamcityUserId=", "Username to connect with (if in teamcity build use system.teamcity.auth.userId)", v => localConfiguration.TeamCityUserId = v},
                    {"teamcityPassword=", "Password to connect with (if in teamcity build use system.teamcity.auth.password)", v => localConfiguration.TeamCityPassword = v},
                    {"s|status=", "Status of the build  (Succeeded, Failed, Stopped, PartiallySucceeded, default: Succeeded)", v => localConfiguration.BuildStatus = v},
                    {"f|flavor=", "Flavor of the build (to track test results against, default: Debug)", v => localConfiguration.BuildFlavor = v},
                    {"l|platform=", "Platform of the build (to track test results against, AnyCPU)", v => localConfiguration.BuildPlatform = v},
                    {"t|target=", "Target of the build (shown on build report, default: default)", v => localConfiguration.BuildTarget = v},
                    {"localpath=", "Local path of solution file. (default: Solution.sln)", v => localConfiguration.LocalPath = v},
                    {"serverpath=", "Version Control path for solution file. (e.g. $/Solution.sln)", v => localConfiguration.ServerPath = v},
                    {"droplocation=", @"Location where builds are dropped (default: \\server\drops\)", v => localConfiguration.DropPath = v},
                    {"buildlog=", @"Location of build log file. (e.g. \\server\folder\build.log)", v => localConfiguration.BuildLog = v },
                    {"testResults=", @"Test results file to publish (*.trx, requires MSTest installed)", v => localConfiguration.TestResults = v},
                    {"create", "Should the build definition be created if it does not exist", v => localConfiguration.CreateBuildDefinitionIfNotExists = (v != null)},
                    {"trigger", "Instead of creating a manual build, we should trigger the build", v=>localConfiguration.TriggerBuild = (v != null)},
                    {"buildController=", @"The name of the build controller to use when creating the build definition (default, first controller)", v => localConfiguration.BuildController = v},
                    {"publishTestRun", @"Creates a test run in Test Manager (requires tcm.exe installed)", v => localConfiguration.PublishTestRun = (v != null)},
                    {"fixTestIds", @"If the .trx file comes from VSTest.Console.exe, the testId's will not be recognised by Test Runs (for associated automation)", v => localConfiguration.FixTestIds = (v != null)},
                    {"testSuiteId=", @"The Test Suite to publish the results of the test run to [tcm /suiteId]", v => localConfiguration.TestSuiteId = int.Parse(v)},
                    {"testConfigid=", @"The Test Configuration to publish the results of the test run to [tcm /configId]", v => localConfiguration.TestConfigId = int.Parse(v)},
                    {"testRunTitle=", @"The title of the test run [tcm /title]", v => localConfiguration.TestRunTitle = v},
                    {"testRunResultOwner=", @"The result owner of the test run [tcm /resultOwner]", v => localConfiguration.TestRunResultOwner = v},
                };

            try
            {
                p.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(p);
                configuration = null;
                return false;
            }

            if (!string.IsNullOrEmpty(localConfiguration.TeamCityBuildId))
                _teamCityBuildInfoFetcher.UpdateConfigurationFromTeamCityBuild(localConfiguration);

            if (string.IsNullOrEmpty(localConfiguration.Collection) ||
                string.IsNullOrEmpty(localConfiguration.Project) ||
                string.IsNullOrEmpty(localConfiguration.BuildDefinition) || 
                (string.IsNullOrEmpty(localConfiguration.BuildNumber) && !localConfiguration.TriggerBuild))
            {
                ShowMissingArgsIfNeeded(localConfiguration);
                ShowHelp(p);
                configuration = null;
                return false;
            }

            ApplyDefaults(localConfiguration);
            configuration = localConfiguration;
            return true;
        }

        private void ApplyDefaults(Configuration configuration)
        {
            configuration.BuildFlavor = configuration.BuildFlavor ?? "Debug";
            configuration.LocalPath = configuration.LocalPath ?? "Solution.sln";
            configuration.BuildPlatform = configuration.BuildPlatform ?? "AnyCPU";
            configuration.ServerPath = configuration.ServerPath ?? "$/Solution.sln";
            configuration.BuildTarget = configuration.BuildTarget ?? "default";
            configuration.DropPath = configuration.DropPath ?? @"\\server\drops\";
            configuration.BuildStatus = configuration.BuildStatus ?? "Succeeded";
        }

        private void ShowMissingArgsIfNeeded(Configuration localConfiguration)
        {
            if (string.IsNullOrEmpty(localConfiguration.Collection) &&
                string.IsNullOrEmpty(localConfiguration.Project) &&
                string.IsNullOrEmpty(localConfiguration.BuildDefinition) &&
                string.IsNullOrEmpty(localConfiguration.BuildNumber))
                return;

            if (string.IsNullOrEmpty(localConfiguration.Collection))
                Console.WriteLine("collection not specified");
            if (string.IsNullOrEmpty(localConfiguration.Project))
                Console.WriteLine("project not specified");
            if (string.IsNullOrEmpty(localConfiguration.BuildDefinition))
                Console.WriteLine("buildDefinition not specified");
            if (string.IsNullOrEmpty(localConfiguration.BuildNumber) && !localConfiguration.TriggerBuild)
                Console.WriteLine("buildNumber not specified");
        }

        void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Creates a build in TFS");
            Console.WriteLine("Usage: TfsCreateBuild.exe /collection:(http://tfsserver:8080/tfs/MyCollection) /project:(TeamProject) /builddefinition:(MyBuild) /buildnumber:(MyApplication_Daily_1.0)");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}