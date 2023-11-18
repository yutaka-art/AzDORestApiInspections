using Azure.Core;
using Azure.Identity;
using ManagedIdentityInspection.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using JsonPatchDocument = Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchDocument;

namespace ManagedIdentityInspection
{
    /// <summary>
    /// ManagedIdentityInspection 検証クラス
    /// </summary>
    public static class Reporting
    {
        /// <summary>OrganizationUrl</summary>
        public static readonly string OrgUrl = $"https://dev.azure.com/{Environment.GetEnvironmentVariable("AzureDevOpsOrg")}";

        /// <summary>UserAgent</summary>
        public static readonly List<ProductInfoHeaderValue> AppUserAgent = new()
        {
            new ProductInfoHeaderValue("Identity.ManagedIdentitySamples", "3.0"),
            new ProductInfoHeaderValue("(AzureDevOpsWithManagedIdentityInspection)")
        };

        /// <summary>Azure DevOps のサービスプリンシパル認証の際に必要なスコープを示す固定の文字列</summary>
        public const string AzureDevOpsAppScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

        /// <summary>
        /// Sample Functions
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Reporting")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Start Log
            log.LogInformation("C# HTTP trigger function processed a request.");

            // リクエストボディよりパラメータを取得
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var receiveModel = JsonConvert.DeserializeObject<ReceiveModel>(requestBody);

            // ManagedIdentityのTenantId,ManagedIdentityClientIdを利用しアクセストークンを取得
            var credentials = new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                    {
                        TenantId = Environment.GetEnvironmentVariable("TenantId"),
                        ManagedIdentityClientId = Environment.GetEnvironmentVariable("ManagedIdentityClientId"),
                        ExcludeEnvironmentCredential = true
                    });
            var accessToken = await credentials.GetTokenAsync(new TokenRequestContext(
                new[] { AzureDevOpsAppScope }), CancellationToken.None);

            var vssAadToken = new VssAadToken("Bearer", accessToken.Token);
            // 取得したトークンでクレデンシャルを確立
            var vssAadCredentials = new VssAadCredential(vssAadToken);
            var settings = VssClientHttpRequestSettings.Default.Clone();
            settings.UserAgent = AppUserAgent;

            // Connection ※Https経由なのでAgentを付与
            var vssConnection = new VssConnection(new Uri(OrgUrl), vssAadCredentials, settings);

            // AzureDevOpsRestAPIの準備
            var workItemTrackingHttpClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();
            JsonPatchDocument patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.Title",
                    Value = receiveModel.title
                }
            };

            try
            {
                // AzureDevOpsRestAPIを発行
                var result = await workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, receiveModel.project, "task");
                var responseMessage = $"Work item '{result.Id}' created.";

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.ToString());
                throw;
            }
        }
    }
}
