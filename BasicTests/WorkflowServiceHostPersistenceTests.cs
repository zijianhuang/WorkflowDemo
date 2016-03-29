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
using System.Diagnostics;

namespace BasicTests
{
    public class WorkflowServiceHostPersistenceTests
    {
        const string connectionString = "Server =localhost; Initial Catalog = WF; Integrated Security = SSPI";
        const string hostBaseAddress = "net.tcp://localhost/CountingService";
        [Fact]
        public void TestOpenHost()
        {
            // Create service host.
            WorkflowServiceHost host = new WorkflowServiceHost(new Microsoft.Samples.BuiltInConfiguration.CountingWorkflow(), new Uri(hostBaseAddress));


            // Add service endpoint.
            host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), "");

            SqlWorkflowInstanceStoreBehavior instanceStoreBehavior = new SqlWorkflowInstanceStoreBehavior("Server =localhost; Initial Catalog = WF; Integrated Security = SSPI");
            instanceStoreBehavior.HostLockRenewalPeriod = new TimeSpan(0, 0, 5);
            instanceStoreBehavior.RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 2);
            instanceStoreBehavior.InstanceCompletionAction = InstanceCompletionAction.DeleteAll;
            instanceStoreBehavior.InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry;
            instanceStoreBehavior.InstanceEncodingOption = InstanceEncodingOption.GZip;
            host.Description.Behaviors.Add(instanceStoreBehavior);

            host.Open(TimeSpan.FromSeconds(2));
            Assert.Equal(CommunicationState.Opened, host.State);


            // Create a client that sends a message to create an instance of the workflow.
            ICountingWorkflow client = ChannelFactory<ICountingWorkflow>.CreateChannel(new NetTcpBinding(), new EndpointAddress(hostBaseAddress));
            client.start();
            Debug.WriteLine("client.start() done.");
            System.Threading.Thread.Sleep(10000);
            Debug.WriteLine("sleep finished");
            host.Close();
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/ff432975%28v=vs.110%29.aspx
        /// </summary>
        [Fact]
        public void TestMultiplyXY()
        {
            // Create service host.
            WorkflowServiceHost host = new WorkflowServiceHost(new BasicTests.MultiplyWorkflow(), new Uri(hostBaseAddress));


            // Add service endpoint.
            host.AddServiceEndpoint("ICalculation", new NetTcpBinding(), "");

            SqlWorkflowInstanceStoreBehavior instanceStoreBehavior = new SqlWorkflowInstanceStoreBehavior("Server =localhost; Initial Catalog = WF; Integrated Security = SSPI");
            instanceStoreBehavior.HostLockRenewalPeriod = new TimeSpan(0, 0, 5);
            instanceStoreBehavior.RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 2);
            instanceStoreBehavior.InstanceCompletionAction = InstanceCompletionAction.DeleteAll;
            instanceStoreBehavior.InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry;
            instanceStoreBehavior.InstanceEncodingOption = InstanceEncodingOption.GZip;
            host.Description.Behaviors.Add(instanceStoreBehavior);

            host.Open(TimeSpan.FromSeconds(2));
            Assert.Equal(CommunicationState.Opened, host.State);


            // Create a client that sends a message to create an instance of the workflow.
            var client = ChannelFactory<ICalculation>.CreateChannel(new NetTcpBinding(), new EndpointAddress(hostBaseAddress));
            client.MultiplyXY(3, 7);

            //Debug.WriteLine("client.start() done.");
            //System.Threading.Thread.Sleep(10000);
            //Debug.WriteLine("sleep finished");
            System.Threading.Thread.Sleep(3000);
            var r = client.GetLateResult();
            Assert.Equal(21, r);
            host.Close();
        }

    }
}
