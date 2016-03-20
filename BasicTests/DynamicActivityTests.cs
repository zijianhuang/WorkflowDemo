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
    public class DynamicActivityTests
    {
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
                        Value=new InArgument<int>(x),
                        //You can't do Value=x, otherwise, System.InvalidCastException : Unable to cast object of type 'System.Int32' to type 'System.Activities.Argument'

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
                                To = new ArgumentReference<int> { ArgumentName = "ZZ" },//So the Value will be assigned to property ZZ. Noted that ArgumentReference<> is a CodeActivity<>
                                Value = new InArgument<int>(env=> t1.Get(env)),  //So the Value  will be wired from t1 in context.
                            },

                        },
                    };
                    return s;
                },


            };

            var dic = new Dictionary<string, object>();
           // dic.Add("XX", x);
            dic.Add("YY", y);

            var r = WorkflowInvoker.Invoke(a, dic);
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
                        Z = t1,  //So Output Z will be assigned to t1
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

    }


}
