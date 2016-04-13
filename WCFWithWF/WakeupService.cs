using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
using Fonlow.Activities;
using System.Activities;
using System.Threading;


namespace Fonlow.WorkflowDemo.Contracts
{


    [ServiceContract]
    public interface IWakeup
    {
        /// <summary>
        /// instantiate a workflow and return the ID right after WorkflowApplication.Run()
        /// </summary>
        [OperationContract]
        Guid Create(string bookmarkName, TimeSpan duration);

        /// <summary>
        /// Reload persisted instance and run and return immediately.
        /// </summary>

        [OperationContract]
        bool LoadAndRun(Guid id);

        /// <summary>
        /// Send a bookmark call to a workflow running. If the workflow instance is not yet loaed upon other events, this call will reload the instance.
        /// </summary>
        [OperationContract]
        string Wakeup(Guid id, string bookmarkName);
    }


    public class WakeupService : IWakeup
    {
        public Guid Create(string bookmarkName, TimeSpan duration)
        {
            var a = new WaitForSignalOrDelayWorkflow()
            {
                Duration = TimeSpan.FromSeconds(10),
                BookmarkName = bookmarkName,
            };

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
                },

                Aborted = (eventArgs) =>
                {

                },

                Unloaded = (eventArgs) =>
                {
                }
            };

            var id = app.Id;
            app.Run();
            return id;
        }


        public bool LoadAndRun(Guid id)
        {
            var app2 = new WorkflowApplication(new WaitForSignalOrDelayWorkflow())
            {
                Completed = e =>
                {
                    if (e.CompletionState == ActivityInstanceState.Closed)
                    {
                        System.Runtime.Caching.MemoryCache.Default.Add(id.ToString(), e.Outputs, DateTimeOffset.MaxValue);//Save outputs at the end of the workflow.
                    }
                },

                Unloaded = e =>
                {
                    System.Diagnostics.Debug.WriteLine("Unloaded in LoadAndRun.");
                },

                InstanceStore = WFDefinitionStore.Instance.Store,
            };

            try
            {
                app2.Load(id);
                app2.Run();
            }
            catch (System.Runtime.DurableInstancing.InstanceNotReadyException ex)
            {
                System.Diagnostics.Trace.TraceWarning(ex.Message);
                return false;
            }

            System.Runtime.Caching.MemoryCache.Default.Add(id.ToString()+"Instance", app2, DateTimeOffset.MaxValue);//Keep the reference to a running instance
            return true;
        }

        public string Wakeup(Guid id, string bookmarkName)
        {
            var savedDic = (System.Runtime.Caching.MemoryCache.Default.Get(id.ToString())) as IDictionary<string, object>;
            if (savedDic!=null)//So the long running process is completed because other events, before the bookmark call comes.
            {
                return (string)savedDic["Result"];
            }

            WorkflowApplication app2;
            var runningInstance = (System.Runtime.Caching.MemoryCache.Default.Get(id.ToString() + "Instance")) as WorkflowApplication;
            if (runningInstance!=null)//the workflow instance is already reloaded
            {
                app2 = runningInstance;
            }
            else
            {
                app2 = new WorkflowApplication(new WaitForSignalOrDelayWorkflow());
            }

            IDictionary<string, object> outputs=null;
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            app2.Completed = e =>
            {
                if (e.CompletionState == ActivityInstanceState.Closed)
                {
                    outputs = e.Outputs;
                }
                syncEvent.Set();
            };

            app2.Unloaded = e =>
            {
                syncEvent.Set();
            };


            if (runningInstance == null)
            {
                try
                {
                    app2.InstanceStore = WFDefinitionStore.Instance.Store;
                    app2.Load(id);
                }
                catch (System.Runtime.DurableInstancing.InstanceNotReadyException ex)
                {
                    System.Diagnostics.Trace.TraceWarning(ex.Message);
                    throw;
                }
            }

            var br = app2.ResumeBookmark(bookmarkName, null);

            switch (br)
            {
                case BookmarkResumptionResult.Success:
                    break;
                case BookmarkResumptionResult.NotFound:
                    throw new InvalidOperationException($"Can not find the bookmark: {bookmarkName}");
                case BookmarkResumptionResult.NotReady:
                    throw new InvalidOperationException($"Bookmark not ready: {bookmarkName}");
                default:
                    throw new InvalidOperationException("hey what's up");
            }

            syncEvent.WaitOne();
            if (outputs == null)
                throw new InvalidOperationException("How can outputs be null?");

            return (string)outputs["Result"];
        }
    }

}
