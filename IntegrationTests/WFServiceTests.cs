using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Fonlow.Testing;

using BasicWFServiceClientApi;

namespace IntegrationTests
{
    [Collection(TestConstants.IisExpressAndInit)]
    public class WFServiceTests
    {
        const string realWorldEndpoint = "DefaultBinding_Workflow";

        [Fact]
        public void TestGetData()
        {
            using (var client = new WorkflowProxy(realWorldEndpoint))
            {
                Assert.Equal("abcaaa",  client.Instance.GetData("abc"));
            }

        }
    }
}
