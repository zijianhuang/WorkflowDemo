﻿using System;
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
    public class WorkflowApplicationTests
    {
        [Fact]
        public void TestWorkflowApplication()
        {
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var a = new System.Activities.Statements.Sequence()
            {
                Activities =
                {
                    new System.Activities.Statements.Delay()
                    {
                        Duration= TimeSpan.FromSeconds(2),
                    },

                    new Multiply()
                    {
                        X = 3,
                        Y = 7,
                    }
                },
            };

            var app = new WorkflowApplication(a);
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            int workFlowThreadId = -1;


            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                workFlowThreadId = Thread.CurrentThread.ManagedThreadId;
                syncEvent.Set();
            };

            var dt = DateTime.Now;
            app.Run();
            Assert.True((DateTime.Now - dt).TotalSeconds < 1, "app.Run() should not be blocking");
            syncEvent.WaitOne();
            Assert.NotEqual(mainThreadId, workFlowThreadId);
        }

        [Fact]
        public void TestWorkflowApplicationCatchException()
        {
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var a = new ThrowSomething();

            var app = new WorkflowApplication(a);
            bool exceptionHandled = false;
            bool aborted = false;
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            int workFlowThreadId = -1;
            app.OnUnhandledException = (e) =>
            {
                Assert.IsType<NotImplementedException>(e.UnhandledException);
                exceptionHandled = true;
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
            Assert.True(exceptionHandled);
            Assert.True(aborted);
            Assert.NotEqual(mainThreadId, workFlowThreadId);
        }

        [Fact]
        public void TestWorkflowApplicationWithoutOnUnhandledException()
        {
            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var a = new ThrowSomething();

            var app = new WorkflowApplication(a);
            bool aborted = false;
            bool completed = false;


            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                completed = true;
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {
                aborted = true;
                syncEvent.Set();
            };
            app.Run();
            syncEvent.WaitOne();
            Assert.True(completed);
            Assert.False(aborted);
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
            throw new NotImplementedException("nothing");
        }
    }



}