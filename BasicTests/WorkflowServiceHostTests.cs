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

namespace BasicTests
{
    public class WorkflowServiceHostTests
    {
        const string connectionString = "Server =localhost; Initial Catalog = WF; Integrated Security = SSPI";
        const string hostBaseAddress = "net.tcp://localhost/CountingService";
        [Fact]
        public void TestOpenHostWithoutContractImpThrows()
        {
            // Create service host.
            using (WorkflowServiceHost host = new WorkflowServiceHost(new Plus(), new Uri(hostBaseAddress)))
            {

                Assert.Throws<InvalidOperationException>(()
                    => host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), ""));
            }
        }

        [Fact]
        public void TestOpenHostWithWrongStoreThrows()
        {
            // Create service host.
            WorkflowServiceHost host = new WorkflowServiceHost(new Microsoft.Samples.BuiltInConfiguration.CountingWorkflow(), new Uri(hostBaseAddress));
            

                // Add service endpoint.
                host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), "");

            SqlWorkflowInstanceStoreBehavior instanceStoreBehavior = new SqlWorkflowInstanceStoreBehavior("Server =localhost; Initial Catalog = WFXXX; Integrated Security = SSPI");
            instanceStoreBehavior.HostLockRenewalPeriod = new TimeSpan(0, 0, 5);
            instanceStoreBehavior.RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 2);
            instanceStoreBehavior.InstanceCompletionAction = InstanceCompletionAction.DeleteAll;
            instanceStoreBehavior.InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry;
            instanceStoreBehavior.InstanceEncodingOption = InstanceEncodingOption.GZip;
            host.Description.Behaviors.Add(instanceStoreBehavior);

            var ex = Assert.Throws<CommunicationException>(()
                => host.Open(TimeSpan.FromSeconds(2)));

            Assert.NotNull(ex.InnerException);
            Assert.Equal(typeof(System.Runtime.DurableInstancing.InstancePersistenceCommandException), ex.InnerException.GetType());
            Assert.Equal(CommunicationState.Faulted, host.State);//so can't be disposed.
        }
    }

    [ServiceContract]
    public interface ICountingWorkflow
    {
        [OperationContract(IsOneWay = true)]
        void start();
    }

}
