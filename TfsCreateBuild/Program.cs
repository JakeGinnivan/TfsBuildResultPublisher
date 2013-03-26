using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using NDesk.Options;

namespace TfsCreateBuild
{
    class Program
    {
        private static string _collection;
        private static string _project;
        private static string _buildDefinition;
        private static string _buildNumber;
        private static string _buildStatus;
        private static string _buildFlavor;
        private static string _buildPlatform;
        private static string _buildTarget;
        private static string _localPath;
        private static string _serverPath;
        private static string _dropPath;
        private static string _testResults;
        private static string _buildLog;
        private static bool _createBuildDefinitionIfNotExists;
        private static string _buildController;
        private static DateTime? _startTime;
        private static DateTime? _finishTime;
        private static bool _publishTestRun;
        private static int? _testSuiteId;
        private static int? _testConfigId;
        private static string _testRunTitle;
        private static string _testRunResultOwner;
        private static bool _fixTestIds;

        static void Main(string[] args)
        {
            var p = new OptionSet
                {
                    {"c|collection=", "The collection", v => _collection = v},
                    {"p|project=", "The team project", v => _project = v},
                    {"b|builddefinition=", "The build definition", v => _buildDefinition = v},
                    {"n|buildnumber=", "The build number to assign the build", v => _buildNumber = v},
                    {"s|status=", "Status of the build  (Succeeded, Failed, Stopped, PartiallySucceeded, default: Succeeded)", v => _buildStatus = v},
                    {"f|flavor=", "Flavor of the build (to track test results against, default: Debug)", v => _buildFlavor = v},
                    {"l|platform=", "Platform of the build (to track test results against, AnyCPU)", v => _buildPlatform = v},
                    {"t|target=", "Target of the build (shown on build report, default: default)", v => _buildTarget = v},
                    {"localpath=", "Local path of solution file. (default: Solution.sln)", v => _localPath = v},
                    {"serverpath=", "Version Control path for solution file. (e.g. $/Solution.sln)", v => _serverPath = v},
                    {"droplocation=", @"Location where builds are dropped (default: \\server\drops\)", v => _dropPath = v},
                    {"buildlog=", @"Location of build log file. (e.g. \\server\folder\build.log)", v => _buildLog = v },
                    {"startTime=", @"The Start Time of the build. (default: now)", v => _startTime = DateTime.Parse(v) },
                    {"finishTime=", @"The Finish Time of the build. (default: now)", v => _finishTime = DateTime.Parse(v) },
                    {"testResults=", @"Test results file to publish (*.trx, requires MSTest installed)", v => _testResults = v},
                    {"create", "Should the build definition be created if it does not exist", v => _createBuildDefinitionIfNotExists = (v != null)},
                    {"buildController=", @"The name of the build controller to use when creating the build definition (default, first controller)", v => _buildController = v},
                    {"publishTestRun", @"Creates a test run in Test Manager (requires tcm.exe installed)", v => _publishTestRun = (v != null)},
                    {"fixTestIds", @"If the .trx file comes from VSTest.Console.exe, the testId's will not be recognised by Test Runs (for associated automation)", v => _fixTestIds = (v != null)},
                    {"testSuiteId=", @"The Test Suite to publish the results of the test run to [tcm /suiteId]", v => _testSuiteId = int.Parse(v)},
                    {"testConfigid=", @"The Test Configuration to publish the results of the test run to [tcm /configId]", v => _testConfigId = int.Parse(v)},
                    {"testRunTitle=", @"The title of the test run [tcm /title]", v => _testRunTitle = v},
                    {"testRunResultOwner=", @"The result owner of the test run [tcm /resultOwner]", v => _testRunResultOwner = v},
                };

            try
            {
                p.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(p);
                return;
            }
            if (string.IsNullOrEmpty(_collection) || string.IsNullOrEmpty(_project) || string.IsNullOrEmpty(_buildDefinition) || string.IsNullOrEmpty(_buildNumber))
            {
                ShowHelp(p);
                return;
            }

            AddBuild(_collection, _project, _buildDefinition, _buildNumber);
            Console.WriteLine("Build added.");
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Creates a build in TFS");
            Console.WriteLine("Usage: TfsCreateBuild.exe /collection:(http://tfsserver:8080/tfs/MyCollection) /project:(TeamProject) /builddefinition:(MyBuild) /buildnumber:(MyApplication_Daily_1.0)");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        static void AddBuild(string serverName, string teamProject, string buildDefinition, string buildNumber)
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
            var startTime = _startTime ?? DateTime.Now;
            var finishTime = _finishTime ?? DateTime.Now;
            var flavor = _buildFlavor ?? "Debug";
            var localPath = _localPath ?? "Solution.sln";
            var platform = _buildPlatform ?? "AnyCPU";
            var serverPath = _serverPath ?? "$/Solution.sln";
            var buildTarget = _buildTarget ?? "default";
            var buildProjectNode = buildDetail.Information.AddBuildProjectNode(finishTime, flavor, localPath, platform, serverPath, startTime, buildTarget);

            if (!string.IsNullOrEmpty(_dropPath))
                buildDetail.DropLocation = _dropPath;

            if (!string.IsNullOrEmpty(_buildLog))
                buildDetail.LogLocation = _buildLog;

            buildProjectNode.Save();

            // Complete the build by setting the status to succeeded
            var buildStatus = (BuildStatus)Enum.Parse(typeof(BuildStatus), _buildStatus);
            buildDetail.FinalizeStatus(buildStatus);

            if (!string.IsNullOrEmpty(_testResults) && File.Exists(_testResults))
                PublishTestResults();

            if (_publishTestRun)
                PublishTestRun();
        }

        private static void PublishTestRun()
        {
            if (_testSuiteId == null)
                throw new ArgumentException("/testSuiteId must be specified when publishing test results");
            if (_testConfigId == null)
                throw new ArgumentException("/testConfigId must be specified when publishing test results");

            if (_fixTestIds)
                FixTestIdsInTrx();

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
            var args = string.Format(argsFormat, _collection, _testSuiteId, _testConfigId, _testResults, _project,
                                     _buildNumber, _buildDefinition, _testRunResultOwner ?? Environment.UserName);

            //Optionally override title
            if (!string.IsNullOrEmpty(_testRunTitle))
                args += " /title " + _testRunTitle;

            Process.Start(tcm, args);
        }

        private static void FixTestIdsInTrx()
        {
            const string ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var trx = File.ReadAllText(_testResults);

            var replaceList = new Dictionary<Guid, Guid>();

            var doc = XDocument.Load(_testResults);
            var unitTests = doc.Descendants(XName.Get("TestRun", ns)).Descendants(XName.Get("TestDefinitions", ns)).Single().Descendants(XName.Get("UnitTest", ns));
            foreach (var unitTest in unitTests)
            {
                var testMethod = unitTest.Descendants(XName.Get("TestMethod", ns)).Single();
                var className = testMethod.Attribute("className");
                var name = testMethod.Attribute("name");
                var id = new Guid(unitTest.Attribute("id").Value);
                replaceList.Add(id, CalcProperGuid(className.Value + "." + name.Value));
            }

            trx = replaceList.Aggregate(trx, (current, replacement) => current.Replace(replacement.Key.ToString(), replacement.Value.ToString()));

            File.WriteAllText(_testResults, trx);
        }

        static Guid CalcProperGuid(string testName)
        {
            var crypto = new SHA1CryptoServiceProvider();
            var bytes = new byte[16];
            Array.Copy(crypto.ComputeHash(Encoding.Unicode.GetBytes(testName)), bytes, bytes.Length);
            return new Guid(bytes);
        }

        private static void PublishTestResults()
        {
            var paths = new[]
                {
                    @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe",
                    @"C:\Program Files\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe"
                };
            var msTest = File.Exists(paths[0]) ? paths[0] : paths[1];
            const string argsFormat = "/publish:\"{0}\" /publishresultsfile:\"{1}\" /teamproject:\"{2}\" /publishbuild:\"{3}\" /platform:\"{4}\" /flavor:\"{5}\"";
            var args = string.Format(argsFormat, _collection, _testResults, _project, _buildNumber, _buildPlatform, _buildFlavor);

            Process.Start(msTest, args);
        }

        private static IBuildDefinition CreateOrGetBuildDefinition(string teamProject, string buildDefinition, IBuildServer buildServer)
        {
            try
            {
                return buildServer.GetBuildDefinition(teamProject, buildDefinition);
            }
            catch (BuildDefinitionNotFoundException)
            {
                if (!_createBuildDefinitionIfNotExists)
                    throw;
            }

            return CreateBuildDefinition(teamProject, buildDefinition, buildServer);
        }

        private static IBuildDefinition CreateBuildDefinition(string teamProject, string buildDefinition, IBuildServer buildServer)
        {
            var controller = GetBuildController(buildServer);

            // Get the Upgrade template to use as the process template
            var processTemplate = buildServer.QueryProcessTemplates(teamProject, new[] { ProcessTemplateType.Upgrade })[0];

            var definition = buildServer.CreateBuildDefinition(teamProject);
            definition.Name = buildDefinition;
            definition.ContinuousIntegrationType = ContinuousIntegrationType.None;
            definition.BuildController = controller;
            definition.DefaultDropLocation = string.IsNullOrEmpty(_dropPath) ? @"\\server\drops\" : _dropPath;
            definition.Description = "Fake build definition used to create fake builds.";
            definition.QueueStatus = DefinitionQueueStatus.Enabled;
            definition.Workspace.AddMapping("$/", "c:\\fake", WorkspaceMappingType.Map);
            definition.Process = processTemplate;
            definition.Save();

            return definition;
        }

        private static IBuildController GetBuildController(IBuildServer buildServer)
        {
            if (string.IsNullOrEmpty(_buildController))
                return buildServer.QueryBuildControllers(false).First();

            return buildServer.GetBuildController(_buildController);
        }
    }
}
