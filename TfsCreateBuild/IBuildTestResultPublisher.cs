namespace TfsBuildResultPublisher
{
    public interface IBuildTestResultPublisher
    {
        bool PublishTestResultsToBuild(string collection, string testResultsFile, string project, string buildNumber, string buildPlatform, string buildFlavour);
    }
}