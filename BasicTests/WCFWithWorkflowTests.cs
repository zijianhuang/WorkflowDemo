using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Fonlow.Activities;
using System.Activities;
using System.Activities.Expressions;
using System.Threading;
using System.Activities.DurableInstancing;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Activities;
using System.ServiceModel.Activities.Description;
using Fonlow.Activities.ServiceModel;
using System.ServiceModel.Description;
using System.Activities.Statements;

namespace BasicTests
{
    public class WCFWithWorkflowTests
    {
        readonly Uri baseUri = new Uri("net.tcp://localhost/");

        ServiceHost CreateHost()
        {
            var host = new ServiceHost(typeof(Fonlow.WorkflowDemo.Contracts.WakeupService), baseUri);
            host.AddServiceEndpoint("Fonlow.WorkflowDemo.Contracts.IWakeup", new NetTcpBinding(), "wakeup");

            ServiceDebugBehavior debug = host.Description.Behaviors.Find<ServiceDebugBehavior>();

            if (debug == null)
            {
                host.Description.Behaviors.Add(
                     new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
            }
            else
            {
                // make sure setting is turned ON
                if (!debug.IncludeExceptionDetailInFaults)
                {
                    debug.IncludeExceptionDetailInFaults = true;
                }
            }

            return host;
        }

        [Fact]
        public void TestWaitForSignalOrDelay()
        {
            Guid id;
            // Create service host.
            using (var host = CreateHost())
            {
                host.Open();
                Assert.Equal(CommunicationState.Opened, host.State);


                // Create a client that sends a message to create an instance of the workflow.
                var client = ChannelFactory<Fonlow.WorkflowDemo.Contracts.IWakeup>.CreateChannel(new NetTcpBinding(), new EndpointAddress(new Uri(baseUri, "wakeup")));
                id = client.Create("Service wakeup", TimeSpan.FromSeconds(100));
                Assert.NotEqual(Guid.Empty, id);

                Thread.Sleep(2000);//so the service may have time to persist.

                var ok = client.LoadAndRun(id);//Optional. Just simulate that the workflow instance may be running againt because of other events.
                Assert.True(ok);

                var r = client.Wakeup(id, "Service wakeup");
                Assert.Equal("Someone waked me up", r);

            }
        }

        [Fact]
        public void TestWaitForSignalOrDelayAfterDelay()
        {
            Guid id;
            // Create service host.
            using (var host = CreateHost())
            {
                host.Open();
                Assert.Equal(CommunicationState.Opened, host.State);


                // Create a client that sends a message to create an instance of the workflow.
                var client = ChannelFactory<Fonlow.WorkflowDemo.Contracts.IWakeup>.CreateChannel(new NetTcpBinding(), new EndpointAddress(new Uri(baseUri, "wakeup")));
                id = client.Create("Service wakeup", TimeSpan.FromSeconds(1));
                Assert.NotEqual(Guid.Empty, id);

                Thread.Sleep(2000);//so the service may have time to persist. Upon being reloaded, no bookmark calls needed.


                var ok = client.LoadAndRun(id);
                Assert.True(ok);
                Thread.Sleep(8000);//So the service may have saved the result.

                var r = client.Wakeup(id, "Service wakeup");
                Assert.Equal("I sleep for good duration", r);

            }
        }


    }
}
