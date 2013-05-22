using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace TfsBuildResultPublisher
{
    public interface ITestRunPublisher
    {
        bool PublishTestRun(Configuration configuration);
    }

    public class TestRunPublisher : ITestRunPublisher
    {
        public bool PublishTestRun(Configuration configuration)
        {
            if (configuration.TestSuiteId == null)
                throw new ArgumentException("/testSuiteId must be specified when publishing test results");
            if (configuration.TestConfigId == null)
                throw new ArgumentException("/testConfigId must be specified when publishing test results");

            string trxPath = Path.Combine(
                Path.GetDirectoryName(configuration.TestResults),
                Path.GetFileNameWithoutExtension(configuration.TestResults) + "_TestRunPublish.trx");

            Console.WriteLine("Taken copy of results file to update for publish ({0})", trxPath);
            File.Copy(configuration.TestResults, trxPath);

            if (configuration.FixTestIds)
                TrxFileCorrector.FixTestIdsInTrx(trxPath);

            var fixedFile = Regex.Replace(File.ReadAllText(trxPath), "TestRun id=\".{36}\"",
                                          string.Format("TestRun id=\"{0}\"", Guid.NewGuid()));
            File.WriteAllText(trxPath, fixedFile);

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
            var args = string.Format(argsFormat, configuration.TestSuiteId, configuration.TestConfigId, trxPath, configuration.Collection, configuration.Project, configuration.BuildNumber, configuration.BuildDefinition, configuration.TestRunResultOwner ?? Environment.UserName);

            //Optionally override title
            if (!string.IsNullOrEmpty(configuration.TestRunTitle))
                args += " /title:\"" + configuration.TestRunTitle + "\"";

            Console.WriteLine("Launching tcm.exe {0}", args);

            string stdOut;
            string stdErr;
            var processStartInfo = new ProcessStartInfo(tcm, args)
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };
            var process = Process.Start(processStartInfo);
            process.InputAndOutputToEnd(string.Empty, out stdOut, out stdErr);

            Console.Write(stdOut);
            Console.Write(stdErr);
            return process.ExitCode == 0;
        }
    }
}