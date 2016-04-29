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
    public class WorkflowServiceHostPersistenceTests
    {
        const string connectionString = "Server =localhost; Initial Catalog = WF; Integrated Security = SSPI";
        const string hostBaseAddress = "net.tcp://localhost/Persist/CountingService";
        [Fact]
        public void TestOpenHost()
        {
            // Create service host.
            using (WorkflowServiceHost host = new WorkflowServiceHost(new Microsoft.Samples.BuiltInConfiguration.CountingWorkflow(), new Uri(hostBaseAddress)))
            {

                // Add service endpoint.
                host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), "");

                SqlWorkflowInstanceStoreBehavior instanceStoreBehavior = new SqlWorkflowInstanceStoreBehavior(connectionString)
                {
                    HostLockRenewalPeriod = new TimeSpan(0, 0, 5),
                    RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 2),
                    InstanceCompletionAction = InstanceCompletionAction.DeleteAll,
                    InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry,
                    InstanceEncodingOption = InstanceEncodingOption.GZip,
                };
                host.Description.Behaviors.Add(instanceStoreBehavior);

                host.Open();
                Assert.Equal(CommunicationState.Opened, host.State);


                // Create a client that sends a message to create an instance of the workflow.
                ICountingWorkflow client = ChannelFactory<ICountingWorkflow>.CreateChannel(new NetTcpBinding(), new EndpointAddress(hostBaseAddress));
                client.start();
                Debug.WriteLine("client.start() done.");
                System.Threading.Thread.Sleep(10000);
                Debug.WriteLine("sleep finished");
                host.Close();
            }
        }

        [Fact]
        public void TestOpenHostWithWrongStoreThrows()
        {
            // Create service host.
            WorkflowServiceHost host = new WorkflowServiceHost(new Microsoft.Samples.BuiltInConfiguration.CountingWorkflow(), new Uri(hostBaseAddress));


            // Add service endpoint.
            host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), "");

            SqlWorkflowInstanceStoreBehavior instanceStoreBehavior = new SqlWorkflowInstanceStoreBehavior("Server =localhost; Initial Catalog = WFXXX; Integrated Security = SSPI")
            {
                HostLockRenewalPeriod = new TimeSpan(0, 0, 5),
                RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 2),
                InstanceCompletionAction = InstanceCompletionAction.DeleteAll,
                InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry,
                InstanceEncodingOption = InstanceEncodingOption.GZip,

            };
            host.Description.Behaviors.Add(instanceStoreBehavior);


            var ex = Assert.Throws<CommunicationException>(()
                => host.Open());

            Assert.NotNull(ex.InnerException);
            Assert.Equal(typeof(System.Runtime.DurableInstancing.InstancePersistenceCommandException), ex.InnerException.GetType());
            Assert.Equal(CommunicationState.Faulted, host.State);//so can't be disposed.
        }


    }
}
