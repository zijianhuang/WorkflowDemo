using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace BasicWFService
{
    [ServiceContract(Namespace = "http://fonlow.com/WorkflowDemo")]
    public interface IBookService
    {
        [OperationContract]
        void Buy(string bookName);

        [OperationContract(IsOneWay = true)]
        void Checkout();
    }
}
