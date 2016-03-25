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

namespace BasicTests
{
    public class Basic
    {
        [Fact]
        public void TestPlusWithDicOutput()
        {
            var a = new Plus()
            {
                X = 1,
                Y = new InArgument<int>(2),
            };

            var dic = WorkflowInvoker.Invoke(a);
            Assert.Equal(3, (int)dic["Z"]);
        }

        [Fact]
        public void TestPlusWithDicInput()
        {
            var a = new Plus();

            var inputs = new Dictionary<string, object>()
            {
                {"X", 1 },
                {"Y", 2 }
            };

            var dic = WorkflowInvoker.Invoke(a, inputs);
            Assert.Equal(3, (int)dic["Z"]);
        }

        [Fact]
        public void TestPlusWithDefaultValue()
        {
            var a = new Plus()
            {
                Y = 2, //X not assigned, thus will have the default value 0 when being invoked.
            };

            Assert.Null(a.X);
            var dic = WorkflowInvoker.Invoke(a);
            Assert.Equal(2, (int)dic["Z"]);
            Assert.NotNull(a.X);
        }


        [Fact]
        public void TestMultiplyWithTypedOutput()
        {
            var a = new Multiply()
            {
                X = 3,
                Y = 2,
            };

            var r = WorkflowInvoker.Invoke(a);
            Assert.Equal(6, r);
        }


        [Fact]
        public void TestMultiplyMissingRequiredThrows()
        {
            var a = new Multiply()
            {
                //           X = 3,
                Y = 2,
            };

            Assert.Throws<ArgumentException>(() => WorkflowInvoker.Invoke(a));
        }

        [Fact]
        public void TestDateToYMD1()
        {
            var a = new DateToYMD1()
            {
                Date = new DateTime(2016, 12, 23)
            };
            var dic = WorkflowInvoker.Invoke(a);
            Assert.Equal(2016, (int)dic["Y"]);
            Assert.Equal(12, (int)dic["M"]);
            Assert.Equal(23, (int)dic["D"]);

        }

        [Fact]
        public void TestDateToYMD2()
        {
            var a = new DateToYMD2()
            {
                Date = new DateTime(2016, 12, 23)
            };
            var r = WorkflowInvoker.Invoke(a);
            Assert.Equal(2016, r.Y);
            Assert.Equal(12, r.M);
            Assert.Equal(23, r.D);
        }

        [Fact]
        public void TestDateToYMD3()
        {
            var a = new DateToYMD3()
            {
                Date = new DateTime(2016, 12, 23)
            };
            var r = WorkflowInvoker.Invoke(a);
            Assert.Equal(2016, r.Item1);
            Assert.Equal(12, r.Item2);
            Assert.Equal(23, r.Item3);
        }



        [Fact]
        public void TestMultiplyGeneric()
        {
            var a = new System.Activities.Expressions.Multiply<long, long, long>()
            {
                Left = 100,
                Right = 200,
            };

            var r = WorkflowInvoker.Invoke(a);
            Assert.Equal(20000L, r);

        }

        /// <summary>
        /// Multiply want all types the same.
        /// </summary>
        [Fact]
        public void TestMultiplyGenericThrows()
        {
            Assert.Throws<InvalidWorkflowException>(() =>
            {
                var a = new System.Activities.Expressions.Multiply<int, int, long>()
                {
                    Left = 100,
                    Right = 200,
                };

                var r = WorkflowInvoker.Invoke(a);
            });

        }

        /// <summary>
        /// Multiply<> want all types the same. It seem either bug or design defect. If not bug, then it is better of to have 1 generic type.
        /// </summary>
        [Fact]
        public void TestMultiplyGenericThrows2()
        {
            Assert.Throws<InvalidWorkflowException>(() =>
            {
                var a = new System.Activities.Expressions.Multiply<int, long, long>()
                {
                    Left = 100,
                    Right = 200L,
                };

                var r = WorkflowInvoker.Invoke(a);
            });

        }

        [Fact]
        public void TestOverloadGroup()
        {
            var a = new QuerySql()
            {
                ConnectionString = "cccc",
            };

            var r = WorkflowInvoker.Invoke(a);

        }
        [Fact]
        public void TestOverloadGroupWithBothGroupsAssignedThrows()
        {
            var a = new QuerySql()
            {
                ConnectionString = "cccc",
                Host = "localhost"
            };

            Assert.Throws<ArgumentException>(() => WorkflowInvoker.Invoke(a));
        }
    }


    public class QuerySql : CodeActivity
    {

        [RequiredArgument]
        [OverloadGroup("G1")]
        public InArgument<string> ConnectionString { get; set; }


        [RequiredArgument]
        [OverloadGroup("G2")]
        public InArgument<string> Host { get; set; }

        [OverloadGroup("G2")]
        public InArgument<string> Database { get; set; }

        [OverloadGroup("G2")]
        public InArgument<string> User { get; set; }

        [OverloadGroup("G2")]
        public InArgument<string> Password { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            //do nothing here
        }
    }



}
