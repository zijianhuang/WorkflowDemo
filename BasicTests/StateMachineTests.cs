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
using System.IO;
using System.Activities.Statements;
using System.Activities.Tracking;


namespace BasicTests
{
    public class StateMachineTests
    {

        [Fact]
        public void TestStateMachineWorkflow()
        {
            var a = new TurnstileStateMachine();

            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.None; //Must be None so 1 application instance will do all states
            };

            var id = app.Id;

  //          var stp = new StatusTrackingParticipant();
  //          app.Extensions.Add(stp);

            app.Run();

            Thread.Sleep(200); //Run and ResumeBookmark are all non blocking asynchronous calls, better to wait prior operation to finish.

            var br = app.ResumeBookmark("coin", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
       //     Assert.Equal("Unlocked", stp.StateName);

            br = app.ResumeBookmark("coin", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
        //    Assert.Equal("Unlocked", stp.StateName);

            br = app.ResumeBookmark("push", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
        //    Assert.Equal("Locked", stp.StateName);

            br = app.ResumeBookmark("push", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
       //     Assert.Equal("Locked", stp.StateName);

            br = app.ResumeBookmark("funky", null);
            Assert.Equal(BookmarkResumptionResult.NotFound, br);
            Thread.Sleep(200);
      //      Assert.Equal("Locked", stp.StateName);

            br = app.ResumeBookmark("coin", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);//ResumeBookmark is asynchrounous in a new thread, so better to wait, otherwise they got killed when app.Cancel is executed.
    //        Assert.Equal("Unlocked", stp.StateName);


            app.Cancel();
        }

        [Fact]
        public void TestTurnstile()
        {
            var lockedState = new State() { DisplayName = "Locked" };
            var unlockedState = new State() { DisplayName = "Unlocked" };

            var insertCoinWhenLocked = new Transition()
            {
                To = unlockedState,
                Trigger = new Wakeup() { BookmarkName = "coin" },
            };

            var insertCoinWhenUnlocked = new Transition()
            {
                To = unlockedState,
                Trigger = new Wakeup() { BookmarkName = "coin" },
            };

            var pushWhenLocked = new Transition()
            {
                To = lockedState,
                Trigger = new Wakeup() { BookmarkName = "push" },
            };

            var pushWhenUnlocked = new Transition()
            {
                To = lockedState,
                Trigger = new Wakeup() { BookmarkName = "push" },
            };

            lockedState.Transitions.Add(pushWhenLocked);
            lockedState.Transitions.Add(insertCoinWhenLocked);

            unlockedState.Transitions.Add(pushWhenUnlocked);
            unlockedState.Transitions.Add(insertCoinWhenUnlocked);

            var a = new StateMachine()
            {
                InitialState = lockedState,
            };

            a.States.Add(lockedState);
            a.States.Add(unlockedState);


            var app = new WorkflowApplication(a);
            app.InstanceStore = WFDefinitionStore.Instance.Store;
            app.PersistableIdle = (eventArgs) =>
            {
                return PersistableIdleAction.None; //Must be None so 1 application instance will do all states
            };


            var id = app.Id;

            app.Extensions.Add(new Fonlow.Utilities.TraceWriter());

            var stp = new StatusTrackingParticipant();
            app.Extensions.Add(stp);

            app.Run();

            Thread.Sleep(200); //Run and ResumeBookmark are all non blocking asynchronous calls, better to wait prior operation to finish.

            var br = app.ResumeBookmark("coin", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
            Assert.Equal("Unlocked", stp.StateName);

            br = app.ResumeBookmark("coin", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
            Assert.Equal("Unlocked", stp.StateName);

            br = app.ResumeBookmark("push", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
            Assert.Equal("Locked", stp.StateName);

            br = app.ResumeBookmark("push", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);
            Assert.Equal("Locked", stp.StateName);

            br = app.ResumeBookmark("funky", null);
            Assert.Equal(BookmarkResumptionResult.NotFound, br);
            Thread.Sleep(200);
            Assert.Equal("Locked", stp.StateName);

            br = app.ResumeBookmark("coin", null);
            Assert.Equal(BookmarkResumptionResult.Success, br);
            Thread.Sleep(200);//ResumeBookmark is asynchrounous in a new thread, so better to wait, otherwise they got killed when app.Cancel is executed.
            Assert.Equal("Unlocked", stp.StateName);


            app.Cancel();
        }

    }


    public class StatusTrackingParticipant : System.Activities.Tracking.TrackingParticipant
    {
        public string StateName { get; private set; }

        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            var stateMachineStateRecord = record as System.Activities.Statements.Tracking.StateMachineStateRecord;
            if (stateMachineStateRecord == null)
                return;

            StateName = stateMachineStateRecord.StateName;
            Trace.TraceInformation("StateName: " + stateMachineStateRecord.StateName);
        }
    }
}
