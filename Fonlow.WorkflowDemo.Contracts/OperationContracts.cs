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
        long CalculateFibonacciSeries(long n);
    }



    [ServiceContract(Namespace = Constants.ContractNamespace)]
    public interface IBookService
    {
        [OperationContract]
        void Buy(string bookName);

        [OperationContract(IsOneWay = true)]
        void Checkout();
    }

    [ServiceContract(Namespace = Constants.ContractNamespace)]
    public interface IWakeup
    {
        [OperationContract]
        Guid Create(string bookmarkName, TimeSpan duration);

        [OperationContract]
        bool Wakeup(string bookmarkName);
    }

}
