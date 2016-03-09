using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.IO;

namespace Fonlow.Activities
{
    public sealed class AsyncFileWriter : AsyncCodeActivity//from msdn example
    {
        public AsyncFileWriter()
            : base()
        {
        }

        /// <summary>
        /// When implemented in a derived class and using the specified execution context, callback method, and user state, enqueues an asynchronous activity in a run-time workflow.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string tempFileName = Path.GetTempFileName();
            System.Diagnostics.Debug.WriteLine("Writing to file: " + tempFileName);

            FileStream file = File.Open(tempFileName, FileMode.Create);

            context.UserState = file;

            byte[] bytes = UnicodeEncoding.Unicode.GetBytes("123456789");
            return file.BeginWrite(bytes, 0, bytes.Length, callback, state);
        }
        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            FileStream file = (FileStream)context.UserState;

            try
            {
                file.EndWrite(result);
                file.Flush();
            }
            finally
            {
                file.Close();
                System.Diagnostics.Debug.WriteLine("Finished writing file");
            }
        }
    }


}
