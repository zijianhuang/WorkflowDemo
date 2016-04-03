using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace Fonlow.Activities
{

    public sealed class GetWorkflowInstanceId : CodeActivity<Guid>
    {
        protected override Guid Execute(CodeActivityContext context)
        {
            return context.WorkflowInstanceId;
        }
    }
}
