using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace TfsCreateBuild
{
    class TrxIdCorrector
    {
        public static void FixTestIdsInTrx(string testResults)
        {
            const string ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var trx = File.ReadAllText(testResults);

            var replaceList = new Dictionary<Guid, Guid>();

            var doc = XDocument.Load(testResults);
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

            File.WriteAllText(testResults, trx);
        }

        static Guid CalcProperGuid(string testName)
        {
            var crypto = new SHA1CryptoServiceProvider();
            var bytes = new byte[16];
            Array.Copy(crypto.ComputeHash(Encoding.Unicode.GetBytes(testName)), bytes, bytes.Length);
            return new Guid(bytes);
        }
    }
}