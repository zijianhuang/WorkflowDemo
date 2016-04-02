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
using System.ServiceModel.Activities.Description;

namespace BasicTests
{
    public class WorkflowServiceHostTests
    {
        const string connectionString = "Server =localhost; Initial Catalog = WF; Integrated Security = SSPI";
        const string hostBaseAddress = "net.tcp://localhost/CountingService";
        [Fact]
        public void TestOpenHostWithoutContractImpThrows()
        {
            // Create service host.
            using (WorkflowServiceHost host = new WorkflowServiceHost(new Plus(), new Uri(hostBaseAddress)))
            {

                Assert.Throws<InvalidOperationException>(()
                    => host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), ""));

                Assert.Equal(CommunicationState.Created, host.State);
            }
        }


        [Fact]
        public void TestOpenHost()
        {
            // Create service host.
            WorkflowServiceHost host = new WorkflowServiceHost(new Microsoft.Samples.BuiltInConfiguration.CountingWorkflow(), new Uri(hostBaseAddress));


            // Add service endpoint.
            host.AddServiceEndpoint("ICountingWorkflow", new NetTcpBinding(), "");

            host.Open();
            Assert.Equal(CommunicationState.Opened, host.State);


            // Create a client that sends a message to create an instance of the workflow.
            ICountingWorkflow client = ChannelFactory<ICountingWorkflow>.CreateChannel(new NetTcpBinding(), new EndpointAddress(hostBaseAddress));
            client.start();

            host.Close();
        }

        [Fact]
        public void TestMultiplyXY()
        {
            // Create service host.
            using (WorkflowServiceHost host = new WorkflowServiceHost(new Fonlow.Activities.MultiplyWorkflow2(), new Uri(hostBaseAddress)))
            {
                Debug.WriteLine("host created.");
                // Add service endpoint.
                host.AddServiceEndpoint("ICalculation", new NetTcpBinding(), "");

                host.Open();
                Debug.WriteLine("host opened");
                Assert.Equal(CommunicationState.Opened, host.State);


                // Create a client that sends a message to create an instance of the workflow.
                var client = ChannelFactory<ICalculation>.CreateChannel(new NetTcpBinding(), new EndpointAddress(hostBaseAddress));
                var r = client.MultiplyXY(3, 7);

                Assert.Equal(21, r);
            }
        }

    }

    [ServiceContract]
    public interface ICountingWorkflow
    {
        [OperationContract(IsOneWay = true)]
        void start();
    }


    [ServiceContract(Namespace ="http://fonlow.com/workflowdemo/")]
    public interface ICalculation
    {
        [OperationContract]
        [return: MessageParameter(Name = "Result")]
        long MultiplyXY(int parameter1, int parameter2);

    }

}
