using System.Reflection;
using Autofac;

namespace TfsCreateBuild
{
    class Program
    {
        static int Main(string[] args)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                            .AsImplementedInterfaces();

            var container = containerBuilder.Build();
            return container.Resolve<IBuildCreator>().Execute(args);
        }
    }
}
