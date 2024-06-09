using System.IO;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;

namespace az_104_app_insert
{
    public static class Function1
    {
        [FunctionName("QuestionInsert")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Azure ADîFèÿópÇÃéëäièÓïÒÇéÊìæ
            string tenantId = Environment.GetEnvironmentVariable("SQL_TENANT_ID");
            string clientId = Environment.GetEnvironmentVariable("SQL_CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable(
                
                "SQL_CLIENT_SECRET");

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            string[] scopes = new string[] { "https://database.windows.net/.default" };
            AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            string accessToken = result.AccessToken;

            // ê⁄ë±ï∂éöóÒÇÃç\íz
            string server = Environment.GetEnvironmentVariable("SQL_SERVER");
            string database = Environment.GetEnvironmentVariable("SQL_DATABASE");
            string connectionString = $"Server={server};Database={database};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";


            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.AccessToken = accessToken;
                    await conn.OpenAsync();

                    if (req.Method == HttpMethods.Post)
                    {
                        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                        dynamic data = JsonConvert.DeserializeObject(requestBody);
                        int categoryId = data?.CategoryID;
                        string questionText = data?.QuestionText;
                        string answerExplanation = data?.AnswerExplanation;
                        int? imageId = data?.ImageID;

                        string query = "INSERT INTO Questions (CategoryID, QuestionText, AnswerExplanation, ImageID) " +
                                       "VALUES (@CategoryID, @QuestionText, @AnswerExplanation, @ImageID)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@CategoryID", categoryId);
                            cmd.Parameters.AddWithValue("@QuestionText", questionText);
                            cmd.Parameters.AddWithValue("@AnswerExplanation", answerExplanation ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ImageID", imageId.HasValue ? (object)imageId.Value : DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        return new OkObjectResult("Data inserted successfully");
                    }
                    else
                    {
                        return new BadRequestObjectResult("Invalid HTTP method");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error during database connection: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
