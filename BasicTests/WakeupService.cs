using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
using Fonlow.Activities;
using System.Activities;


namespace Fonlow.WorkflowDemo.Contracts
{


    [ServiceContract]
    public interface IWakeup
    {
        [OperationContract]
        Guid Create(string bookmarkName, TimeSpan duration);

        [OperationContract]
        bool Wakeup(Guid id, string bookmarkName);
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

        public bool Wakeup(Guid id, string bookmarkName)
        {
            IDictionary<string, object> outputs;
            var app2 = new WorkflowApplication(new WaitForSignalOrDelayWorkflow())
            {
                Completed = e =>
                {
                    if (e.CompletionState == ActivityInstanceState.Closed)
                    {
                        outputs = e.Outputs;
                    }
                },

                Unloaded = e =>
                {
                },

                InstanceStore = WFDefinitionStore.Instance.Store,
            };

            try
            {
                app2.Load(id);

            }
            catch (System.Runtime.DurableInstancing.InstanceNotReadyException ex)
            {
                System.Diagnostics.Trace.TraceWarning(ex.Message);
                return false;
            }

            var br = app2.ResumeBookmark(bookmarkName, null);

            switch (br)
            {
                case BookmarkResumptionResult.Success:
                    return true;
                case BookmarkResumptionResult.NotFound:
                    throw new InvalidOperationException($"Can not find the bookmark: {bookmarkName}");
                case BookmarkResumptionResult.NotReady:
                    throw new InvalidOperationException($"Bookmark not ready: {bookmarkName}");
                default:
                    throw new InvalidOperationException("hey what's up");
                    ;
            }

        }
    }

}
