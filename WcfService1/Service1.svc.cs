using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Activities;
using System.Diagnostics;

namespace WcfService1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        public bool RunWaitOrDelay(int seconds)
        {
            var a = new Fonlow.Activities.WaitOrDelay()
            {
                DelaySeconds=seconds,
            };


            var app = new WorkflowApplication(a);
            app.InstanceStore = Fonlow.Activities.WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.None;
            };

            app.OnUnhandledException = (e) =>
            {
                // 
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Trace.TraceInformation("WaitOrDelay completed.");
            };

            app.Aborted = (eventArgs) =>
            {
                // 
            };

            app.Unloaded = (eventArgs) =>
            {
                // 
            };

            var id = app.Id;
            app.Run();
            
            Trace.TraceInformation("WaitOrDelayFired");
            return true;
        }

    }
}
