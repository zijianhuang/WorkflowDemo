using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace BasicWFServiceClientApi
{
    /// <summary>
    /// http://webandlife.blogspot.com/2012/08/proper-disposal-of-wcf-channels-against.html
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>Better to put this into another component</remarks>
    public class SafeChannel<T> : IDisposable where T : ICommunicationObject, IDisposable
    {
        public SafeChannel(T channel)
        {
            Instance = channel;
        }
        public static IDisposable AsDisposable(T client)
        {
            return new SafeChannel<T>(client);
        }
        public T Instance { get; private set; }
        bool disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Close();
                }
                this.disposed = true;
            }
        }
        void Close()
        {
            bool success = false;
            try
            {
                if (Instance.State != CommunicationState.Faulted)
                {
                    Instance.Close();
                    success = true;
                }
            }
            finally
            {
                if (!success)
                    Instance.Abort();
            }
        }
    }


    public class WorkflowProxy : SafeChannel<Fonlow.WorkflowDemo.Clients.ServiceClient>
    {
        public WorkflowProxy(string endpointName)
            : base(new Fonlow.WorkflowDemo.Clients.ServiceClient(endpointName))
        {

        }
    }
}
