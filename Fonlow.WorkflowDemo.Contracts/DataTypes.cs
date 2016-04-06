using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace Fonlow.WorkflowDemo.Contracts
{
    public static class Constants
    {
        public const string ContractNamespace = "http://fonlow.com/FonlowDemo/2016/03";
    }

    [DataContract(Namespace =Constants.ContractNamespace)]
    public enum TriangleType
    {
        [EnumMember]
        Error,
        [EnumMember]
        Scalene,
        [EnumMember]
        Isosceles,
        [EnumMember]
        Equilateral
    };

}
