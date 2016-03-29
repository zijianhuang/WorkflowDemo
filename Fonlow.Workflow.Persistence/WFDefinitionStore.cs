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
    public class WFDefinitionStore
    {
        private static readonly Lazy<WFDefinitionStore> lazy = new Lazy<WFDefinitionStore>(() => new WFDefinitionStore());

        public static WFDefinitionStore Instance { get { return lazy.Value; } }

        public WFDefinitionStore()
        {
            InstanceDefinitions = new System.Collections.Concurrent.ConcurrentDictionary<Guid, byte[]>();

            Store = new SqlWorkflowInstanceStore("Server =localhost; Initial Catalog = WF; Integrated Security = SSPI")
            {
                InstanceCompletionAction = InstanceCompletionAction.DeleteAll,
                InstanceEncodingOption = InstanceEncodingOption.GZip,

            };
        }

        public System.Collections.Concurrent.ConcurrentDictionary<Guid, byte[]> InstanceDefinitions { get; private set; }

        public System.Runtime.DurableInstancing.InstanceStore Store { get; private set; }

        public bool TryAdd(Guid instanceId, Activity a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ActivityPersistenceHelper.SaveActivity(a, stream);
                stream.Position = 0;
                return InstanceDefinitions.TryAdd(instanceId, stream.ToArray());
            }

        }

        public bool TryAdd<T>(Guid instanceId, ActivityBuilder<T> ab)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ActivityPersistenceHelper.SaveActivity(ab, stream);
                stream.Position = 0;
                return InstanceDefinitions.TryAdd(instanceId, stream.ToArray());
            }

        }

        public Activity this[Guid id] { get
            {
                return ActivityPersistenceHelper.LoadActivity(WFDefinitionStore.Instance.InstanceDefinitions[id]);
            } }
    }

    /// <summary>
    /// Persist activity for purpose of resuming later.
    /// </summary>
    /// <remarks>The functions here are for bookmark and long running only, not for general purpose.</remarks>
    /// <example>inspired by https://msdn.microsoft.com/en-us/library/ff458319%28v=vs.110%29.aspx</example>
    public static class ActivityPersistenceHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="stream">External XML UTF8 stream. Caller is responsible to set position if the stream supports random seek, and to dispose.</param>
        public static void SaveActivity(Activity activity, Stream stream)
        {
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 512, true))
            using (var xw = ActivityXamlServices.CreateBuilderWriter(new System.Xaml.XamlXmlWriter(streamWriter, new System.Xaml.XamlSchemaContext())))
            {
                System.Xaml.XamlServices.Save(xw, activity);
            }
        }

        public static void SaveActivity<T>(ActivityBuilder<T> ab, Stream stream)
        {
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 512, true))
            using (var xw = ActivityXamlServices.CreateBuilderWriter(new System.Xaml.XamlXmlWriter(streamWriter, new System.Xaml.XamlSchemaContext())))
            {
                System.Xaml.XamlServices.Save(xw, ab);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">XAML stream defining an Activity</param>
        /// <returns></returns>
        public static Activity LoadActivity(Stream stream)
        {
            var settings = new ActivityXamlServicesSettings()
            {
                CompileExpressions = true,
            };

            var activity = ActivityXamlServices.Load(stream, settings);
            return activity;
        }

        public static Activity LoadActivity(byte[] bytes)
        {
            var settings = new ActivityXamlServicesSettings()
            {
                CompileExpressions = true,
            };

            using (var stream = new MemoryStream(bytes))
            {
                var activity = ActivityXamlServices.Load(stream, settings);
                return activity;
            }
        }


        public static System.Runtime.DurableInstancing.InstanceStore NewStore(TimeSpan  timeout)
        {
            var store = new SqlWorkflowInstanceStore("Server =localhost; Initial Catalog = WF; Integrated Security = SSPI")
            {
                
            };

            var handle = store.CreateInstanceHandle();
            var view = store.Execute(handle, new CreateWorkflowOwnerCommand(), timeout);
            handle.Free();
            store.DefaultInstanceOwner = view.InstanceOwner;
            return store;
        }


    }
}
