namespace TfsBuildResultPublisher
{
    public interface IBuildInvoker
    {
        string TriggerBuildAndWaitForCompletion(string serverPath, string project, string buildDefinition);
    }
}