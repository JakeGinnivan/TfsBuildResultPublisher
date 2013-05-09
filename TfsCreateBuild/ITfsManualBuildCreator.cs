namespace TfsBuildResultPublisher
{
    public interface ITfsManualBuildCreator
    {
        void CreateManualBuild(string buildStatus, string collection, string buildLog, string dropPath, string buildFlavour, string localPath, string buildPlatform, string buildTarget, string project, string buildDefinition, bool createBuildDefinitionIfNotExists, string buildController, string buildNumber, string serverPath, bool keepForever);
    }
}