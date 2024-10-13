using Azure.Core;
using Azure.Identity;
using ManagedIdentityInspection.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.Net.Http.Headers;
using JsonPatchDocument = Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchDocument;

namespace ManagedIdentityInspection
{
    public class Reporting
    {
        private readonly ILogger<Reporting> _logger;
        /// <summary>OrganizationUrl</summary>
        public static readonly string OrgUrl = $"https://dev.azure.com/{Environment.GetEnvironmentVariable("AzureDevOpsOrg")}";

        /// <summary>UserAgent</summary>
        public static readonly List<ProductInfoHeaderValue> AppUserAgent = new()
        {
            new ProductInfoHeaderValue("Identity.ManagedIdentitySamples", "3.0"),
            new ProductInfoHeaderValue("(AzureDevOpsWithManagedIdentityInspection)")
        };

        /// <summary>Azure DevOps �̃T�[�r�X�v�����V�p���F�؂̍ۂɕK�v�ȃX�R�[�v�������Œ�̕�����</summary>
        public const string AzureDevOpsAppScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

        public Reporting(ILogger<Reporting> logger)
        {
            _logger = logger;
        }

        [Function("Reporting")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // ���N�G�X�g�{�f�B���p�����[�^���擾
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //var receiveModel = JsonConvert.DeserializeObject<ReceiveModel>(requestBody);

            var receiveModel = new ReceiveModel();
            receiveModel.title = "osatest-20241014_01";
            receiveModel.project = "Realis_Fleuze";

            // ManagedIdentity��TenantId,ManagedIdentityClientId�𗘗p���A�N�Z�X�g�[�N�����擾
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
            // �擾�����g�[�N���ŃN���f���V�������m��
            var vssAadCredentials = new VssAadCredential(vssAadToken);
            var settings = VssClientHttpRequestSettings.Default.Clone();
            settings.UserAgent = AppUserAgent;

            // Connection ��Https�o�R�Ȃ̂�Agent��t�^
            var vssConnection = new VssConnection(new Uri(OrgUrl), vssAadCredentials, settings);

            // AzureDevOpsRestAPI�̏���
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
                // AzureDevOpsRestAPI�𔭍s
                var result = await workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, receiveModel.project, "task");
                var responseMessage = $"Work item '{result.Id}' created.";

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());
                throw;
            }

            //return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
