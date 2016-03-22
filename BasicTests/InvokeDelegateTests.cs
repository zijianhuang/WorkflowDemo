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
using System.Activities.Statements;

namespace BasicTests
{
    public class InvokeDelegateTests
    {
        [Fact]
        public void TestActivityDelegate()
        {
            var da1 = new DelegateInArgument<int>();
            var da2 = new DelegateInArgument<int>();
            var a = new MyCallbakActivity()
            {
                XX = 3,
                YY = 7,
                Callback = new ActivityFunc<int, int, long>()
                {
                    Argument1= da1,
                    Argument2=da2,
                    Handler = new Multiply()
                    {
                        X = new InArgument<int>(da1),
                        Y = new InArgument<int>(da2),
                    },
                },
            };

            var r = WorkflowInvoker.Invoke(a);
            Assert.Equal(21, r);
        }


    }

    /// <summary>
    /// This activity expects and external activity with certain signature.
    /// </summary>
    public class MyCallbakActivity : Activity<long>
    {
        /// <summary>
        /// Activity delegate
        /// </summary>
        public ActivityFunc<int, int, long> Callback { get; set; }

        public InArgument<int> XX { get; set; }
        public InArgument<int> YY { get; set; }

        Variable<long> v = new Variable<long>("v");

        public MyCallbakActivity()
        {
            Implementation = () => new Sequence()
            {
                Variables = { v },
                Activities =
                {
                    //To wrap the activity delegate
                    new InvokeFunc<int, int, long>
                    {
                        Func=Callback,
                        Argument1= new InArgument<int>(env=>XX.Get(env)),//Must wire with local InArgument this way.
                        Argument2= new InArgument<int>(env=>YY.Get(env)),
                        Result=  v, //buffer the callback Result in v
                    },

                    //To wire the callback Result with the local Result.
                    new Assign<long>
                    {
                        To=new ArgumentReference<long> { ArgumentName="Result" },
                        Value=v,
                    }
                },
            };
        }
    }
}
