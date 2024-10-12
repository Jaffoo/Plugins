using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace DownloadDLL;

internal class Program
{
    static async Task Main()
    {
        var pkgName = await GetPkgLastVersion();
        if (string.IsNullOrWhiteSpace(pkgName))
        {
            Console.WriteLine("未发现到包");
            Console.ReadKey();
            return;
        }
        var cPath = Directory.GetCurrentDirectory();
        var pPath = Directory.GetParent(cPath)!.FullName;
        var path = Path.Combine(pPath, "BaseUsing", "nuget");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        foreach (var file in Directory.GetFiles(path))
            File.Delete(file);
        using var stream = await GetPkgStream(pkgName);
        var filePath = Path.Combine(path, pkgName);
        using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        Console.WriteLine("文件下载并保存成功。");
        Console.WriteLine("按任意键退出");
        Console.ReadKey();
    }
    private static async Task<string> GetPkgLastVersion()
    {
        var clientHandler = new HttpClientHandler
        {
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        var client = new HttpClient(clientHandler);
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://github.com/Jaffoo/QQBotPlugin/tree-commit-info/master/IPluginBase/bin/Debug"),
            Headers =
    {
        { "accept", "application/json" },
        { "accept-language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6" },
        { "cache-control", "no-cache" },
                { "github-verified-fetch", "true" },
                { "pragma", "no-cache" },
                { "priority", "u=1, i" },
                { "referer", "https://github.com/Jaffoo/QQBotPlugin/tree/master/IPluginBase/bin/Debug" },
                { "sec-ch-ua", "\"Microsoft Edge\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "Windows" },
                { "sec-fetch-dest", "empty" },
                { "sec-fetch-mode", "cors" },
                { "sec-fetch-site", "same-origin" },
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0" },
                { "x-requested-with", "XMLHttpRequest" },
                { "Accept", "*/*" },
                { "User-Agent", "PostmanRuntime-ApipostRuntime/1.1.0" },
                { "Connection", "keep-alive" },
    },
        };
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var jsonObject = JsonConvert.DeserializeObject<JObject>(body);
        if (jsonObject == null) return "";
        // 获取最新的键名
        var latestKey = jsonObject.Properties()
            .Select(p => p.Name)
            .OrderByDescending(name => name) // 按字典序降序排序
            .FirstOrDefault();
        return latestKey ?? "";
    }
    private static async Task<Stream> GetPkgStream(string pkgName)
    {
        var clientHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        var client = new HttpClient(clientHandler);
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://raw.githubusercontent.com/Jaffoo/QQBotPlugin/refs/heads/master/IPluginBase/bin/Debug/" + pkgName),
            Headers =
            {
                { "accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" },
                { "accept-language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6" },
                { "cache-control", "no-cache" },
                { "pragma", "no-cache" },
                { "priority", "u=0, i" },
                { "sec-ch-ua", "\"Microsoft Edge\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "Windows" },
                { "sec-fetch-dest", "document" },
                { "sec-fetch-mode", "navigate" },
                { "sec-fetch-site", "none" },
                { "sec-fetch-user", "?1" },
                { "upgrade-insecure-requests", "1" },
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0" },
                { "Accept", "*/*" },
                { "User-Agent", "PostmanRuntime-ApipostRuntime/1.1.0" },
                { "Connection", "keep-alive" },
            },
        };
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        return stream;
    }
}
