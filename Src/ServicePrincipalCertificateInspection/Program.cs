using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.Security.Cryptography.X509Certificates;

namespace ServicePrincipalCertificateInspection
{
    /// <summary>
    /// ServicePrincipalCertificateInspection 検証クラス
    /// </summary>
    public class Program
    {
        /// <summary>
        /// エントリポイント
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task<int> Main(string[] args)
        {
            // 設定ファイル読込
            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json");
            var config = configuration.Build();

            // 設定ファイルより取得
            var orgUrl = new Uri(config["OrgUrl"]);
            var tenantId = config["TenantId"];
            var clientId = config["ClientId"];
            var certificateThumbprint = config["ClientCertificateThumbprint"];

            // Azure DevOps のサービスプリンシパル認証の際に必要なスコープを示す固定の文字列
            // ※appsettings.json のような設定ファイルに持たせてもよい
            const string azureDevOpsAppScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

            // 証明書ストア.現在ユーザより証明書情報を取得
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certificate = store.Certificates.Cast<X509Certificate2>().FirstOrDefault(cert => cert.Thumbprint == certificateThumbprint);

            // サービスプリンシパルのTenantId,ClientId,Thumbprint(拇印)を利用しアクセストークンを取得
            var credentails = new ClientCertificateCredential(tenantId, clientId, certificate);
            var accessToken = await credentails.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { azureDevOpsAppScope }));

            var vssAadToken = new VssAadToken("Bearer", accessToken.Token);
            // 取得したトークンでクレデンシャルを確立
            var vssAadCredentials = new VssAadCredential(vssAadToken);

            // Connect
            var connection = new VssConnection(orgUrl, vssAadCredentials);

            var client = connection.GetClient<WorkItemTrackingHttpClient>();

            Console.WriteLine("Enter the mode (get/create):");
            var command = Console.ReadLine().ToLowerInvariant();

            Console.WriteLine("Enter the project name:");
            var project = Console.ReadLine();

            // Mode=get の場合、IDを指定して既存のWorkItem＃を取得しタイトルを返却
            if (command == "get")
            {
                Console.WriteLine("Enter the WorkItem ID:");
                var id = int.Parse(Console.ReadLine());

                var workItem = await client.GetWorkItemAsync(project, id);
                Console.WriteLine(workItem.Fields["System.Title"]);
            }
            // Mode=create の場合、Titleを指定して新規にWorkItemを生成
            else if (command == "create")
            {
                Console.WriteLine("Enter the title for the new WorkItem:");
                var title = Console.ReadLine();

                var patchDocument = new JsonPatchDocument
                {
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = title
                    }
                };

                try
                {
                    var result = await client.CreateWorkItemAsync(patchDocument, project, "task");
                    Console.WriteLine($"work item created: Id = {result.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("Invalid mode entered.");
                return -1;
            }

            return 0;
        }
    }
}
