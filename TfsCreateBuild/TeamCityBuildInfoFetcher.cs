using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace TfsCreateBuild
{
    public class TeamCityBuildInfoFetcher
    {
        public void UpdateConfigurationFromTeamCityBuild(Configuration configuration)
        {
            ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);

            var userName = Environment.GetEnvironmentVariable("teamcity.auth.userId", EnvironmentVariableTarget.User);
            var password = Environment.GetEnvironmentVariable("teamcity.auth.password", EnvironmentVariableTarget.User);
            using (var handler = new HttpClientHandler { Credentials = new NetworkCredential(userName, password) })
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.GetAsync(string.Format("{0}/httpAuth/app/rest/builds/id:{1}", configuration.TeamCityServerAddress, configuration.TeamCityBuildId)).Result;
                var result = response.Content.ReadAsAsync<dynamic>().Result;

                configuration.BuildNumber = configuration.BuildNumber ?? result.number;
                configuration.BuildStatus = configuration.BuildStatus ?? ToTfsStatus(result.status);
                configuration.BuildDefinition = configuration.BuildDefinition ?? result.buildType.name;
                configuration.StartTime = configuration.StartTime ?? ToDateTimeFromIsoDate(result.startDate);
                configuration.FinishTime = configuration.FinishTime ?? ToDateTimeFromIsoDate(result.finishDate);
            }
        }

        private static DateTime ToDateTimeFromIsoDate(string date)
        {
            return DateTime.ParseExact(date, @"yyyyMMdd\THHmmsszzzz", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }

        private static string ToTfsStatus(string status)
        {
            switch (status)
            {
                case "SUCCESS/FAILURE/ERROR":
                    return "Succeeded";
                case "FAILURE":
                case "ERROR":
                    return "Failed";
                default:
                    return "Succeeded"; // Hrrmm?
            }
        }
    }
}