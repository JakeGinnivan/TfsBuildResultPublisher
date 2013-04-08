namespace TfsCreateBuild
{
    public interface IBuildCreator
    {
        int Execute(string[] args);
    }
}