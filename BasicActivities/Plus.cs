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
            return r;
        }

        public InArgument<int> X { get; set; }

        public InArgument<int> Y { get; set; }

        /// <summary>
        /// This is compiled however useless since there's no interface to access since the dictionary is unavailable to the output.
        /// So in production codes, OutArgument should not be defined.
        /// </summary>
        public OutArgument<long> Z { get; set; }

    }
}
