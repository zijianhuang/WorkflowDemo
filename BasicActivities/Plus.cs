using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;


namespace Fonlow.Activities
{
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

        public OutArgument<long> Z { get; set; }//client code accesses Z through Execute.

    }
}
