namespace TfsBuildResultPublisher
{
    public interface ITeamCityBuildInfoFetch
    {
        void UpdateConfigurationFromTeamCityBuild(Configuration configuration);
    }
}