
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Data.SqlClient;
using Dapper;

namespace LogIngestApi
{
    public static class LogIngestApi
    {
        [FunctionName("LogIngest")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {
            log.LogInformation($"function initiated! RequestUri={req.Path}");

            //Very important tasks here :)
            //.....
            //.....

            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent;

            using (var reader = new StreamReader(req.Body))
            {
                jsonContent = reader.ReadToEnd();
            }
            
            string userName = null;

            try
            {
                dynamic data = JsonConvert.DeserializeObject(jsonContent);

                //If there is no username, we return the error message.
                if (data.userName == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }

                userName = data.userName;
            }
            catch (Exception e)
            {
                userName = "Anonymous";
            }

            //Azure SQLDB Log 
            var logAdded = true;
            try
            {

                //We get the Connection String in the Function App Settings section we defined.
                var connectionString = Environment.GetEnvironmentVariable("SqlConnection");


                using (var connection = new SqlConnection(connectionString))
                {
                    //Opens Azure SQL DB connection.
                    connection.Open();

                    var logMessage = $"Function called by {userName} on {DateTime.UtcNow} .";

                    // Insert Log to database.
                    connection.Execute("INSERT INTO [dbo].[LogRequest] ([Log]) VALUES (@logMessage)", new { logMessage }); 
                    //connection.Execute("INSERT INTO [dbo].[Logs] ([LogMessage], [CreateDate]) VALUES (@logMessage, @createDate)", new { logMessage, createDate = DateTime.UtcNow });
                    log.LogInformation("Log record was successfully added to the database!");
                }
            }
            catch(Exception e)
            {
                throw e;
                logAdded = false;
            }

            // We complete our function. According to its success status, it will display the message.
            return !logAdded
                 ? new HttpResponseMessage(HttpStatusCode.BadRequest)
                 : new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
