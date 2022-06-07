using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cray.CheckHashAgainstGithub;

public static class HashCompare
{
    [FunctionName("HashCompare")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        var result = new Dictionary<string, string>();

        if (data?.url is null)
        {
            result["error"] = "Missing url in JSON body.";
            return new JsonResult(result);
        }

        if (data?.hash is null)
        {
            result["error"] = "Missing hash in JSON body.";
            return new JsonResult(result);
        }

        string url = data.url.ToString();
        string hash = data.hash.ToString();
        var tempDir = Path.GetTempPath();
        var filePath = Path.Join(tempDir, "file.tohash");

        var client = new WebClient(); // Obsolete but IDGAF
        client.Proxy = null; // Prevent attempted proxy resolution, save execution time

        try
        {
            client.DownloadFile(url, filePath);
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var computedHash = Convert.ToHexString(sha256.ComputeHash(stream));
            Console.WriteLine($"Hash: {computedHash}");
            result["result"] = computedHash.Equals(hash) ? "True" : "False";
            return new JsonResult(result);
        }
        catch (Exception ex) // Lazy catch
        {
            result["error"] = $"Exception while calculating hash: {ex.Message}";
            return new JsonResult(result);
        }
    }
}