using System.Xml.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using TfsBuildResultPublisher.Tests.Properties;
using Xunit;

namespace TfsBuildResultPublisher.Tests
{
    [UseReporter(typeof(DiffReporter))]
    public class ApproveTrxFixes
    {
        [Fact]
        public void Approve()
        {
            var doc = XDocument.Parse(Resources.SourceTrx);

            var fixedTrx = TrxFileCorrector.FixTrx(doc);

            Approvals.Verify(fixedTrx);
        }
    }
}