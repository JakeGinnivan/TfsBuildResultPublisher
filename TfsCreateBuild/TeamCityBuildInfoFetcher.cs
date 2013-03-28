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

            using (var handler = new HttpClientHandler { Credentials = new NetworkCredential(configuration.TeamCityUserId, configuration.TeamCityPassword) })
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = null;
                try
                {
                    
                    response = client.GetAsync(string.Format("{0}/httpAuth/app/rest/builds/id:{1}", configuration.TeamCityServerAddress, configuration.TeamCityBuildId)).Result;
                    var result = response.Content.ReadAsAsync<dynamic>().Result;

                    configuration.BuildNumber = configuration.BuildNumber ?? result.number;
                    configuration.BuildStatus = configuration.BuildStatus ?? ToTfsStatus(result.status);
                    configuration.BuildDefinition = configuration.BuildDefinition ?? result.buildType.name;
                    configuration.StartTime = configuration.StartTime ?? ToDateTimeFromIsoDate(result.startDate);
                    configuration.FinishTime = configuration.FinishTime ?? ToDateTimeFromIsoDate(result.finishDate);
                }
                catch (Exception ex)
                {
                    if (response != null)
                    {
                        Console.WriteLine("Unable to fetch build information from TeamCity");
                        Console.WriteLine();
                        Console.WriteLine("Exception:");
                        Console.WriteLine(ex.ToString());
                        Console.WriteLine();
                        Console.WriteLine("Content:");
                        Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                        Console.WriteLine();
                        throw;
                    }
                }
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