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
    public class WFDefinitionIdentityStore
    {
        private static readonly Lazy<WFDefinitionIdentityStore> lazy = new Lazy<WFDefinitionIdentityStore>(() => new WFDefinitionIdentityStore());

        public static WFDefinitionIdentityStore Instance { get { return lazy.Value; } }

        public WFDefinitionIdentityStore()
        {
            InstanceDefinitions = new System.Collections.Concurrent.ConcurrentDictionary<WorkflowIdentity, byte[]>();

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

        public System.Collections.Concurrent.ConcurrentDictionary<WorkflowIdentity, byte[]> InstanceDefinitions { get; private set; }

        public System.Runtime.DurableInstancing.InstanceStore Store { get; private set; }

        public bool TryAdd(WorkflowIdentity definitionIdentity, Activity a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ActivityPersistenceHelper.SaveActivity(a, stream);
                stream.Position = 0;
                return InstanceDefinitions.TryAdd(definitionIdentity, stream.ToArray());
            }

        }

        public bool TryAdd<T>(WorkflowIdentity definitionIdentity, ActivityBuilder<T> ab)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ActivityPersistenceHelper.SaveActivity(ab, stream);
                stream.Position = 0;
                return InstanceDefinitions.TryAdd(definitionIdentity, stream.ToArray());
            }

        }

        public Activity this[WorkflowIdentity definitionIdentity]
        {
            get
            {
                return ActivityPersistenceHelper.LoadActivity(WFDefinitionIdentityStore.Instance.InstanceDefinitions[definitionIdentity]);
            }
        }
    }

}