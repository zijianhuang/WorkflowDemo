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
        [Fact]
        public void TestPersistenceWithBookmark()
        {
            const string readLineBookmark = "ReadLine1";
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
            LoadAndComplete(a, id, readLineBookmark);
        }

        static void LoadAndComplete(Activity workflowDefinition, Guid instanceId, string bookmarkName)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app2 = new WorkflowApplication(workflowDefinition)
            {
                Completed = e =>
                {
                    completed2 = true;
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent.Set();
                },

                InstanceStore = NewStore(),
            };

            var input = "Something";//The condition is met
            app2.Load(instanceId);

            //this resumes the bookmark setup by readline
            app2.ResumeBookmark(bookmarkName, input);

            syncEvent.WaitOne();

            Assert.True(completed2);
            Assert.True(unloaded2);

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
        public void TestPersistenceSqlWithDelay()
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
                      new Multiply()
                    {
                        X=3,
                        Y=7,
                    },

                            new System.Activities.Statements.Delay()
                            {
                                Duration= TimeSpan.FromSeconds(3),
                            },
                            plus,

                        },
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
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            app.Run();
            syncEvent.WaitOne();

            stopwatch.Stop();
            Assert.True(stopwatch.ElapsedMilliseconds < 500, "The activity will take over 2 seconds however will be persisted and unloaded righ away.");

            Assert.False(completed1);
            Assert.True(unloaded1);

            //Now to use a new WorkflowApplication to load the persisted instance.
            LoadAndCompleteLongRunning(a, id);
        }

        static void LoadAndCompleteLongRunning(Activity workflowDefinition, Guid instanceId)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var app2 = new WorkflowApplication(workflowDefinition)
            {
                Completed = e =>
                {
                    completed2 = true;
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent.Set();
                },

                InstanceStore = NewStore(),
            };

            app2.Load(instanceId);

            app2.Run();

            var dt = DateTime.Now;
            syncEvent.WaitOne();
            Assert.True((DateTime.Now - dt).TotalSeconds > 2);//But if the long running process is fired and forgot, the late load and run may be completed immediately.

            Assert.True(completed2);
            Assert.True(unloaded2);

        }



        [Fact]
        public void TestPersistenceSqlWithActivityPersistedInStream()
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
                            new System.Activities.Statements.Delay()
                            {
                                Duration= TimeSpan.FromSeconds(3),
                            },
                            plus,

                        },
            };


            bool completed1 = false;
            bool unloaded1 = false;

            AutoResetEvent syncEvent = new AutoResetEvent(false);
            WorkflowIdentity identity = new WorkflowIdentity()
            {
                Name = "MyDemoWorkflow" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                Version = new Version(1, 0, 0),
            };

            var app = new WorkflowApplication(a, identity);
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


            var stream = new MemoryStream();
            ActivityPersistenceHelper.SaveActivity(a, stream);
            stream.Position = 0;

            File.WriteAllBytes(@"c:\test\activity.xml", stream.ToArray());


            //Now to use a new WorkflowApplication to load the persisted instance.
            LoadAndComplete2(stream, id, identity);
        }

        static void LoadAndComplete2(Stream wfStream, Guid instanceId, WorkflowIdentity identity)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var workflowDefinition = ActivityPersistenceHelper.LoadActivity(wfStream);
            wfStream.Dispose();

            var app2 = new WorkflowApplication(workflowDefinition, identity)
            {
                Completed = e =>
                {
                    completed2 = true;
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent.Set();
                },

                InstanceStore = NewStore(),
            };

            app2.Load(instanceId);

            //this resumes the bookmark setup by readline
            app2.Run();
            syncEvent.WaitOne();

            Assert.True(completed2);
            Assert.True(unloaded2);

        }


        [Fact]
        public void TestPersistenceSqlWithBookmarkWithActivityPersistedInStream()
        {
            const string readLineBookmark = "ReadLine1";
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

            MemoryStream stream = new MemoryStream();
            ActivityPersistenceHelper.SaveActivity(a, stream);
            stream.Position = 0;

            WFDefinitionStore.Instance.InstanceDefinitions.TryAdd(id, stream.ToArray());
            //Now to use a new WorkflowApplication to load the persisted instance.
            LoadAndComplete3(id, readLineBookmark);
        }

        static void LoadAndComplete3(Guid instanceId, string bookmarkName)
        {
            bool completed2 = false;
            bool unloaded2 = false;
            AutoResetEvent syncEvent = new AutoResetEvent(false);

            var workflowDefinition = ActivityPersistenceHelper.LoadActivity(WFDefinitionStore.Instance.InstanceDefinitions[instanceId]);
            var app2 = new WorkflowApplication(workflowDefinition)
            {
                Completed = e =>
                {
                    completed2 = true;
                },

                Unloaded = e =>
                {
                    unloaded2 = true;
                    syncEvent.Set();
                },

                InstanceStore = NewStore(),
            };

            var input = "Something";//The condition is met
            app2.Load(instanceId);

            //this resumes the bookmark setup by readline
            app2.ResumeBookmark(bookmarkName, input);

            syncEvent.WaitOne();

            Assert.True(completed2);
            Assert.True(unloaded2);

            byte[] temp;
            WFDefinitionStore.Instance.InstanceDefinitions.TryRemove(instanceId, out temp);

        }


        static System.Runtime.DurableInstancing.InstanceStore NewStore()
        {
            var store = new SqlWorkflowInstanceStore("Server =localhost; Initial Catalog = WF; Integrated Security = SSPI")
            {
                InstanceCompletionAction= InstanceCompletionAction.DeleteAll,
                InstanceEncodingOption= InstanceEncodingOption.GZip,
                
            };

            var handle = store.CreateInstanceHandle();
            var view = store.Execute(handle, new CreateWorkflowOwnerCommand(), TimeSpan.FromSeconds(30));
            handle.Free();
            store.DefaultInstanceOwner = view.InstanceOwner;
            return store;
        }

    }
}
