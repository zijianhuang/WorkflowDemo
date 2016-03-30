using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;

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

            context.SetValue(base.Result, input+"ABC");
        }
    }

    public class LongRunning : CodeActivity
    {
        [RequiredArgument]
        public InArgument<int> Seconds { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(Seconds.Get(context)));
        }
    }
}
