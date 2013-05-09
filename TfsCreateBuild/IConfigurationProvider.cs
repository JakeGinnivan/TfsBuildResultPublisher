namespace TfsBuildResultPublisher
{
    public interface IConfigurationProvider
    {
        bool TryProvide(string[] args, out Configuration configuration);
    }
}