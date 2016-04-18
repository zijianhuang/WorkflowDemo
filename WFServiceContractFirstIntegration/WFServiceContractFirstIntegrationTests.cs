using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Fonlow.Testing;
using Fonlow.WorkflowDemo.Contracts;
using System.Diagnostics;
using System.ServiceModel;


namespace WFServiceContractFirstIntegration
{
    [Collection(TestConstants.IisExpressAndInit)]
    public class WFServiceContractFirstIntegrationTests
    {
        const string hostBaseAddress = "http://localhost:2327/BookService.xamlx";
        [Fact]
        public void TestBuyBook()
        {
            var client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            const string bookName = "Alice in Wonderland";
            var customerId = Guid.NewGuid();
            client.Buy(customerId, bookName);
            var checkOutBookName = client.Checkout(customerId);
            Assert.Equal(bookName, checkOutBookName);
            client.Pay(customerId, "Visa card");
        }

        [Fact]
        public void TestBuyBookInWrongOrderThrows()
        {
            var client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            const string bookName = "Alice in Wonderland";
            var customerId = Guid.NewGuid();
            client.Buy(customerId, bookName);
            var ex = Assert.Throws<FaultException>(
                () => client.Pay(customerId, "Visa card"));
            Assert.Contains("correct order", ex.ToString());
        }

        [Fact]
        public void TestNonExistingSessionThrows()
        {
            var client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            var customerId = Guid.NewGuid();
            var ex = Assert.Throws<FaultException>(
                () => { client.Checkout(customerId); });
            Assert.Contains("InstancePersistenceCommand", ex.ToString());
        }


    }
}
