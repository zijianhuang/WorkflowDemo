using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;


namespace Fonlow.Activities
{
    /// <summary>
    /// simple X+Y=Z. Old style CodeActivity
    /// </summary>
    public class Plus : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            Z.Set(context, X.Get(context) + Y.Get(context));
            System.Diagnostics.Debug.WriteLine("Plus Done");
        }

        public InArgument<int> X { get; set; }

        public InArgument<int> Y { get; set; }

        public OutArgument<int> Z { get; set; }//client code accesses Z through dictionary
    }

    /// <summary>
    /// Simple X*Y=Z. CodeActivity with typed output through generic.
    /// </summary>
    public class Multiply : CodeActivity<long>
    {
        protected override long Execute(CodeActivityContext context)
        {
            var r= X.Get(context) * Y.Get(context);
            Z.Set(context, r);
            System.Diagnostics.Debug.WriteLine("Multiply done");
            return r;
        }

        [RequiredArgument]//https://msdn.microsoft.com/en-us/library/ee358733%28v=vs.110%29.aspx
        public InArgument<int> X { get; set; }

        [RequiredArgument]
        public InArgument<int> Y { get; set; }

        /// <summary>
        /// This is compiled however in production codes, OutArgument should not be defined.
        /// </summary>
        public OutArgument<long> Z { get; set; }

    }

    public class Person
    {
        public string Surname { get; set; }

        public string GivenName { get; set; }
    }
}
