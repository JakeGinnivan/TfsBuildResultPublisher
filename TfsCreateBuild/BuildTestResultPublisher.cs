using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TfsBuildResultPublisher
{
    public class BuildTestResultPublisher : IBuildTestResultPublisher
    {
        public bool PublishTestResultsToBuild(string collection, string testResultsFile, string project, string buildNumber, string buildPlatform, string buildFlavour)
        {
            var paths = new[]
                {
                    @"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\MSTest.exe",
                    @"C:\Program Files\Microsoft Visual Studio 14.0\Common7\IDE\MSTest.exe",
                    @"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe",
                    @"C:\Program Files\Microsoft Visual Studio 11.0\Common7\IDE\MSTest.exe"
                };
            var msTest = paths.FirstOrDefault(File.Exists);
            const string argsFormat = "/publish:\"{0}\" /publishresultsfile:\"{1}\" /teamproject:\"{2}\" /publishbuild:\"{3}\" /platform:\"{4}\" /flavor:\"{5}\"";
            var args = string.Format(argsFormat, collection, testResultsFile, project, buildNumber, buildPlatform, buildFlavour);
            string stdOut;
            string stdErr;
            var processStartInfo = new ProcessStartInfo(msTest, args)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            Console.WriteLine("Launching mstest.exe {0}", args);

            var process = Process.Start(processStartInfo);
            process.InputAndOutputToEnd(string.Empty, out stdOut, out stdErr);
            Console.Write(stdOut);
            Console.Write(stdErr);

            return process.ExitCode == 0;
        }
    }
}