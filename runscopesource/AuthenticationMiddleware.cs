using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace runscopesource
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                var encodedCredential = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                var credertial = encoding.GetString(Convert.FromBase64String(encodedCredential));

                var separatorIndex = credertial.IndexOf(':');

                var username = credertial.Substring(0, separatorIndex);
                var password = credertial.Substring(separatorIndex + 1);

                var (Username, Password) = GetCredentials();

                if (username == Username && password == Password)
                {
                    await _next.Invoke(context);
                }
                else
                {
                    context.Response.StatusCode = 401;
                }
            }
            else
            {
                context.Response.StatusCode = 401;
            }
        }

        (string Username, string Password) GetCredentials()
        {
            var configfile = "appsettings.json";
            var content = File.ReadAllText(configfile);

            dynamic jobject = JObject.Parse(content);

            return (jobject.Username.Value, jobject.Password.Value);
        }
    }
}
