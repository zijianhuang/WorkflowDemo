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

        static WorkflowServiceHost CreateHost(System.ServiceModel.Channels.Binding binding, EndpointAddress endpointAddress)
        {
            var host = new WorkflowServiceHost(new WaitForSignalOrDelayWorkflow());
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

            }

            return host;
        }


        [Fact]
        public void TestWaitForSignalOrDelay()
        {
            var endpointAddress = new EndpointAddress("net.tcp://localhost/nonservice/wakeup");
            var endpointBinding = new NetTcpBinding(SecurityMode.None);
            var host = CreateHost(endpointBinding, endpointAddress);

            host.Open();
            Assert.Equal(CommunicationState.Opened, host.State);


            IWorkflowCreation client = new ChannelFactory<IWorkflowCreation>(endpointBinding, endpointAddress).CreateChannel();
            //create an instance
            Guid id = client.Create(new Dictionary<string, object> { { "BookmarkName", "NonService Wakeup" }, { "Duration", TimeSpan.FromSeconds(100) } });
            Assert.NotEqual(Guid.Empty, id);

            Thread.Sleep(2000);//so the service may have time to persist.

            //   client.CreateWithInstanceId(id, null);

            client.ResumeBookmark(id, "NonService Wakeup", "something");

            Thread.Sleep(2000);
        }


        //[Fact]
        //public void TestWaitForSignalOrDelayAfterDelay()
        //{
        //    var uri = new Uri("net.tcp://localhost/");
        //    Guid id;
        //    // Create service host.
        //    using (var host = new ServiceHost(typeof(Fonlow.WorkflowDemo.Contracts.WakeupService), uri))
        //    {
        //        host.AddServiceEndpoint("Fonlow.WorkflowDemo.Contracts.IWakeup", new NetTcpBinding(), "wakeup");

        //        ServiceDebugBehavior debug = host.Description.Behaviors.Find<ServiceDebugBehavior>();

        //        if (debug == null)
        //        {
        //            host.Description.Behaviors.Add(
        //                 new ServiceDebugBehavior() { IncludeExceptionDetailInFaults = true });
        //        }
        //        else
        //        {
        //            // make sure setting is turned ON
        //            if (!debug.IncludeExceptionDetailInFaults)
        //            {
        //                debug.IncludeExceptionDetailInFaults = true;
        //            }
        //        }


        //        host.Open();
        //        Assert.Equal(CommunicationState.Opened, host.State);


        //        // Create a client that sends a message to create an instance of the workflow.
        //        var client = ChannelFactory<Fonlow.WorkflowDemo.Contracts.IWakeup>.CreateChannel(new NetTcpBinding(), new EndpointAddress(new Uri(uri, "wakeup")));
        //        id = client.Create("Service wakeup", TimeSpan.FromSeconds(1));
        //        Assert.NotEqual(Guid.Empty, id);

        //        Thread.Sleep(2000);//so the service may have time to persist. Upon being reloaded, no bookmark calls needed.


        //        var ok = client.LoadAndRun(id);
        //        Assert.True(ok);
        //        Thread.Sleep(8000);//So the service may have saved the result.

        //        var r = client.Wakeup(id, "Service wakeup");
        //        Assert.Equal("I sleep for good duration", r);

        //        host.Close();
        //    }
        //}

        static Sequence CreateWorkflow()
        {
            Sequence workflow = new Sequence
            {
                Activities =
                {
                    new WriteActivity
                    {
                        BookmarkName = "hello"
                    }
                }
            };

            return workflow;
        }

    }


    //custom activity 
    class WriteActivity : NativeActivity
    {
        public string BookmarkName { get; set; }

        protected override bool CanInduceIdle
        {
            get { return true; }
        }

        protected override void Execute(NativeActivityContext context)
        {
            //create a bookmark
            context.CreateBookmark(BookmarkName, new BookmarkCallback(OnBookmarkCallback));
        }

        void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object state)
        {
            //write a message when bookmark resumed
            string message = (string)state;
            Debug.WriteLine(message);
        }
    }


}