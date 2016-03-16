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
using System.Activities.DurableInstancing;
using System.Diagnostics;

namespace BasicTests
{
    public class Persistence
    {
        [Fact]
        public void TestPersistenceNoPersistableIdle()
        {
            var a = new Multiply()
            {
                X = 3,
                Y = 2,
            };

            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var store = new SqlWorkflowInstanceStore("Server =localhost; Initial Catalog = Persistence; Integrated Security = SSPI");

            var app = new WorkflowApplication(a);
            app.InstanceStore = store;
            app.PersistableIdle = (eventArgs) =>
            {
                Assert.True(false);
                return PersistableIdleAction.Persist;
            };

            //None of the handlers should be running
            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Assert.True(true);
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                Assert.True(true);
            };

            app.Run();
            syncEvent.WaitOne();

        }

        /// <summary>
        /// To prove that InstanceStore is lazy.
        /// </summary>
        [Fact]
        public void TestPersistenceNoSqlDefinedNoPersistence()
        {
            var a = new Multiply()
            {
                X = 3,
                Y = 2,
            };

            AutoResetEvent syncEvent = new AutoResetEvent(false);
            var store = new SqlWorkflowInstanceStore("Server =localhost; Initial Catalog = Persistencexxx; Integrated Security = SSPI");

            var app = new WorkflowApplication(a);
            app.InstanceStore = store;
            app.PersistableIdle = (eventArgs) =>
            {
                Assert.True(false);
                return PersistableIdleAction.Persist;
            };

            //None of the handlers should be running
            app.OnUnhandledException = (e) =>
            {
                Assert.True(false);
                return UnhandledExceptionAction.Abort;
            };

            app.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                Assert.True(true);
                syncEvent.Set();
            };

            app.Aborted = (eventArgs) =>
            {
                Assert.True(false);
            };

            app.Unloaded = (eventArgs) =>
            {
                Assert.True(true);
            };

            app.Run();
            syncEvent.WaitOne();

        }


    }
}
