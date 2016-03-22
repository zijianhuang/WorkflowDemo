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
using System.ServiceModel;
using System.ServiceModel.Activities;

namespace BasicTests
{
    public class WorkflowServiceHostTests
    {
            //const string connectionString = "Data Source=.\\SQLEXPRESS;Initial Catalog=InstanceStore;Integrated Security=True;Asynchronous Processing=True";
            const string hostBaseAddress = "net.tcp://localhost/CountingService";
        [Fact]
        public void TestOpenHostWithoutStoreThrows()
        {
            // Create service host.
            WorkflowServiceHost host = new WorkflowServiceHost(new Plus(), new Uri(hostBaseAddress));

            // Add service endpoint.
            host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), "");

            // Open service host.
            host.Open();

        }
    }

        [ServiceContract]
        public interface ICountingWorkflow
        {
            [OperationContract(IsOneWay = true)]
            void start();
        }

    }
