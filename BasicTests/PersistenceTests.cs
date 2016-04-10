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


    public class WorkflowApplicationPersistenceTests
    {
        public WorkflowApplicationPersistenceTests()
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
            var bookmarkName = NewBookmarkName();
            var a = new System.Activities.Statements.Sequence()
            {
                Variables =
                        {
                            t1
                        },
                Activities = {
                            new Multiply()
                            {
                                X=3, Y=7,
                            },

                            new ReadLine()
                            {
                                BookmarkName=bookmarkName,
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

                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                unloaded1 = true;

            };

            app.Aborted = (eventArgs) =>
            {

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

            //  app.Persist();//This is optional, since Workflow runtime will persist when the execution reach to ReadLine.
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);
            Assert.True(isIdle);
            //At this point, DB WF/InstancesTable has a new record, and the value of column BlockingBookmark contains the bookmarkName

            //Now to use a new WorkflowApplication to load the persisted instance.
            LoadWithBookmarkAndComplete(a, id, bookmarkName, "abc");
            //The record is now deleted by WF runtime.
        }

        static IDictionary<string, object> LoadWithBookmarkAndComplete(Activity workflowDefinition, Guid instanceId, string bookmarkName, string bookmarkValue)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            IDictionary<string, object> outputs = null;

            var app2 = new WorkflowApplication(workflowDefinition)
            {
                Completed = e =>
                {
                    if (e.CompletionState == ActivityInstanceState.Closed)
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

            app2.Load(instanceId);
            var br = app2.ResumeBookmark(bookmarkName, bookmarkValue);
            Assert.Equal(BookmarkResumptionResult.Success, br);

            syncEvent.WaitOne();

            Assert.True(completed2);
            Assert.True(unloaded2);

            return outputs;
        }


        [Fact]
        public void TestPersistenceWithBookmarkAndOutputs()
        {
            var bookmarkName = NewBookmarkName();
            var a = new ReadLineReturnOrThrow()
            {
                BookmarkName = bookmarkName,
            };


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

            //  app.Persist();
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);

            //Now to use a new WorkflowApplication to load the persisted instance.
            var dic = LoadWithBookmarkAndComplete(a, id, bookmarkName, "Something");
            Assert.Equal("SomethingABC", dic["FinalResult"]);
        }



        [Fact]
        public void TestPersistenceWithBookmarkAndThrowsWihoutOnUnhandledException()
        {
            var bookmarkName = NewBookmarkName();
            var a = new ReadLineReturnOrThrow()
            {
                BookmarkName=bookmarkName,
            };


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

                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                unloaded1 = true;

            };

            app.Aborted = (eventArgs) =>
            {

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
                    Assert.Equal(ActivityInstanceState.Faulted, e.CompletionState);// Exception thrown during resume but without exception handler, so it should be Faulted
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

            app2.Load(id);

            app2.ResumeBookmark(bookmarkName, null); //so exception will be thrown

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

            };

            app.Persist();
            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.True(completed1);

        }


        //[Fact]
        //public void TestPersistenceWithNoPersistableIdle()
        //{
        //    var a = new Multiply()
        //    {
        //        X = 3,
        //        Y = 7,
        //    };


        //    bool unloaded = false;
        //    bool completed = false;

        //    AutoResetEvent syncEvent = new AutoResetEvent(false);

        //    var app = new WorkflowApplication(a);
        //    app.InstanceStore = WFDefinitionStore.Instance.Store;
        //    app.PersistableIdle = (eventArgs) =>
        //    {
        //        Assert.True(false, "quick action no need to persist");//lazy
        //        return PersistableIdleAction.Persist;
        //    };

        //    //None of the handlers should be running
        //    app.OnUnhandledException = (e) =>
        //    {
        //        
        //        return UnhandledExceptionAction.Abort;
        //    };

        //    app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
        //    {
        //        completed = true;
        //        syncEvent.Set();
        //    };

        //    app.Aborted = (eventArgs) =>
        //    {
        //        
        //    };

        //    app.Unloaded = (eventArgs) =>
        //    {
        //        unloaded = true;
        //        Assert.True(true);
        //    };

        //    app.Run();
        //    Assert.False(completed);
        //    Assert.False(unloaded);
        //    syncEvent.WaitOne();

        //    Assert.True(completed);
        //    Assert.False(unloaded);
        //}

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
            var t = WFDefinitionStore.Instance.TryAdd(id, a);
            stopwatch.Stop();
            Trace.TraceInformation("It took {0} seconds to persist definition", stopwatch.Elapsed.TotalSeconds);

            //Now to use a new WorkflowApplication to load the persisted instance.
            var dic =  LoadAndCompleteLongRunning(id);
            var finalResult = (long)dic["Result"];
            Assert.Equal(21, finalResult);

        }

        IDictionary<string, object> LoadAndCompleteLongRunning(Guid instanceId)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            IDictionary<string, object> dic = null;
            var app2 = new WorkflowApplication(WFDefinitionStore.Instance[instanceId])
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

                InstanceStore = WFDefinitionStore.Instance.Store,
            };

            stopwatch.Restart();
            app2.Load(instanceId);
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

                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                completed1 = true;
                //   Assert.True(stopwatch.Elapsed.TotalSeconds > 3 && stopwatch.Elapsed.TotalSeconds < 5);
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {

            };

            app.Unloaded = (eventArgs) =>
            {

            };

            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();


            Assert.True(completed1);
            Assert.False(unloaded1);

        }


        [Fact]
        public void TestWaitForSignalOrDelayWithBookmarkToWakup()
        {
            var bookmarkName = "Wakup Now";
            var a = new WaitForSignalOrDelay()
            {
                Duration = TimeSpan.FromSeconds(10),
                BookmarkName = bookmarkName,
            };

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            bool completed1 = false;
            bool unloaded1 = false;
            IDictionary<string, object> outputs = null;
            var app = new WorkflowApplication(a)
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
                    outputs = e.Outputs;
                    syncEvent.Set();
                },

                Aborted = (eventArgs) =>
                {

                },

                Unloaded = (eventArgs) =>
                {
                    unloaded1 = true;
                    syncEvent.Set();
                }
            };

            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);

            outputs = LoadWithBookmarkAndComplete(a, id, bookmarkName, null);//Wakup does not need bookmark value
            Assert.True((bool)outputs["Result"]);
        }

        [Fact]
        public void TestWaitForSignalOrDelayAndWakupAfterPendingTimeExpire()
        {
            var a = new WaitForSignalOrDelay()
            {
                Duration=TimeSpan.FromSeconds(10),
                BookmarkName="Wakeup",
            };

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            bool completed1 = false;
            bool unloaded1 = false;
            var app = new WorkflowApplication(a)
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

            WFDefinitionStore.Instance.TryAdd(id, a);

            Thread.Sleep(5000); // from 1 seconds to 9 seconds, the total time of the test case is the same.

            var outputs = LoadAndCompleteLongRunning(id);

            Assert.False((bool)outputs["Result"]);

        }


        [Fact]
        public void TestSleepingWorkflowWithBookmarkToWakup()
        {
            var bookmarkName = "Wakup Now";
            var a = new WaitForSignalOrDelayWorkflow()
            {
                Duration = TimeSpan.FromSeconds(10),
                BookmarkName = bookmarkName,
            };

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            bool completed1 = false;
            bool unloaded1 = false;
            IDictionary<string, object> outputs = null;
            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.Unload;
            };

            app.OnUnhandledException = (e) =>
            {

                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                completed1 = true;
                outputs = e.Outputs;
                syncEvent.Set();
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
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);

            outputs = LoadWithBookmarkAndComplete(a, id, bookmarkName, null);//Wakup does not need bookmark value
            Assert.Equal("Someone waked me up", (string)outputs["Result"]);
        }



        [Fact]
        public void TestRunningWorkflowTwice()
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
            var t = WFDefinitionStore.Instance.TryAdd(id, a);
            stopwatch.Stop();
            Trace.TraceInformation("It took {0} seconds to persist definition", stopwatch.Elapsed.TotalSeconds);

            Assert.Throws<WorkflowApplicationUnloadedException>(() => app.Run());

        }


        [Fact]
        public void TestWaitForSignalOrAlarmWithBookmarkToWakup()
        {
            var bookmarkName = "Wakup Now";
            var a = new WaitForSignalOrAlarm()
            {
                AlarmTime=DateTime.Now.AddSeconds(10),
                BookmarkName = bookmarkName,
            };

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            bool completed1 = false;
            bool unloaded1 = false;
            IDictionary<string, object> outputs = null;
            var app = new WorkflowApplication(a)
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
                    outputs = e.Outputs;
                    syncEvent.Set();
                },

                Aborted = (eventArgs) =>
                {
                    syncEvent.Set();
                },

                Unloaded = (eventArgs) =>
                {
                    unloaded1 = true;
                    syncEvent.Set();
                }
            };

            var id = app.Id;
            app.Run();
            syncEvent.WaitOne();

            Assert.False(completed1);
            Assert.True(unloaded1);

            outputs = LoadWithBookmarkAndComplete(a, id, bookmarkName, null);//Wakup does not need bookmark value
            Assert.True((bool)outputs["Result"]);
        }

        [Fact]
        public void TestWaitForSignalOrAlarmAndWakupAfterPendingTimeExpire()
        {
            var a = new WaitForSignalOrAlarm()
            {
                AlarmTime = DateTime.Now.AddSeconds(10),
                BookmarkName = "Wakeup",
            };

            AutoResetEvent syncEvent = new AutoResetEvent(false);

            bool completed1 = false;
            bool unloaded1 = false;
            var app = new WorkflowApplication(a)
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

            WFDefinitionStore.Instance.TryAdd(id, a);

            Thread.Sleep(5000); // from 1 seconds to 9 seconds, the total time of the test case is the same.

            var outputs = LoadAndCompleteLongRunning(id);

            Assert.False((bool)outputs["Result"]);
        }



    }


}
