using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;

namespace Fonlow.Activities
{
    /// <summary>
    /// Caller to provide a bookmark value and return BookmarkValue+"ABC"
    /// </summary>
    public sealed class ReadLine : NativeActivity<string>
    {
        public ReadLine()
        {
        }

        public InArgument<string> BookmarkName { get; set; }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            string name = this.BookmarkName.Get(context);

            if (name == null)
            {
                throw new ArgumentException(string.Format("ReadLine {0}: BookmarkName cannot be null", this.DisplayName), "BookmarkName");
            }

            context.CreateBookmark(name, new BookmarkCallback(OnReadComplete));
        }

        void OnReadComplete(NativeActivityContext context, Bookmark bookmark, object state)
        {
            string input = state as string;

            if (input == null)
            {
                throw new ArgumentException("ReadLine {0}: ReadLine must be resumed with a non-null string");
            }

            context.SetValue(base.Result, input + "ABC");
        }
    }

    public sealed class Wakeup : NativeActivity
    {
        public Wakeup()
        {
        }

        public InArgument<string> BookmarkName { get; set; }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            string name = this.BookmarkName.Get(context);

            if (name == null)
            {
                throw new ArgumentException(string.Format("ReadLine {0}: BookmarkName cannot be null", this.DisplayName), "BookmarkName");
            }

            context.CreateBookmark(name);
        }

    }

    public sealed class WaitForSignalOrAlarm : Activity<bool>
    {
        public InArgument<DateTime> AlarmTime { get; set; }

        public InArgument<string> BookmarkName { get; set; }


        public WaitForSignalOrAlarm()
        {
            Implementation = () =>
                new Pick()
                {
                    Branches = {
                        new PickBranch
                        {
                            Trigger = new Wakeup()
                            {
                                BookmarkName=new InArgument<string>((c)=> BookmarkName.Get(c))
                            },

                            Action = new Assign<bool>()
                            {
                                To= new ArgumentReference<bool> { ArgumentName = "Result" },
                                Value= true,
                            }
                        },
                        new PickBranch
                        {
                            Trigger = new Delay
                            {
                                Duration = new InArgument<TimeSpan>((c)=> GetDuration(AlarmTime.Get(c)))
                            },
                            Action = new Assign<bool>()
                            {
                                To=new ArgumentReference<bool> { ArgumentName = "Result" },
                                Value= false,
                            }
                        }
                        }

                };

        }

        static TimeSpan GetDuration(DateTime alarmTime)
        {
            if (alarmTime < DateTime.Now)
                return TimeSpan.Zero;

            return alarmTime - DateTime.Now;
        }
    }

}
