using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;

namespace Fonlow.Activities
{
    public class DateToYMD1 : CodeActivity //classic way of return results
    {
        protected override void Execute(CodeActivityContext context)
        {
            var v= Date.Get(context);
            Y.Set(context, v.Year);
            M.Set(context, v.Month);
            D.Set(context, v.Day);
        }

        public InArgument<DateTime> Date { get; set; }

        public OutArgument<int> Y { get; set; }

        public OutArgument<int> M { get; set; }

        public OutArgument<int> D { get; set; }
    }


    public class YMD
    {
        public int Y { get; set; }

        public int M { get; set; }

        public int D { get; set; }
    }

    public class DateToYMD2 : CodeActivity<YMD> //strongly typed with a data container class defined in advance
    {
        protected override YMD Execute(CodeActivityContext context)
        {
            var v = Date.Get(context);
            Y.Set(context, v.Year);
            M.Set(context, v.Month);
            D.Set(context, v.Day);
            return new YMD()
            {
                Y = v.Year,
                M = v.Month,
                D = v.Day
            };
        }

        public InArgument<DateTime> Date { get; set; }

        public OutArgument<int> Y { get; set; }

        public OutArgument<int> M { get; set; }

        public OutArgument<int> D { get; set; }
    }


    public class DateToYMD3 : CodeActivity<YMD> //strongly typed with a data container class defined in advance
    {
        protected override YMD Execute(CodeActivityContext context)
        {
            var v = Date.Get(context);
            var r= new YMD()
            {
                Y = v.Year,
                M = v.Month,
                D = v.Day
            };
            YMD.Set(context, r);
            return r;
        }

        public InArgument<DateTime> Date { get; set; }

        public OutArgument<YMD> YMD { get; set; }
    }


    public class DateToYMD4 : CodeActivity<Tuple<int, int, int>> //strongly typed with Tuple
    {
        protected override Tuple<int, int, int> Execute(CodeActivityContext context)
        {
            var v = Date.Get(context);
            Y.Set(context, v.Year);
            M.Set(context, v.Month);
            D.Set(context, v.Day);
            return new Tuple<int, int, int>(v.Year, v.Month, v.Day);
        }

        public InArgument<DateTime> Date { get; set; }

        public OutArgument<int> Y { get; set; }

        public OutArgument<int> M { get; set; }

        public OutArgument<int> D { get; set; }
    }

}
