using FluentScheduler;
using Newtonsoft.Json;
using System;
using TBC.CommonLib;
using UmoBot.PluginServer;
using UnifyBot.Message;
using UnifyBot.Message.Chain;
using UnifyBot.Receiver.MessageReceiver;

namespace Plugins;
public class SixtySeeWorld : BasePlugin
{
    public override string Name { get; set; } = "SixtySeeWorld";
    public override string Desc { get; set; } = "60s看世界";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "每日9点定时发送";
    public SixtySeeWorld()
    {
        SetTimer(async () => await GetImage(), x => x.WithName("SixtySeeWorld").ToRunEvery(9).Hours());
    }
    public async Task<string> GetImage()
    {
        try
        {
            string url = "https://api.pearktrue.cn/api/60s/image/";
            using HttpClient client = new HttpClient();
            // 发送 GET 请求
            HttpResponseMessage response = await client.GetAsync(url);

            // 确保响应成功
            response.EnsureSuccessStatusCode();

            // 读取响应内容为字节数组
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

            // 将字节数组写入文件
            await File.WriteAllBytesAsync("60.png", imageBytes);
            return "60.png";
        }
        catch (Exception)
        {
            return "";
        }
    }
}

public class OilPrice
{
    public string Province { get; set; } = "";
    public Dictionary<string, string> Prices { get; set; } = [];
}
