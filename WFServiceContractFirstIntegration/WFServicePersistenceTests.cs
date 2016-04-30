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
    public class WFServicePersistenceTests
    {
        const string hostBaseAddress = "http://localhost:2327/BookService.xamlx";
        [Fact]
        public void TestBuyBook()
        {
            IisExpressAgent agent = new IisExpressAgent();
            const string bookName = "Alice in Wonderland";
            var customerId = Guid.NewGuid();
            agent.Start();
            var client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            client.Buy(customerId, bookName);
            agent.Stop();
            System.Threading.Thread.Sleep(6000);//WF runtime seems to be taking long time to persist data. And there may be delayed write, so closing the service may force writing data to DB.
            // The wait time has better to be hostLockRenewalPeriod + runnableInstancesDetectionPeriod + 1 second.
            agent.Start();
            client = ChannelFactory<IBookService>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(hostBaseAddress));
            var checkOutBookName = client.Checkout(customerId);
            Assert.Equal(bookName, checkOutBookName);
            client.Pay(customerId, "Visa card");
            agent.Stop();
        }

    }
}
