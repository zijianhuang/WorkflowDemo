using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace Fonlow.Activities
{
    /// <summary>
    /// For workflow that need to return the InstanceId to client.
    /// </summary>
    public sealed class GetWorkflowInstanceId : CodeActivity<Guid>
    {
        protected override Guid Execute(CodeActivityContext context)
        {
            return context.WorkflowInstanceId;
        }
    }
}
