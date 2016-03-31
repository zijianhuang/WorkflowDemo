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
    public class InvokeMethodTests
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
        public void TestInvokeStaticMethodMissingThrows()
        {
            var a = new InvokeMethod<int>()
            {
                MethodName = "GetSomethingMissing",
                TargetType = this.GetType(),
            };

            Assert.Throws<InvalidWorkflowException>(() => WorkflowInvoker.Invoke(a));
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


        [Fact]
        public void TestInvokeMethod()
        {
            var a = new InvokeMethod<string>()
            {
                MethodName = "DoSomething",
                TargetObject = new InArgument<InvokeMethodTests>(c => this),
                Parameters = { new InArgument<string>("Abcd") },
            };

            var r = WorkflowInvoker.Invoke(a);//method GetSomething() run in the same thread
            System.Diagnostics.Debug.WriteLine("Something invoke");
            Assert.Equal("Abcd", r);
        }

        [Fact]
        public void TestInvokeMethodMissingparametersThrows()
        {
            var a = new InvokeMethod<string>()
            {
                MethodName = "DoSomething",
                TargetObject = new InArgument<InvokeMethodTests>(c => this),
            };

            Assert.Throws<InvalidWorkflowException>(() => WorkflowInvoker.Invoke(a));
        }

        public string DoSomething(string s)
        {
            System.Threading.Thread.Sleep(200);
            System.Diagnostics.Debug.WriteLine("DoSomething");
            return s;

        }

        public static int GetSomething()
        {
            System.Threading.Thread.Sleep(200);
            System.Diagnostics.Debug.WriteLine("Something");
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public static void ThrowException()
        {
            throw new InvalidProgramException("Just a funky test");
        }

        [Fact]
        public void TestInvokeStaticMethodThatThrows()
        {
            var a = new System.Activities.Statements.InvokeMethod()
            {
                MethodName = "ThrowException",
                TargetType = this.GetType(),
            };

            Assert.Throws<InvalidProgramException>(() => WorkflowInvoker.Invoke(a));
        }

        [Fact]
        public void TestInvokeStaticMethodAsyncThatThrows()
        {
            var a = new System.Activities.Statements.InvokeMethod()
            {
                MethodName = "ThrowException",
                TargetType = this.GetType(),
                RunAsynchronously=true,
            };

            Assert.Throws<InvalidProgramException>(() => WorkflowInvoker.Invoke(a));
        }



        [Fact]
        public void TestDynamicActivityWithInvokeMethod()
        {
            var a = new DynamicActivity
            {
                DisplayName = "Dynamic Plus",
                Properties =
                {
                    new DynamicActivityProperty()
                    {
                        Name="ZZ",
                        Type=typeof(OutArgument<int>),
                    }

                },

                Implementation = () =>
                {
                    Variable<int> t1 = new Variable<int>("t1");

                    var plus = new Plus()
                    {
                        X = new ArgumentValue<int>() { ArgumentName = "XX" },
                        Y = new ArgumentValue<int>() { ArgumentName = "YY" },
                        Z = t1,
                    };

                    var s = new System.Activities.Statements.Sequence()
                    {
                        Variables =
                        {
                            t1
                        },
                        Activities = {
                            new InvokeMethod<string>()
                            {
                                MethodName = "DoSomething",
                                TargetObject = new InArgument<InvokeMethodTests>(c => this),
                                Parameters = { new InArgument<string>("Abcd") },

                            },

                            new InvokeMethod<int>()
                            {
                                MethodName = "GetSomething",
                                TargetType = this.GetType(),
                                Result= t1,
                            },

                    new System.Activities.Statements.Assign<int>
                            {
                                To = new ArgumentReference<int> { ArgumentName = "ZZ" },//So the Value will be assigned to property ZZ. Noted that ArgumentReference<> is a CodeActivity<>
                                Value = new InArgument<int>(env=> t1.Get(env)),  //So the Value  will be wired from t1 in context.
                            },

                        },
                    };
                    return s;
                },


            };


            var r = WorkflowInvoker.Invoke(a);
            var threadId = (int)r["ZZ"];
            Assert.Equal(System.Threading.Thread.CurrentThread.ManagedThreadId, threadId);
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
