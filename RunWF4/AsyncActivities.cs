using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.IO;
using System.Net;

namespace Fonlow.Activities
{

    public class AsyncDoSomethingAndWait : AsyncCodeActivity
    {

        int DoSomething()
        {
            System.Threading.Thread.Sleep(1100);
            System.Diagnostics.Trace.TraceInformation("Do AsyncDoSomethingAndWait");
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

    public class AsyncDoSomethingNotWait : AsyncCodeActivity
    {

        int DoSomething()
        {
            System.Threading.Thread.Sleep(3100);
            System.Diagnostics.Trace.TraceInformation("Do AsyncDoSomethingNotWait");
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            Func<int> d = () => DoSomething();
            return d.BeginInvoke(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            //not WaitOne
        }
    }


    public class AsyncHttpGet : AsyncCodeActivity<string>
    {
        public InArgument<string> Uri { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
       //     WebRequest request = HttpWebRequest.Create(this.Uri.Get(context));
            WebRequest request = HttpWebRequest.Create("http://fonlow.com");
            context.UserState = request;
            return request.BeginGetResponse(callback, state);
        }

        protected override string EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            WebRequest request = context.UserState as WebRequest;
            using (WebResponse response = request.EndGetResponse(result))
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    var s = reader.ReadToEnd();
                    Console.WriteLine(s);
                    System.Diagnostics.Trace.TraceInformation(s);
                    return s;
                }
            }
        }
    }


}
