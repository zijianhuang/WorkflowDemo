using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.Activities.XamlIntegration;
using System.IO;
using System.Activities.DurableInstancing;



namespace Fonlow.Activities
{
    public class WFDefinitionIdentityFactory
    {
        private static readonly Lazy<WFDefinitionIdentityFactory> lazy = new Lazy<WFDefinitionIdentityFactory>(() => new WFDefinitionIdentityFactory());

        public static WFDefinitionIdentityFactory Instance { get { return lazy.Value; } }

        public WFDefinitionIdentityFactory()
        {
            InstanceDefinitions = new System.Collections.Concurrent.ConcurrentDictionary<WorkflowIdentity, Activity>();

            Store = new SqlWorkflowInstanceStore("Server =localhost; Initial Catalog = WF; Integrated Security = SSPI")
            {
                InstanceCompletionAction = InstanceCompletionAction.DeleteAll,
                InstanceEncodingOption = InstanceEncodingOption.GZip,

            };

            var handle = Store.CreateInstanceHandle();
            var view = Store.Execute(handle, new CreateWorkflowOwnerCommand(), TimeSpan.FromSeconds(50));
            handle.Free();
            Store.DefaultInstanceOwner = view.InstanceOwner;

        }

        public System.Collections.Concurrent.ConcurrentDictionary<WorkflowIdentity, Activity> InstanceDefinitions { get; private set; }

        public System.Runtime.DurableInstancing.InstanceStore Store { get; private set; }

        public bool TryAdd(WorkflowIdentity definitionIdentity, Activity a)
        {
            return InstanceDefinitions.TryAdd(definitionIdentity, a);
        }

        public Activity this[WorkflowIdentity definitionIdentity]
        {
            get
            {
                Activity activity = null;
                var found = InstanceDefinitions.TryGetValue(definitionIdentity, out activity);
                if (found)
                    return activity;

                var assemblyFullName = definitionIdentity.Package;
                var activityTypeName = definitionIdentity.Name;
                System.Diagnostics.Trace.Assert(assemblyFullName.Contains(definitionIdentity.Version.ToString()));
                var objectHandle=  Activator.CreateInstance(assemblyFullName, activityTypeName);//tons of exceptions needed to be handled in production
                activity = objectHandle.Unwrap() as Activity;
                if (activity==null)
                {
                    throw new InvalidOperationException("You must have been crazy.");
                }

                InstanceDefinitions.TryAdd(definitionIdentity, activity);
                return activity;

            }
        }

        public static WorkflowIdentity GetWorkflowIdentity(Activity activity)
        {
            var type = activity.GetType();
            var name = type.FullName;
            var assembly = type.Assembly;
            var package = assembly.FullName;
            var version = assembly.GetName().Version;
            return new WorkflowIdentity(name, version, package);
        }
    }

}