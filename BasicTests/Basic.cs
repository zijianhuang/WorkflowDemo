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

    public class WorkflowApplicationTests
    {
        [Fact]
        public void TestWorkflowApplicationCatchException()
        {
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var a = new ThrowSomething();

            var app = new WorkflowApplication(a);
            bool exceptionHappened = false;
            bool aborted = false;
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            int workFlowThreadId = -1;
            app.OnUnhandledException = (e) =>
            {
                Assert.IsType<ArgumentException>(e.UnhandledException);
                exceptionHappened = true;
                workFlowThreadId = Thread.CurrentThread.ManagedThreadId;
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Assert.True(false, "Never completed");
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {
                aborted = true;
                syncEvent.Set();
            };
            app.Run();
            syncEvent.WaitOne();
            Assert.True(exceptionHappened);
            Assert.True(aborted);
            Assert.NotEqual(mainThreadId, workFlowThreadId);
        }

        [Fact]
        public void TestWorkflowApplicationNotCatchExceptionWhenValidatingArguments()
        {
            var a = new Multiply()
            {
                Y = 2,
            };

            var app = new WorkflowApplication(a);

            //None of the handlers should be running
            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Assert.True(false);
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                Assert.True(false);
            };
            
            Assert.Throws<ArgumentException>(() => app.Run());//exception occurs during validation and in the same thread of the caller, before any activity runs.

        }


    }

    public class ThrowSomething : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            throw new ArgumentException("nothing");
        }
    }

    public class InvokeMethodTest
    {
        [Fact]
        public void TestInvokeStaticMethod()
        {
            var a = new InvokeMethod<int>()
            {
                MethodName = "GetSomething",
                TargetType = this.GetType(),
            };

            var r = WorkflowInvoker.Invoke(a);//method GetSomething() run in the same thread
            System.Diagnostics.Debug.WriteLine("Something invoke");
            Assert.Equal(System.Threading.Thread.CurrentThread.ManagedThreadId, r);
        }

        [Fact]
        public void TestInvokeStaticMethodAsync()
        {
            var a = new InvokeMethod<int>()
            {
                MethodName = "GetSomething",
                TargetType = this.GetType(),
                RunAsynchronously = true,
            };

            var r = WorkflowInvoker.Invoke(a);//run in a new thread, however, wait for it finished.
            System.Diagnostics.Debug.WriteLine("Something invoke");
            Assert.NotEqual(System.Threading.Thread.CurrentThread.ManagedThreadId, r);

        }

        [Fact]
        public void TestInvokeStaticMethodAsyncInSequence()
        {
            var t1 = new Variable<int>("t1");
            var a = new InvokeMethod<int>()
            {
                MethodName = "GetSomething",
                TargetType = this.GetType(),
                RunAsynchronously = true,
                Result = t1,
            };

            var s = new System.Activities.Statements.Sequence()
            {
                Variables = { t1 },
                Activities = {
                    new Plus() {X=2, Y=3 },
                    a,
                    new Multiply() {X=3, Y=7 },
                },
                
            };

            
            var r = WorkflowInvoker.Invoke(s); 
            System.Diagnostics.Debug.WriteLine("Something invoke");
            //So all run in sequences. The async activity is not being executed in fire and forget style, but probably just good not freezing the UI thread if UI is involved.

        }

        public static int GetSomething()
        {
            System.Threading.Thread.Sleep(200);
            System.Diagnostics.Debug.WriteLine("Something");
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public string DoSomething()
        {
            System.Threading.Thread.Sleep(200);
            System.Diagnostics.Debug.WriteLine("DoSomething");
            return "DoSomething";

        }



    }

    public class AsyncCodeActivityTests
    {
        [Fact]
        public void TestAsyncDoSomethingInSequence()
        {
            System.Diagnostics.Debug.WriteLine("Before AsyncDoSomething in Sequence invoke");
            var a = new AsyncDoSomething();
            var s = new System.Activities.Statements.Sequence()
            {
                Activities = {
                    new Plus() {X=2, Y=3 },
                    a,
                    new Multiply() {X=3, Y=7 },
                },
            };

            var r = WorkflowInvoker.Invoke(s);
            System.Diagnostics.Debug.WriteLine("After AsyncDoSomething in Sequence invoke");
            //check the log file, the invoker will just run 3 activities one by one, and waiting for a to finish, though the key function of a is running in a new thread
        }

        [Fact]
        public void TestAsyncDoSomething()
        {
            System.Diagnostics.Debug.WriteLine("Before AsyncDoSomething invoke");
            var a = new AsyncDoSomething();
            var r = WorkflowInvoker.Invoke(a);
            System.Diagnostics.Debug.WriteLine("After AsyncDoSomething invoke");
            //check the log file, BeginExecute and EndExecute run in the caller thread.
        }

        [Fact]
        public void TestAsyncDoSomethingInApplication()
        {
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var a = new AsyncDoSomething();
            var s = new System.Activities.Statements.Sequence()
            {
                Activities = {
                    new Plus() {X=2, Y=3 },
                    a,
                    new Multiply() {X=3, Y=7 },
                },
            };


            var app = new WorkflowApplication(s);
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            int workFlowThreadId = -1;
            app.OnUnhandledException = (e) =>
            {
                Assert.IsType<ArgumentException>(e.UnhandledException);
                workFlowThreadId = Thread.CurrentThread.ManagedThreadId;
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                System.Diagnostics.Debug.WriteLine("Completed");
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {
                syncEvent.Set();
            };
            app.Run();
            syncEvent.WaitOne();
            Assert.NotEqual(mainThreadId, workFlowThreadId);
        }

    }

}
