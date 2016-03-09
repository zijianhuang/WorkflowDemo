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
        public void TestPlus()
        {
            var a = new Plus()
            {
                X = 1,
                Y = 2,
            };

            var dic = WorkflowInvoker.Invoke(a);
            Assert.Equal(3, (int)dic["Z"]);
        }

        [Fact]
        public void TestPlusWithDicAsInput()
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
        public void TestMultiply()
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

        public class ThrowSomething : CodeActivity
        {
            protected override void Execute(CodeActivityContext context)
            {
                throw new ArgumentException("nothing");
            }
        }

        [Fact]
        public void TestWorkflowApplicationCatchException()
        {
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var a = new ThrowSomething();

            var app = new WorkflowApplication(a);
            bool exceptionHappened = false;
            bool aborted = false;
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            int workFlowThreadId=-1;
            app.OnUnhandledException = (e) =>
            {
                Assert.IsType<ArgumentException>(e.UnhandledException);
                exceptionHappened = true;
                workFlowThreadId = Thread.CurrentThread.ManagedThreadId;
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Assert.True(false);
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

            Assert.Throws<ArgumentException>(() => app.Run());//exception occurs during validation and in the same thread of the caller.

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
        public void TestDateToYMD4()
        {
            var a = new DateToYMD4()
            {
                Date = new DateTime(2016, 12, 23)
            };
            var r = WorkflowInvoker.Invoke(a);
            Assert.Equal(2016, r.Item1);
            Assert.Equal(12, r.Item2);
            Assert.Equal(23, r.Item3);
        }


        [Fact]
        public void TestDynamicActivity()
        {
            var x = 100;
            var y = 200;
            var a = new DynamicActivity
            {
                DisplayName = "Dynamic Plus",
                Properties =
                {
                    new DynamicActivityProperty()
                    {
                        Name="XX",
                        Type= typeof(InArgument<int>),
                        //Value=x, //This must not be done. Otherwise, System.InvalidCastException : Unable to cast object of type 'System.Int32' to type 'System.Activities.Argument'
                        //The MSDN example is not working at least in WF 4.5. 

                    },
                    new DynamicActivityProperty()
                    {
                        Name="YY",
                        Type=typeof(InArgument<int>),
                        //Value=y,
                    },
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
                            plus,

                            new System.Activities.Statements.Assign<int>
                            {
                                To = new ArgumentReference<int> { ArgumentName = "ZZ" },
                                Value = new InArgument<int>(env=> t1.Get(env)),
                            },

                        },
                    };
                    return s;
                },


            };

            var dic = new Dictionary<string, object>();
            dic.Add("XX", x);
            dic.Add("YY", y);

            var r = WorkflowInvoker.Invoke(a, dic);
            //          var r = WorkflowInvoker.Invoke(a);
            Assert.Equal(300, (int)r["ZZ"]);
        }

        [Fact]
        public void TestDynamicActivityGeneric()
        {
            var x = 100;
            var y = 200;
            var a = new DynamicActivity<int>
            {
                DisplayName = "Dynamic Plus",
                Properties =
                {
                    new DynamicActivityProperty()
                    {
                        Name="XX",
                        Type= typeof(InArgument<int>),

                    },
                    new DynamicActivityProperty()
                    {
                        Name="YY",
                        Type=typeof(InArgument<int>),
                    },

                },

                Implementation = () =>
                {
                    var t1 = new Variable<int>("t1");

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
                            plus,
                            new System.Activities.Statements.Assign<int>
                            {
                                To = new ArgumentReference<int> { ArgumentName="Result" },//I just had a good guess about how Result get assigned.
                                Value = new InArgument<int>(env=> t1.Get(env)),
                            },


                        },
                    };
                    return s;
                },


            };

            var dic = new Dictionary<string, object>();
            dic.Add("XX", x);
            dic.Add("YY", y);

            var r = WorkflowInvoker.Invoke(a, dic);
            Assert.Equal(300, r);
        }

        [Fact]
        public void TestDynamicActivityGenericWithResult()
        {
            var x = 100;
            var y = 200;
            var a = new DynamicActivity<long>
            {
                DisplayName = "Dynamic Plus",
                Properties =
                {
                    new DynamicActivityProperty()
                    {
                        Name="XX",
                        Type= typeof(InArgument<int>),

                    },
                    new DynamicActivityProperty()
                    {
                        Name="YY",
                        Type=typeof(InArgument<int>),
                    },

                },

                Implementation = () =>
                {
                    var t1 = new Variable<long>("t1");

                    var multiply = new Multiply()
                    {
                        X = new ArgumentValue<int>() { ArgumentName = "XX" },
                        Y = new ArgumentValue<int>() { ArgumentName = "YY" },
                        Result = t1,
                    };
                    var s = new System.Activities.Statements.Sequence()
                    {
                        Variables =
                        {
                            t1
                        },
                        Activities = {
                            multiply,
                            new System.Activities.Statements.Assign<long>
                            {
                                To = new ArgumentReference<long> { ArgumentName="Result" },//I just had a good guess about how Result get assigned.
                                Value = new InArgument<long>(env=> t1.Get(env)),
                            },


                        },
                    };
                    return s;
                },


            };

            var dic = new Dictionary<string, object>();
            dic.Add("XX", x);
            dic.Add("YY", y);

            var r = WorkflowInvoker.Invoke(a, dic);
            Assert.Equal(20000, r);
        }

        [Fact]
        public void TestDynamicActivityGenericWithGeneric()
        {
            var x = 100;
            var y = 200;
            var a = new DynamicActivity<long>
            {
                DisplayName = "Dynamic Multiply",
                Properties =
                {
                    new DynamicActivityProperty()
                    {
                        Name="XX",
                        Type= typeof(InArgument<long>),

                    },
                    new DynamicActivityProperty()
                    {
                        Name="YY",
                        Type=typeof(InArgument<long>),
                    },

                },

                Implementation = () =>
                {
                    var t1 = new Variable<long>("t1");

                    var multiply = new System.Activities.Expressions.Multiply<long, long, long>()
                    {
                        Left = new ArgumentValue<long>() { ArgumentName = "XX" },
                        Right = new ArgumentValue<long>() { ArgumentName = "YY" },
                        Result = t1,
                    };
                    var s = new System.Activities.Statements.Sequence()
                    {
                        Variables =
                        {
                            t1
                        },
                        Activities = {
                            multiply,
                            new System.Activities.Statements.Assign<long>
                            {
                                To = new ArgumentReference<long> { ArgumentName="Result" },
                                Value = new InArgument<long>(env=> t1.Get(env)),
                            },


                        },
                    };
                    return s;
                },


            };

            var dic = new Dictionary<string, object>();
            dic.Add("XX", x);
            dic.Add("YY", y);

            var r = WorkflowInvoker.Invoke(a, dic);
            Assert.Equal(20000L, r);
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
        public void TestAsyncFileWriter()
        {
            var a = new AsyncFileWriter();
            var r = WorkflowInvoker.Invoke(a);
            System.Diagnostics.Debug.WriteLine("AsyncFileWriter invoke");
            //check the log file, AsyncFileWriter is not doing anything async. all run in the same thread. Probably 
        }

        [Fact]
        public void TestInvokeMethod()
        {
            var a = new InvokeMethod<string>()
            {
                MethodName = "GetSomething",
                TargetType = this.GetType(),
            };

            var r = WorkflowInvoker.Invoke(a);//run in the same thread
            System.Diagnostics.Debug.WriteLine("Something invoke");
            Assert.Equal("Something", r);

        }

        [Fact]
        public void TestInvokeMethodAsync()
        {
            var a = new InvokeMethod<string>()
            {
                MethodName = "GetSomething",
                TargetType = this.GetType(),
                RunAsynchronously = true,
            };

            var r = WorkflowInvoker.Invoke(a);//run in a new thread, however, wait for it finished.
            System.Diagnostics.Debug.WriteLine("Something invoke");
            Assert.Equal("Something", r);

        }

        public static string GetSomething()
        {
            System.Threading.Thread.Sleep(200);
            System.Diagnostics.Debug.WriteLine("Something");
            return "Something";
        }

        [Fact]
        public void TestOverloadGroupThrows()
        {
            var a = new CreateLocation()
            {
                Latitude = 100,
                Longitude = 200,
                Street = "abc"
            };

            Assert.Throws<ArgumentException>(() => WorkflowInvoker.Invoke(a));
        }
    }


    public class CreateLocation : Activity
    {
        [RequiredArgument]
        public InArgument<string> Name { get; set; }

        public InArgument<string> Description { get; set; }

        [RequiredArgument]
        [OverloadGroup("G1")]
        public InArgument<int> Latitude { get; set; }

        [RequiredArgument]
        [OverloadGroup("G1")]
        public InArgument<int> Longitude { get; set; }

        [RequiredArgument]
        [OverloadGroup("G2")]
        [OverloadGroup("G3")]
        public InArgument<string> Street { get; set; }

        [RequiredArgument]
        [OverloadGroup("G2")]
        public InArgument<string> City { get; set; }

        [RequiredArgument]
        [OverloadGroup("G2")]
        public InArgument<string> State { get; set; }

        [RequiredArgument]
        [OverloadGroup("G3")]
        public InArgument<int> Zip { get; set; }
    }
}
