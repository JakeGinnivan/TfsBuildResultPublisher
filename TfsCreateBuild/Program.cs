namespace TfsCreateBuild
{
    class Program
    {
        static int Main(string[] args)
        {
            return new BuildCreator().CreateBuild(args);
        }
    }
}
