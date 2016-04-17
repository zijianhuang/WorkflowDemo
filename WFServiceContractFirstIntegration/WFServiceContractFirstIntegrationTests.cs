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
            // Create service host.
            // Create a client that sends a message to create an instance of the workflow.
            var client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            const string bookName = "Alice in Wonderland";
            client.Buy(1234, bookName);
            var checkOutBookName = client.Checkout(1234);
            Assert.Equal(bookName, checkOutBookName);
            client.Pay(1234, "Visa card");
        }

        [Fact]
        public void TestBuyBookInWrongOrder()
        {
            // Create service host.
            // Create a client that sends a message to create an instance of the workflow.
            var client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            const string bookName = "Alice in Wonderland";
            client.Buy(2222, bookName);
            var ex = Assert.Throws<FaultException>(
                () => client.Pay(2222, "Visa card"));
            Assert.Contains("correct order", ex.ToString());
        }

        [Fact]
        public void TestBuyBookButCheckOutFirst()
        {
            // Create service host.
            // Create a client that sends a message to create an instance of the workflow.
            var client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            var ex = Assert.Throws<FaultException>(
                () => { client.Checkout(2222); });
            Assert.Contains("correct order", ex.ToString());
        }


    }
}
