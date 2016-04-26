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
using System.IO;


namespace BasicTests
{


    public class WFDefinitionIdentityFactoryTests
    {
        public WFDefinitionIdentityFactoryTests()
        {
            stopwatch = new Stopwatch();
            stopwatch2 = new Stopwatch();
        }

        Stopwatch stopwatch, stopwatch2;

        static string NewBookmarkName()
        {
            return "ReadLine" + DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
        }


        [Fact]
        public void TestPersistenceWithDelayAndResult()
        {
            var a = new Fonlow.Activities.Calculation();
            a.XX = 3;
            a.YY = 7;

            bool completed1 = false;
            bool unloaded1 = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var definitionIdentity = WFDefinitionIdentityFactory.GetWorkflowIdentity(a);
            var app = new WorkflowApplication(a, definitionIdentity);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.Unload;//so persist and unload
            };

            app.OnUnhandledException = (e) =>
            {

                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                completed1 = true;

            };

            app.Aborted = (eventArgs) =>
            {

            };

            app.Unloaded = (eventArgs) =>
            {
                unloaded1 = true;
                syncEvent.Set();
            };

            var id = app.Id;
            stopwatch.Restart();
            stopwatch2.Restart();
            app.Run();
            syncEvent.WaitOne();

            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds < 2500, String.Format("The first one is executed for {0} milliseconds", stopwatch.ElapsedMilliseconds));
            //the ellipsed time depends on the performance of the WF runtime when handling persistence. The first case of persistence is slow.

            Assert.False(completed1);
            Assert.True(unloaded1);

            stopwatch.Restart();
            var t = WFDefinitionIdentityFactory.Instance.TryAdd(definitionIdentity, a);
            stopwatch.Stop();
            Trace.TraceInformation("It took {0} seconds to persist definition", stopwatch.Elapsed.TotalSeconds);

            //Now to use a new WorkflowApplication to load the persisted instance.
            var dic =  LoadAndCompleteLongRunning(id, definitionIdentity);
            var finalResult = (long)dic["Result"];
            Assert.Equal(21, finalResult);

        }

        IDictionary<string, object> LoadAndCompleteLongRunning(Guid instanceId, WorkflowIdentity definitionIdentity)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var instance = WorkflowApplication.GetInstance(instanceId, WFDefinitionIdentityStore.Instance.Store);
            var definition = WFDefinitionIdentityFactory.Instance[definitionIdentity];
            IDictionary<string, object> dic = null;
            var app2 = new WorkflowApplication(definition, instance.DefinitionIdentity)
            {
                Completed = e =>
                {
                    completed2 = true;
                    if (e.CompletionState== ActivityInstanceState.Closed)
                    {
                        dic = e.Outputs;
                    }
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent.Set();
                },

                InstanceStore = WFDefinitionIdentityStore.Instance.Store,
            };

            stopwatch.Restart();
            app2.Load(instance);
            Trace.TraceInformation("It took {0} seconds to load workflow", stopwatch.Elapsed.TotalSeconds);


            app2.Run();
            syncEvent.WaitOne();
            stopwatch2.Stop();
            var seconds = stopwatch2.Elapsed.TotalSeconds;

            Assert.True(completed2);
            Assert.True(unloaded2);

            return dic;

        }


        [Fact]
        public void TestWaitForSignalOrDelay()
        {
            var a = new WaitForSignalOrDelay()
            {
                Duration=TimeSpan.FromSeconds(10),
                BookmarkName="Wakeup",
            };

            var definitionIdentity = WFDefinitionIdentityFactory.GetWorkflowIdentity(a);

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            bool completed1 = false;
            bool unloaded1 = false;
            var app = new WorkflowApplication(a, definitionIdentity)
            {
                InstanceStore = WFDefinitionStore.Instance.Store,
                PersistableIdle = (eventArgs) =>
                {
                    return PersistableIdleAction.Unload;
                },

                OnUnhandledException = (e) =>
                {
                    return UnhandledExceptionAction.Abort;
                },

                Completed = delegate (WorkflowApplicationCompletedEventArgs e)
                {
                    completed1 = true;
                    syncEvent.Set();
                },


                Unloaded = (eventArgs) =>
                {
                    unloaded1 = true;
                    syncEvent.Set();
                },
            };

            var id = app.Id;
            app.Run();


            syncEvent.WaitOne();
            Assert.False(completed1);
            Assert.True(unloaded1);


            Thread.Sleep(5000); // from 1 seconds to 9 seconds, the total time of the test case is the same.

            var outputs = LoadAndCompleteLongRunning(id, definitionIdentity);

            Assert.False((bool)outputs["Result"]);

        }


    }


}
