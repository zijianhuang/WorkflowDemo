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
    public class AsyncCodeActivityTests
    {
        [Fact]
        public void TestAsyncDoSomethingInSequence()
        {
            System.Diagnostics.Debug.WriteLine("TestAsyncDoSomethingInSequence");
            var a = new AsyncDoSomethingAndWait();
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
        public void TestAsyncDoSomethingNotWaitInSequence()
        {
            System.Diagnostics.Debug.WriteLine("TestAsyncDoSomethingNotWaitInSequence");
            var a = new AsyncDoSomethingNotWait();
            var s = new System.Activities.Statements.Sequence()
            {
                Activities = {
                    new Plus() {X=2, Y=3 },
                    a,
                    new Multiply() {X=3, Y=7 },
                },
            };

            var r = WorkflowInvoker.Invoke(s);
            System.Diagnostics.Debug.WriteLine("After AsyncDoSomethingNotWait in Sequence invoke");
            //check the log file, the invoker will just run 3 activities one by one, and waiting for a to finish, though the key function of a is running in a new thread
            System.Threading.Thread.Sleep(1100);
        }

        [Fact]
        public void TestAsyncHttpGetInSequence()
        {
            System.Diagnostics.Debug.WriteLine("TestAsyncHttpGetInSequence2");
            var a = new AsyncHttpGet() { Uri = "http://fonlow.com" };
            var s = new System.Activities.Statements.Sequence()
            {
                Activities = {
                    new WriteLine() {Text="Before AsyncHttpGet", TextWriter=new InArgument<System.IO.TextWriter>((c)=> new Fonlow.Utilities.TraceWriter()) },
                    a,
                    new WriteLine() {Text="After AsyncHttpGet", TextWriter=new InArgument<System.IO.TextWriter>((c)=> new Fonlow.Utilities.TraceWriter()) },
                },
            };

            var r = WorkflowInvoker.Invoke(s);
            System.Diagnostics.Debug.WriteLine("After AsyncHttpGet in Sequence invoke");
            //check the log file, the invoker will just run 3 activities one by one, and waiting for a to finish, though the key function of a is running in a new thread
        }


    }


}
