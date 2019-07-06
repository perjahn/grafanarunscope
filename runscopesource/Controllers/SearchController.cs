using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace runscopesource.Controllers
{
    [Produces("application/json")]
    [Route("search")]
    public class SearchController : Controller
    {
        [HttpGet]
        [HttpPost]
        public async Task<JArray> Post([FromBody]JObject value)
        {
            var tests = await GetTestsAsync();

            var result = new JArray();

            foreach (var testId in tests.Keys
                .Where(k => IsParameterValid(value, tests[k]))
                .OrderBy(k => tests[k]))
            {
                dynamic jobject = new JObject();
                jobject.text = tests[testId];
                jobject.value = testId;

                result.Add(jobject);
            }

            return result;
        }

        bool IsParameterValid(JObject jobject, string testName)
        {
            if (jobject == null)
            {
                return true;
            }

            if (((dynamic)jobject).target == null)
            {
                return true;
            }

            if (((dynamic)jobject).target.Value != null &&
                (jobject.Property("target").Value.ToString() == string.Empty || jobject.Property("target").Value.ToString() == testName))
            {
                return true;
            }

            return false;
        }

        async Task<Dictionary<string, string>> GetTestsAsync()
        {
            var accessToken = GetRunscopeAccessToken();
            var baseAddress = GetRunscopeBaseAddress();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseAddress);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var listResponse = await client.GetAsync("/buckets");
                listResponse.EnsureSuccessStatusCode();
                var content = await listResponse.Content.ReadAsStringAsync();

                JArray buckets = ((dynamic)JObject.Parse(content)).data;

                var result = new Dictionary<string, string>();

                var tasks = new List<Task<HttpResponseMessage>>();

                foreach (var bucket in buckets)
                {
                    string tests_url = ((dynamic)bucket).tests_url.Value;

                    var uriBuilder = new UriBuilder(tests_url)
                    {
                        Query = "count=10000"
                    };

                    tasks.Add(client.GetAsync(uriBuilder.Uri));
                }

                foreach (var buckettask in buckets.Zip(tasks, (bucket, task) => new { bucket, task }))
                {
                    string bucketname = ((dynamic)buckettask.bucket).name.Value;
                    string bucketkey = ((dynamic)buckettask.bucket).key.Value;

                    HttpResponseMessage response = await buckettask.task;
                    response.EnsureSuccessStatusCode();
                    content = await response.Content.ReadAsStringAsync();

                    JArray tests = ((dynamic)JObject.Parse(content)).data;

                    foreach (var test in tests)
                    {
                        var testname = ((dynamic)test).name.Value;
                        var testid = $"{bucketkey}.{((dynamic)test).id.Value}";

                        var fullname = $"{GetCleanName(bucketname)}.{GetCleanName(testname)}";

                        result[testid] = fullname;
                    }
                }

                return result;
            }
        }

        string GetCleanName(string s)
        {
            var sb = new StringBuilder();

            foreach (var c in s.ToArray())
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
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
