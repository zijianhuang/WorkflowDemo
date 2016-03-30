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


    public class PersistenceTests
    {
        public PersistenceTests()
        {
            stopwatch = new Stopwatch();
            stopwatch2 = new Stopwatch();
        }

        Stopwatch stopwatch, stopwatch2;

        const string readLineBookmark = "ReadLine1";

        [Fact]
        public void TestPersistenceWithBookmark()
        {
            var x = 100;
            var y = 200;
            var t1 = new Variable<int>("t1");

            var plus = new Plus()
            {
                X = x,
                Y = y,
                Z = t1,  //So Output Z will be assigned to t1
            };
            var a = new System.Activities.Statements.Sequence()
            {
                Variables =
                        {
                            t1
                        },
                Activities = {
                            new ReadLine()
                            {
                                BookmarkName=readLineBookmark,
                            },
                            plus,

                        },
            };


            bool completed1 = false;
            bool unloaded1 = false;
            bool isIdle = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.Unload;//so persist and unload
            };

            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                unloaded1 = true;
                Assert.True(false);
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                unloaded1 = true;
                syncEvent.Set();
            };

            app.Idle = e =>
            {
                Assert.Equal(1, e.Bookmarks.Count);
                isIdle = true;
            };

            //  app.Persist();
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);
            Assert.True(isIdle);

            //Now to use a new WorkflowApplication to load the persisted instance.
            LoadAndComplete(a, id, readLineBookmark);
        }

        static IDictionary<string, object> LoadAndComplete(Activity workflowDefinition, Guid instanceId, string bookmarkName)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            IDictionary<string, object> outputs = null;

            var app2 = new WorkflowApplication(workflowDefinition)
            {
                Completed = e =>
                {
                    if (e.CompletionState== ActivityInstanceState.Closed)
                    {
                        outputs = e.Outputs;
                    }
                    completed2 = true;
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent.Set();
                },

                InstanceStore = WFDefinitionStore.Instance.Store,
            };

            var input = "Something";//The condition is met
            app2.Load(instanceId);

            //this resumes the bookmark setup by readline
            app2.ResumeBookmark(bookmarkName, input);

            syncEvent.WaitOne();

            Assert.True(completed2);
            Assert.True(unloaded2);

            return outputs;
        }


        [Fact]
        public void TestPersistenceWithBookmarkAndOutputs()
        {
            var a = new ReadLineReturnOrThrow();


            bool completed1 = false;
            bool unloaded1 = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.Unload;//so persist and unload
            };

            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                unloaded1 = true;
                Assert.True(false);
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                unloaded1 = true;
                syncEvent.Set();
            };

            //  app.Persist();
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);

            //Now to use a new WorkflowApplication to load the persisted instance.
            var dic = LoadAndComplete(a, id, readLineBookmark);
            Assert.Equal("SomethingABC", dic["FinalResult"]);
        }

        [Fact]
        public void TestPersistenceWithBookmarkAndThrows()
        {
            var a = new ReadLineReturnOrThrow();


            bool completed1 = false;
            bool unloaded1 = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.Unload;//so persist and unload
            };

            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                unloaded1 = true;
                Assert.True(false);
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                unloaded1 = true;
                syncEvent.Set();
            };

            //  app.Persist();
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);




            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent2 = new AutoResetEvent(false);
            IDictionary<string, object> outputs = null;

            var app2 = new WorkflowApplication(a)
            {
                Completed = e =>
                {
                    Assert.True(false);
                },

                Unloaded = e =>
                {
                    Assert.True(false);
                },

                //Must not syncEvent.Set() in the callback.
                OnUnhandledException = (e) =>
                {
                    Assert.True(e.UnhandledException is ArgumentException);
                    return UnhandledExceptionAction.Abort;
                },

                Aborted = e=>
                {
                    Assert.True(e.Reason is ArgumentException);
                    syncEvent2.Set();
                },


                InstanceStore = WFDefinitionStore.Instance.Store,
            };

            string input = null;//The condition is met
            app2.Load(id);

            //this resumes the bookmark setup by readline
            app2.ResumeBookmark(readLineBookmark, input);

            syncEvent2.WaitOne();

            Assert.False(completed2);
            Assert.False(unloaded2);

        }


        [Fact]
        public void TestPersistenceWithBookmarkAndThrowsWihoutOnUnhandledException()
        {
            var a = new ReadLineReturnOrThrow();


            bool completed1 = false;
            bool unloaded1 = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.Unload;//so persist and unload
            };

            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                unloaded1 = true;
                Assert.True(false);
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                unloaded1 = true;
                syncEvent.Set();
            };

            //  app.Persist();
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);




            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent2 = new AutoResetEvent(false);
            IDictionary<string, object> outputs = null;

            var app2 = new WorkflowApplication(a)
            {
                Completed = e =>
                {
                    Assert.Equal(ActivityInstanceState.Faulted, e.CompletionState);
                    Assert.NotNull(e.Outputs);
                    Assert.Equal(0, e.Outputs.Count);
                    if (e.CompletionState == ActivityInstanceState.Closed)
                    {
                        outputs = e.Outputs;
                    }
                    completed2 = true;
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent2.Set();
                },

                InstanceStore = WFDefinitionStore.Instance.Store,
            };

            string input = null;//The condition is met
            app2.Load(id);

            //this resumes the bookmark setup by readline
            app2.ResumeBookmark(readLineBookmark, input);

            syncEvent2.WaitOne();

            Assert.True(completed2);
            Assert.True(unloaded2);
            Assert.Null(outputs);

        }




        [Fact]
        public void TestPersistWithWrongStoreThrows()
        {
            var a = new Multiply()
            {
                X = 3,
                Y = 7,
            };


            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = new SqlWorkflowInstanceStore("Server =localhost; Initial Catalog = WFXXX; Integrated Security = SSPI");
            app.PersistableIdle = (eventArgs) =>
            {
                Assert.True(false, "quick action no need to persist");//lazy
                return PersistableIdleAction.Persist;
            };

            //None of the handlers should be running
            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            var ex = Assert.Throws<System.Runtime.DurableInstancing.InstancePersistenceCommandException>
               (() => app.Persist(TimeSpan.FromSeconds(2)));

            Assert.NotNull(ex.InnerException);
            Assert.Equal(typeof(TimeoutException), ex.InnerException.GetType());
        }

        [Fact]
        public void TestPersistNonPersistable()
        {
            var a = new Multiply()
            {
                X = 3,
                Y = 7,
            };

            bool completed1 = false;
            bool isIdle = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                Assert.True(false, "quick action no need to persist");//lazy
                return PersistableIdleAction.Persist;
            };

            //None of the handlers should be running
            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = (e) =>
            {
                completed1 = true;
                syncEvent.Set();
            };

            app.Unloaded = (e) =>
            {
                Assert.True(false, "Nothing to persist");
            };

            app.Idle = e =>
            {
                Assert.True(false);
            };

            app.Persist();
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.True(completed1);

        }


        [Fact]
        public void TestPersistenceWithNoPersistableIdle()
        {
            var a = new Multiply()
            {
                X = 3,
                Y = 7,
            };


            bool unloaded = false;
            bool completed = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                Assert.True(false, "quick action no need to persist");//lazy
                return PersistableIdleAction.Persist;
            };

            //None of the handlers should be running
            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                completed = true;
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                unloaded = true;
                Assert.True(true);
            };

            app.Run();
            Assert.False(completed);
            Assert.False(unloaded);
            syncEvent.WaitOne();

            Assert.True(completed);
            Assert.False(unloaded);
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

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.Unload;//so persist and unload
            };

            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                completed1 = true;
                Assert.True(false);
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
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
            var t = WFDefinitionStore.Instance.TryAdd(id, a);
            stopwatch.Stop();
            Trace.TraceInformation("It took {0} seconds to persist definition", stopwatch.Elapsed.TotalSeconds);

            //Now to use a new WorkflowApplication to load the persisted instance.
            LoadAndCompleteLongRunning(id);
        }

        void LoadAndCompleteLongRunning(Guid instanceId)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app2 = new WorkflowApplication(WFDefinitionStore.Instance[instanceId])
            {
                Completed = e =>
                {
                    completed2 = true;
                    var finalResult = (long)e.Outputs["Result"];
                    Assert.Equal(21, finalResult);
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent.Set();
                },

                InstanceStore = WFDefinitionStore.Instance.Store,
            };

            stopwatch.Restart();
            app2.Load(instanceId);
            Trace.TraceInformation("It took {0} seconds to load workflow", stopwatch.Elapsed.TotalSeconds);


            app2.Run();
            syncEvent.WaitOne();
            stopwatch2.Stop();
            var seconds = stopwatch2.Elapsed.TotalSeconds;
            Assert.True(seconds > 3, String.Format("Activity execute for {0} seconds", seconds));//But if the long running process is fired and forgot, the late load and run may be completed immediately.

            Assert.True(completed2);
            Assert.True(unloaded2);

        }


        [Fact]
        public void TestPersistenceWithDelayAndResultButNoPersist()
        {
            var a = new Fonlow.Activities.Calculation();
            a.XX = 3;
            a.YY = 7;

            bool completed1 = false;
            bool unloaded1 = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.None;
            };

            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                completed1 = true;
                stopwatch.Stop();
                Assert.True(stopwatch.Elapsed.TotalSeconds > 3 && stopwatch.Elapsed.TotalSeconds < 5);
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                Assert.True(false);
            };

            var id = app.Id;
            stopwatch.Restart();
            app.Run();
            syncEvent.WaitOne();


            Assert.True(completed1);
            Assert.False(unloaded1);

        }



    }


}
