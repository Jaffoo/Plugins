﻿using SqlSugar;
using TBC.CommonLib;
using IPluginBase;
using UnifyBot.Message.Chain;
using UnifyBot.Receiver.MessageReceiver;

namespace SixtySeeWorld;
public class SixtySeeWorld : PluginBase
{
    public override string Name { get; set; } = "SixtySeeWorld";
    public override string Desc { get; set; } = "60s看世界";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "每日9点定时发送，输入指令看世界+qq添加，取消看世界+qq删除，今日资讯获取";
    public override string ConfPath
    {
        get
        {
            var path = base.ConfPath + "SixtySeeWorld.txt";
            if (!Directory.Exists(base.ConfPath)) Directory.CreateDirectory(base.ConfPath);
            return path;
        }
        set { }
    }
    public SixtySeeWorld()
    {
        if (!File.Exists(ConfPath)) File.Create(ConfPath);
        SetTimer("SixtySeeWorld", async () => await GetImage(), x => x.WithName("SixtySeeWorld").ToRunEvery(1).Days().At(9, 30));
    }
    public async Task GetImage(long senderQQ = 0)
    {
        try
        {
            string url = "https://api.pearktrue.cn/api/60s/image/";
            using HttpClient client = new();
            // 发送 GET 请求
            HttpResponseMessage response = await client.GetAsync(url);

            // 确保响应成功
            response.EnsureSuccessStatusCode();

            // 读取响应内容为字节数组
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            var b64 = "base64://" + Convert.ToBase64String(imageBytes);
            var qqStr = File.ReadAllText(ConfPath);
            if (qqStr.IsNullOrWhiteSpace()) return;
            var qq = qqStr.ToListInt().Select(x => x.ToLong()).ToList();
            if (senderQQ > 0 && qq.Contains(senderQQ))
                await SendPrivateMsg(senderQQ, new MessageChainBuild().ImageByBase64(b64).Build());
            else
                foreach (var item in qq)
                    await SendPrivateMsg(item, new MessageChainBuild().ImageByBase64(b64).Build());
        }
        catch (Exception)
        {
        }
    }
    public override async Task FriendMessage(PrivateReceiver gmr)
    {
        var text = gmr.Message?.GetPlainText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text == "今日资讯")
        {
            await GetImage(gmr.SenderQQ);
        }
        if (text.Length > 3 && text[..3] == "看世界")
        {
            var qq = text[3..];
            var str = await File.ReadAllTextAsync(ConfPath);
            var list = str.IsNullOrWhiteSpace() ? [] : str.ToListStr();
            if (!list.Contains(qq))
            {
                list.Add(qq);
                File.WriteAllText(ConfPath, list.ListToStr());
            }
            await gmr.SendMessage("已添加");
        }
        if (text.Length > 5 && text[..5] == "取消看世界")
        {
            var qq = text[5..];
            var str = await File.ReadAllTextAsync(ConfPath);
            var list = str.IsNullOrWhiteSpace() ? [] : str.ToListStr();
            list.Remove(qq);
            await File.WriteAllTextAsync(ConfPath, list.ListToStr());
            await gmr.SendMessage("已删除");
        }
    }
}

public class OilPrice
{
    public string Province { get; set; } = "";
    public Dictionary<string, string> Prices { get; set; } = [];
}
