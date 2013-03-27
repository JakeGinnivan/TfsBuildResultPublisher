TfsCreateBuild
==============

In TFS it's possible to create "faked" builds, without actually running them. You can read about it in this [blog post](http://blogs.msdn.com/b/jpricket/archive/2010/02/23/creating-fake-builds-in-tfs-build-2010.aspx) by [Jason Prickett](http://social.msdn.microsoft.com/profile/jason%20prickett%20-%20msft/).

Main scenarios are:

 - Using a different build tool than TFS Build 
(by creating faked builds will let you use some of the great TFS features).
 - Creating sample data to show off TFS functionality
You can create "faked" builds on the command line using the following tool.

## Syntax/Usage:

    TfsCreateBuild.exe /collection:http://tfsserver:8080/tfs/MyCollection 
                       /project:TeamProject 
                       /builddefinition:"Daily Build"
                       /buildnumber:"MyApplication_Daily_1.0"
                       
    Options:
    -c, --collection=VALUE           The collection
    -p, --project=VALUE              The team project
    -b, --builddefinition=VALUE      The build definition
    -n, --buildnumber=VALUE          The build number to assign the build
    -s, --status=VALUE               Status of the build  (Succeeded, Failed, Stopped, PartiallySucceeded, default: Succeeded)
    -f, --flavor=VALUE               Flavor of the build (to track test results against, default: Debug)
    -l, --platform=VALUE             Platform of the build (to track test results against, AnyCPU)
    -t, --target=VALUE               Target of the build (shown on build report, default: default)
        --localpath=VALUE            Local path of solution file. (default: Solution.sln)
        --serverpath=VALUE           Version Control path for solution file. (e.g. $/Solution.sln)
        --droplocation=VALUE         Location where builds are dropped (default: \\server\drops\)
        --buildlog=VALUE             Location of build log file. (e.g. \\server\folder\build.log)
        --startTime=VALUE            The Start Time of the build. (default: now)
        --finishTime=VALUE           The Finish Time of the build. (default: now)
        --testResults=VALUE          Test results file to publish (*.trx, requires MSTest installed)
        --create                     Should the build definition be created if it does not exist
        --buildController=VALUE      The name of the build controller to use when creating the build definition (default, first controller)
        --publishTestRun             Creates a test run in Test Manager (requires tcm.exe installed)
        --fixTestIds                 If the .trx file comes from VSTest.Console.exe, the testId's will not be recognised by Test Runs (for associated automation)
        --testSuiteId=VALUE          The Test Suite to publish the results of the test run to [tcm /suiteId]
        --testConfigid=VALUE         The Test Configuration to publish the results of the test run to [tcm /configId]
        --testRunTitle=VALUE         The title of the test run [tcm /title]
        --testRunResultOwner=VALUE   The result owner of the test run [tcm /resultOwner]

## References
**Original Blog post:** [http://blogs.msdn.com/b/jpricket/archive/2010/02/23/creating-fake-builds-in-tfs-build-2010.aspx](http://blogs.msdn.com/b/jpricket/archive/2010/02/23/creating-fake-builds-in-tfs-build-2010.aspx)  
Inspiration for creating a 2012 command line tool: [http://msmvps.com/blogs/vstsblog/archive/2011/04/26/creating-fake-builds-in-tfs-build-2010-using-the-command-line.aspx](http://msmvps.com/blogs/vstsblog/archive/2011/04/26/creating-fake-builds-in-tfs-build-2010-using-the-command-line.aspx)
