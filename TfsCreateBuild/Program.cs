using System;
using System.Linq;
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

        static void Main(string[] args)
        {
            var p = new OptionSet
                {
                    {"c|collection=", "The collection", v => _collection = v},
                    {"p|project=", "The team project", v => _project = v},
                    {"b|builddefinition=", "The build definition", v => _buildDefinition = v},
                    {"n|buildnumber=", "The build number to assign the build", v => _buildNumber = v},
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

            //TODO Optional parameters:
            //status:<value> Status of the build (Succeeded, Failed, Stopped, PartiallySucceeded)
            //flavor:<name> Flavor of the build (to track test results against, default: Debug)
            //platform:<name> Platform of the build (to track test results against, default: x86)
            //target:<name> Target of the build (shown on build report, default: default)
            //localpath:<path> Local path of solution file. (e.g. Solution.sln)
            //serverpath:<path> Version Control path for solution file. (e.g. $/proj/src/app.sln)
            //compileerrors:# Number of compilation errors.
            //compilewarnings:# Number of compilation warnings.
            //analysiserrors:# Number of static code analysis errors.
            //analysiswarnings:# Number of static code analysis warnings.
            //droplocation:<path> Location where builds are dropped.
            //buildlog:<path> Location of build log file. (e.g. \server\folder\build.log)

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
            var definition = AddDefinition(buildServer, teamProject, buildDefinition);

            // Create the build detail object
            var buildDetail = definition.CreateManualBuild(buildNumber);

            // Create platform/flavor information against which test 
            // results can be published
            var buildProjectNode = buildDetail.Information.AddBuildProjectNode(DateTime.Now, "Debug", "Dummy.sln", "x86", @"$/Dummy.sln", DateTime.Now, "default");
            buildProjectNode.Save();

            // Complete the build by setting the status to succeeded. This call also
            // sets the finish time of the build.
            buildDetail.FinalizeStatus(BuildStatus.Succeeded);
        }

        private static IBuildDefinition AddDefinition(IBuildServer buildServer, string teamProject, string definitionName)
        {
            try
            {
                // See if it already exists, if so return it
                return buildServer.GetBuildDefinition(teamProject, definitionName);
            }
            catch (BuildDefinitionNotFoundException)
            {
                // no definition was found so continue on and try to create one
            }

            // Use the first build controller as the controller for these builds
            var controller = AddBuildController(buildServer);

            // Get the Upgrade template to use as the process template
            var processTemplate = buildServer.QueryProcessTemplates(teamProject, new[] { ProcessTemplateType.Upgrade })[0];

            var definition = buildServer.CreateBuildDefinition(teamProject);
            definition.Name = definitionName;
            definition.ContinuousIntegrationType = ContinuousIntegrationType.None;
            definition.BuildController = controller;
            definition.DefaultDropLocation = @"\\MySharedMachine\drops\";
            definition.Description = "Fake build definition used to create fake builds.";
            definition.Enabled = false;
            definition.Workspace.AddMapping("$/", "c:\\fake", WorkspaceMappingType.Map);
            definition.Process = processTemplate;
            definition.Save();

            return definition;
        }

        private static IBuildController AddBuildController(IBuildServer buildServer)
        {
            return buildServer.QueryBuildControllers().First();
        }
    }
}
