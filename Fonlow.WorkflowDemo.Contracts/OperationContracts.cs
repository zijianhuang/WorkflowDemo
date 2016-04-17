using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Fonlow.WorkflowDemo.Contracts
{
    [ServiceContract(Namespace = Constants.ContractNamespace)]
    public interface IMath
    {
        [OperationContract]
        [return: System.ServiceModel.MessageParameterAttribute(Name = "GetTriangleTypeResult")]
        TriangleType GetTriangleType(int side1, int side2, int side3);

        [OperationContract]
        [return: System.ServiceModel.MessageParameterAttribute(Name = "CalculateFibonacciSeriesResult")]
        long CalculateFibonacciSeries(long n);
    }



    [ServiceContract(Namespace = Constants.ContractNamespace)]
    public interface IBookService
    {
        [OperationContract]
        void Buy(int customerId, string bookName);

        [OperationContract]
        [return: System.ServiceModel.MessageParameterAttribute(Name = "CheckoutResult")]
        string Checkout(int customerId);

        [OperationContract]
        void Pay(int customerId, string paymentDetail);
    }

    [ServiceContract(Namespace = Constants.ContractNamespace)]
    public interface IWakeup
    {
        [OperationContract]
        [return: System.ServiceModel.MessageParameterAttribute(Name = "CreateResult")]
        Guid Create(string bookmarkName, TimeSpan duration);

        [OperationContract]
        [return: System.ServiceModel.MessageParameterAttribute(Name = "WakerupResult")]
        bool Wakeup(string bookmarkName);
    }

}
