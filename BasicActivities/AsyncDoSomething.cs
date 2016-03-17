using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.IO;

namespace Fonlow.Activities
{

    public class AsyncDoSomething : AsyncCodeActivity
    {

        int DoSomething()
        {
            System.Threading.Thread.Sleep(1100);
            System.Diagnostics.Trace.TraceInformation("Do AsyncDoSomething");
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            Func<int> d = () => DoSomething();
            return d.BeginInvoke(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            result.AsyncWaitHandle.WaitOne();
        }
    }


}
