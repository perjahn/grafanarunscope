using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace runscopesource.Controllers
{
    class TargetMetrics
    {
        public string TargetName { get; set; }
        public Sample[] Samples { get; set; }
    }

    class Sample
    {
        public long Time { get; set; }
        public float Value { get; set; }
    }

    [Produces("application/json")]
    [Route("query")]
    public class QueryController : Controller
    {
        [HttpGet]
        [HttpPost]
        public async Task<JArray> Post([FromBody]JObject value)
        {
            if (value == null || ((dynamic)value).targets == null)
            {
                return null;
            }

            JArray targetsarray = ((dynamic)value).targets;

            var targets = new List<string>();
            foreach (dynamic jobject in targetsarray)
            {
                if (jobject.target != null)
                {
                    string target = jobject.target.Value;

                    if (target.Contains('.'))
                    {
                        targets.Add(target);
                    }
                }
            }

            var targetResults = await GetMetricsAsync(targets.ToArray());

            var result = new JArray();

            foreach (var target in targetResults.OrderBy(t => t.TargetName))
            {
                dynamic serie = new JObject();

                serie.Add(new JProperty("target", target.TargetName));

                dynamic datapoints = new JArray();
                foreach (var sample in target.Samples)
                {
                    var targetSample = new JArray
                    {
                        sample.Value,
                        sample.Time
                    };
                    datapoints.Add(targetSample);
                }
                serie.datapoints = datapoints;

                result.Add(serie);
            }

            return result;
        }

        async Task<TargetMetrics[]> GetMetricsAsync(string[] targets)
        {
            var accessToken = GetRunscopeAccessToken();
            var baseAddress = GetRunscopeBaseAddress();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseAddress);

                var result = new List<TargetMetrics>();

                foreach (var target in targets)
                {
                    var targetResult = new TargetMetrics
                    {
                        TargetName = target
                    };

                    var separatorIndex = target.IndexOf('.');
                    var bucketKey = target.Substring(0, separatorIndex);
                    var testId = target.Substring(separatorIndex + 1);

                    var url = $"/buckets/{bucketKey}/tests/{testId}/metrics";

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);


                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    JArray response_times = ((dynamic)JObject.Parse(content)).response_times;

                    var samples = new List<Sample>();

                    foreach (dynamic response_time in response_times)
                    {
                        samples.Add(new Sample
                        {
                            Time = response_time.timestamp * 1000,
                            Value = response_time.avg_response_time_ms
                        });
                    }

                    targetResult.Samples = samples.ToArray();
                    result.Add(targetResult);
                }

                return result.ToArray();
            }
        }

        string GetRunscopeAccessToken()
        {
            var configfile = "appsettings.json";
            var content = System.IO.File.ReadAllText(configfile);

            dynamic jobject = JObject.Parse(content);

            return jobject.RunscopeAccessToken.Value;
        }

        string GetRunscopeBaseAddress()
        {
            var configfile = "appsettings.json";
            var content = System.IO.File.ReadAllText(configfile);

            dynamic jobject = JObject.Parse(content);

            return jobject.RunscopeBaseAddress.Value;
        }
    }
}
