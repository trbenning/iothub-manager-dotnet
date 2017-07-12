// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services;
using Microsoft.Azure.IoTSolutions.IotHubManager.Services.Runtime;
using Microsoft.Azure.IoTSolutions.IotHubManager.WebService.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Config = Microsoft.Azure.IoTSolutions.IotHubManager.WebService.Runtime.Config;

namespace Microsoft.Azure.IoTSolutions.IotHubManager.WebService
{
    public class DependencyResolution
    {
        /// <summary>
        /// Autofac configuration. Find more information here:
        /// http://docs.autofac.org/en/latest/integration/owin.html
        /// http://autofac.readthedocs.io/en/latest/register/scanning.html
        /// </summary>
        public static IContainer Setup(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            builder.Populate(services);

            AutowireAssemblies(builder);
            SetupCustomRules(builder);

            var container = builder.Build();
            RegisterFactory(container);

            return container;
        }

        /// <summary>
        /// Autowire interfaces to classes from all the assemblies, to avoid
        /// manual configuration. Note that autowiring works only for interfaces
        /// with just one implementation.
        /// </summary>
        private static void AutowireAssemblies(ContainerBuilder builder)
        {
            var assembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            // Auto-wire additional assemblies
            assembly = typeof(IServicesConfig).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
        }

        /// <summary>Setup Custom rules overriding autowired ones.</summary>
        private static void SetupCustomRules(ContainerBuilder builder)
        {
            // Make sure the configuration is read only once.
            var config = new Config(new ConfigData());
            builder.RegisterInstance(config).As<IConfig>().SingleInstance();

            // Service configuration is generated by the entry point, so we
            // prepare the instance here.
            builder.RegisterInstance(config.ServicesConfig).As<IServicesConfig>().SingleInstance();

            // By default Autofac uses a request lifetime, creating new objects
            // for each request, which is good to reduce the risk of memory
            // leaks, but not so good for the overall performance.
            // TODO: revisit when migrating to ASP.NET Core.
            builder.RegisterType<Services.Devices>().As<IDevices>().SingleInstance();
            builder.RegisterType<DeviceTwins>().As<IDeviceTwins>().SingleInstance();
        }

        private static void RegisterFactory(IContainer container)
        {
            Factory.RegisterContainer(container);
        }


        //// <summary>
        /// Provide factory pattern for dependencies that are instantiated
        /// multiple times during the application lifetime.
        /// How to use:
        ///
        /// class MyClass : IMyClass {
        ///     public MyClass(DependencyInjection.IFactory factory) {
        ///         this.factory = factory;
        ///     }
        ///     public SomeMethod() {
        ///         var instance1 = this.factory.Resolve<ISomething>();
        ///         var instance2 = this.factory.Resolve<ISomething>();
        ///         var instance3 = this.factory.Resolve<ISomething>();
        ///     }
        /// }
        /// </summary>
        public interface IFactory
        {
            T Resolve<T>();
        }

        public class Factory : IFactory
        {
            private static IContainer container;

            public static void RegisterContainer(IContainer c)
            {
                container = c;
            }

            public T Resolve<T>()
            {
                return container.Resolve<T>();
            }
        }
    }

}
