using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace TfsBuildResultPublisher
{
    class TrxFileCorrector
    {
        const string Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        public static void FixTestIdsInTrx(string testResults)
        {
            var trx = File.ReadAllText(testResults);

            var replaceList = new Dictionary<Guid, Guid>();
            var storageReplace = new Dictionary<string, string>();

            var doc = XDocument.Load(testResults);
            FixEndDateBeforeStartDate(doc);
            var unitTests = doc.Descendants(XName.Get("TestRun", Ns)).Descendants(XName.Get("TestDefinitions", Ns)).Single().Descendants(XName.Get("UnitTest", Ns));
            foreach (var unitTest in unitTests)
            {
                //Storage attribute from VSTest.Console is fully qualified, i.e c:\foo\bar.dll, but tcm requires just bar.dll
                var testMethod = unitTest.Descendants(XName.Get("TestMethod", Ns)).Single();
                var storage = unitTest.Attribute("storage").Value;
                if (!storageReplace.ContainsKey(storage))
                    storageReplace.Add(storage, Path.GetFileName(storage));

                //VSTest.Console.exe generates random Guids for test Ids, we need to generate a guid based on the test name using the same
                // algorithm as MSTest
                var className = testMethod.Attribute("className");
                var name = testMethod.Attribute("name");
                var id = new Guid(unitTest.Attribute("id").Value);
                if (!replaceList.ContainsKey(id))
                    replaceList.Add(id, CalcProperGuid(className.Value + "." + name.Value));
            }

            trx = replaceList.Aggregate(trx, (current, replacement) => current.Replace(replacement.Key.ToString(), replacement.Value.ToString()));
            trx = storageReplace.Aggregate(trx, (current, replacement) => current.Replace(replacement.Key.ToString(), replacement.Value.ToString()));
            
            File.WriteAllText(testResults, trx);
        }

        private static void FixEndDateBeforeStartDate(XDocument doc)
        {
            var unitTests = doc.Descendants(XName.Get("TestRun", Ns)).Descendants(XName.Get("Results", Ns)).Single().Descendants(XName.Get("UnitTestResult", Ns));

            foreach (var unitTest in unitTests)
            {
                var endTimeValue = unitTest.Attribute("endTime").Value;
                var startTimeValue = unitTest.Attribute("startTime").Value;
                var startTime = DateTime.Parse(startTimeValue);
                var endTime = DateTime.Parse(endTimeValue);
                if (startTime > endTime)
                {
                    unitTest.SetAttributeValue("startTime", endTime.Subtract(TimeSpan.Parse(unitTest.Attribute("duration").Value)));
                }
            }
        }
        
        static Guid CalcProperGuid(string testName)
        {
            // Algorithm taken from http://blogs.msdn.com/b/gautamg/archive/2012/01/01/how-to-associate-automation-programmatically.aspx
            var crypto = new SHA1CryptoServiceProvider();
            var bytes = new byte[16];
            Array.Copy(crypto.ComputeHash(Encoding.Unicode.GetBytes(testName)), bytes, bytes.Length);
            return new Guid(bytes);
        }
    }
}