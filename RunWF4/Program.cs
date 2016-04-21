using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using Fonlow.Activities;
using System.Activities.Statements;

namespace RunWF4
{
    class Program
    {
        static void Main(string[] args)
        {
            //var a = new AsyncHttpGet() { Uri = "http://fonlow.com" };
            //var s = new System.Activities.Statements.Sequence()
            //{
            //    Activities = {
            //        new WriteLine() {Text="Before AsyncHttpGet" },
            //        a,
            //        new WriteLine() {Text="After AsyncHttpGet" },
            //    },
            //};

            //System.Activities.WorkflowInvoker.Invoke(s);
            System.Activities.WorkflowInvoker.Invoke(new AsyncHttpGetWF());

            Console.ReadLine();
        }
    }
}
