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
    public class NonServiceWorkflowTests
    {

        static WorkflowServiceHost CreateHost(Activity activity, System.ServiceModel.Channels.Binding binding, EndpointAddress endpointAddress)
        {
            var host = new WorkflowServiceHost(activity);
            {
                SqlWorkflowInstanceStoreBehavior instanceStoreBehavior = new SqlWorkflowInstanceStoreBehavior("Server =localhost; Initial Catalog = WF; Integrated Security = SSPI")
                {
                    HostLockRenewalPeriod = new TimeSpan(0, 0, 5),
                    RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 2),
                    InstanceCompletionAction = InstanceCompletionAction.DeleteAll,
                    InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry,
                    InstanceEncodingOption = InstanceEncodingOption.GZip,
                    MaxConnectionRetries = 3,
                };
                host.Description.Behaviors.Add(instanceStoreBehavior);

                //Make sure this is cleared defined, otherwise the bookmark is not really saved in DB though a new record is created. https://msdn.microsoft.com/en-us/library/ff729670%28v=vs.110%29.aspx
                WorkflowIdleBehavior idleBehavior = new WorkflowIdleBehavior()
                {
                    TimeToPersist = TimeSpan.Zero,
                    TimeToUnload = TimeSpan.Zero,
                };
                host.Description.Behaviors.Add(idleBehavior);

                WorkflowUnhandledExceptionBehavior unhandledExceptionBehavior = new WorkflowUnhandledExceptionBehavior()
                {
                    Action = WorkflowUnhandledExceptionAction.Terminate,
                };
                host.Description.Behaviors.Add(unhandledExceptionBehavior);

                ResumeBookmarkEndpoint endpoint = new ResumeBookmarkEndpoint(binding, endpointAddress);
                host.AddServiceEndpoint(endpoint);

                var debugBehavior = host.Description.Behaviors.Find<ServiceDebugBehavior>();
                if (debugBehavior == null)
                {
                    host.Description.Behaviors.Add(new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
                }
                else
                {
                    debugBehavior.IncludeExceptionDetailInFaults = true;
                }
            }

            return host;
        }


        [Fact]
        public void TestWaitForSignalOrDelayWithBookmark()
        {
            var endpointAddress = new EndpointAddress("net.tcp://localhost/nonservice/wakeup");
            var endpointBinding = new NetTcpBinding(SecurityMode.None);
            using (var host = CreateHost(new WaitForSignalOrDelayWorkflow(), endpointBinding, endpointAddress))
            {
                host.Open();
                Assert.Equal(CommunicationState.Opened, host.State);


                IWorkflowWithBookmark client = new ChannelFactory<IWorkflowWithBookmark>(endpointBinding, endpointAddress).CreateChannel();
                Guid id = client.Create(new Dictionary<string, object> { { "BookmarkName", "NonService Wakeup" }, { "Duration", TimeSpan.FromSeconds(100) } });
                Assert.NotEqual(Guid.Empty, id);

                Thread.Sleep(2000);//so the service may have time to persist.

                client.ResumeBookmark(id, "NonService Wakeup", "something");
            }
        }

        [Fact]
        public void TestWaitForSignalOrDelayWithWrongBookmark()
        {
            var endpointAddress = new EndpointAddress("net.tcp://localhost/nonservice/wakeup");
            var endpointBinding = new NetTcpBinding(SecurityMode.None);
            using (var host = CreateHost(new WaitForSignalOrDelayWorkflow(), endpointBinding, endpointAddress))
            {

                host.Open();
                Assert.Equal(CommunicationState.Opened, host.State);

                IWorkflowWithBookmark client = new ChannelFactory<IWorkflowWithBookmark>(endpointBinding, endpointAddress).CreateChannel();
                Guid id = client.Create(new Dictionary<string, object> { { "BookmarkName", "NonService Wakeup" }, { "Duration", TimeSpan.FromSeconds(100) } });
                Assert.NotEqual(Guid.Empty, id);

                Thread.Sleep(2000);//so the service may have time to persist.

                var ex = Assert.Throws<FaultException>(() =>
                    client.ResumeBookmark(id, "NonService Wakeupkkk", "something"));

                Debug.WriteLine(ex.ToString());
            }
        }


    }


}