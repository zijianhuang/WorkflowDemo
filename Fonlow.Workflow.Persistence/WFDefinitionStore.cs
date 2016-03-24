using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.Activities.XamlIntegration;
using System.IO;


namespace Fonlow.Activities
{
    class WFDefinitionStore
    {
    }

    /// <summary>
    /// Persist activity for purpose of resuming later.
    /// </summary>
    /// <remarks>The functions here are for bookmark and long running only, not for general purpose.</remarks>
    /// <example>https://msdn.microsoft.com/en-us/library/ff458319%28v=vs.110%29.aspx</example>
    public static class ActivityPersistenceHelper
    {
        public static void SaveActivity(Activity activity, string name, Stream stream)
        {
            var ab = new ActivityBuilder()
            {
                Name = name,
                Implementation = activity,
            };


            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 512, true))
            using (var xw = ActivityXamlServices.CreateBuilderWriter(new System.Xaml.XamlXmlWriter(streamWriter, new System.Xaml.XamlSchemaContext())))
            {
                System.Xaml.XamlServices.Save(xw, ab);
            }
        }

        //public static void SaveActivity<T>(Activity<T> activity, string name, Stream stream)
        //{
        //    var ab = new ActivityBuilder<T>()
        //    {
        //        Name = name,
        //        Implementation = activity,
        //    };


        //    using (var streamWriter = new StreamWriter(stream))
        //    using (var xw = ActivityXamlServices.CreateBuilderWriter(new System.Xaml.XamlXmlWriter(streamWriter, new System.Xaml.XamlSchemaContext())))
        //    {
        //        System.Xaml.XamlServices.Save(xw, ab);
        //    }
        //}


        public static DynamicActivity LoadActivity(Stream stream)
        {
            var settings = new ActivityXamlServicesSettings()
            {
                CompileExpressions = true,
            };

            var activity = ActivityXamlServices.Load(stream, settings) as DynamicActivity;
            return activity;
        }

        //public static DynamicActivity<T> LoadActivity<T>(Stream stream)
        //{
        //    var settings = new ActivityXamlServicesSettings()
        //    {
        //        CompileExpressions = true,
        //    };

        //    var activity = ActivityXamlServices.Load(stream, settings) as DynamicActivity<T>;
        //    return activity;
        //}


    }
}
