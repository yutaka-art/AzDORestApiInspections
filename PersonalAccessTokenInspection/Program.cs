using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace PersonalAccessTokenInspection
{
    /// <summary>
    /// PersonalAccessTokenInspection 検証クラス
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

            // 設定ファイルより、OrganizationURLとPATを取得
            var orgUrl = new Uri(config["OrgUrl"]);
            var pat = config["PersonalAccessToken"];

            // PATを利用しクレデンシャルを確立
            var credentials = new VssBasicCredential(string.Empty, pat);
            var connection = new VssConnection(orgUrl, credentials);

            // Connect
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
