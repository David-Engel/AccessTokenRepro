using Microsoft.Identity.Client;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;

namespace AccessTokenRepro
{
    class Program
    {
        private static string ClientId = "<your client ID>";
        private static string Tenant = "<your tenant ID>";
        private static string ClientSecret = "<your client secret>";
        private static string[] scopes = new string[] { "https://database.windows.net//.default" };
        private static string AzureSqlDbConnectionString = "Server=tcp:<your Azure SQL DB>.database.windows.net,1433;Initial Catalog=TestApp;Persist Security Info=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        static void Main(string[] args)
        {
            var observer = new SqlClientDiagnosticObserver();
            IDisposable subscription = DiagnosticListener.AllListeners.Subscribe(observer);

            try
            {
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ClientId)
                    .WithClientSecret(ClientSecret)
                    .WithAuthority(AzureCloudInstance.AzurePublic, Tenant)
                    .Build();

                AuthenticationResult authResult = app.AcquireTokenForClient(scopes)
                    .ExecuteAsync()
                    .Result;

                string tokenString = authResult.AccessToken;

                JwtSecurityToken token = new JwtSecurityToken(tokenString);
                Console.WriteLine($"Token valid for {token.ValidTo.Subtract(DateTime.Now).TotalMinutes} more minutes. (ValidTo: {token.ValidTo})");

                string badTokenString = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6Imk2bEdrM0ZaenhSY1ViMkMzbkVRN3N5SEpsWSJ9.eyJhdWQiOiI2ZTc0MTcyYi1iZTU2LTQ4NDMtOWZmNC1lNjZhMzliYjEyZTMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3YyLjAiLCJpYXQiOjE1MzcyMzEwNDgsIm5iZiI6MTUzNzIzMTA0OCwiZXhwIjoxNTM3MjM0OTQ4LCJhaW8iOiJBWFFBaS84SUFBQUF0QWFaTG8zQ2hNaWY2S09udHRSQjdlQnE0L0RjY1F6amNKR3hQWXkvQzNqRGFOR3hYZDZ3TklJVkdSZ2hOUm53SjFsT2NBbk5aY2p2a295ckZ4Q3R0djMzMTQwUmlvT0ZKNGJDQ0dWdW9DYWcxdU9UVDIyMjIyZ0h3TFBZUS91Zjc5UVgrMEtJaWpkcm1wNjlSY3R6bVE9PSIsImF6cCI6IjZlNzQxNzJiLWJlNTYtNDg0My05ZmY0LWU2NmEzOWJiMTJlMyIsImF6cGFjciI6IjAiLCJuYW1lIjoiQWJlIExpbmNvbG4iLCJvaWQiOiI2OTAyMjJiZS1mZjFhLTRkNTYtYWJkMS03ZTRmN2QzOGU0NzQiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhYmVsaUBtaWNyb3NvZnQuY29tIiwicmgiOiJJIiwic2NwIjoiYWNjZXNzX2FzX3VzZXIiLCJzdWIiOiJIS1pwZmFIeVdhZGVPb3VZbGl0anJJLUtmZlRtMjIyWDVyclYzeERxZktRIiwidGlkIjoiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3IiwidXRpIjoiZnFpQnFYTFBqMGVRYTgyUy1JWUZBQSIsInZlciI6IjIuMCJ9.pj4N-w_3Us9DrBLfpCt";
                JwtSecurityToken badToken = new JwtSecurityToken(badTokenString);
                Console.WriteLine($"Expired/bad token ValidTo: {badToken.ValidTo}");

                using (SqlConnection conn = new SqlConnection(AzureSqlDbConnectionString))
                {
                    conn.AccessToken = tokenString;
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1";
                        int result = (int)cmd.ExecuteScalar();
                        Console.WriteLine($"Success1: {result}");
                    }
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(AzureSqlDbConnectionString))
                    {
                        conn.AccessToken = badTokenString;
                        conn.Open();
                        Console.WriteLine("Fail. Connection succeeded with bad token. Exception expected.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Pass: {ex.Message}");
                }

                using (SqlConnection conn = new SqlConnection(AzureSqlDbConnectionString))
                {
                    conn.AccessToken = tokenString;
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1";
                        int result = (int)cmd.ExecuteScalar();
                        Console.WriteLine($"Success2: {result}");
                    }
                    conn.Close();

                    conn.AccessToken = badTokenString;
                    try
                    {
                        conn.Open();
                        Console.WriteLine("Fail. Connection succeeded with bad token. Exception expected.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Pass: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex);
            }

            Console.WriteLine();
            Console.Write("Press a key to continue...");
            Console.ReadKey();
        }
    }
}
