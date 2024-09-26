﻿using Newtonsoft.Json.Linq;
using PluginServer;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using TBC.CommonLib;
using UnifyBot.Message.Chain;
using UnifyBot.Receiver.MessageReceiver;

namespace Plugins;

public class DouYin : BasePlugin
{
    public override string Name { get; set; } = "DouYin";
    public override string Desc { get; set; } = "监听新动态和直播";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "输入抖音直播/抖音关注/抖音通知+抖音号";
    public override string LogPath
    {
        get
        {
            var path = Path.Combine(base.LogPath, "DouYu.log");
            return path;
        }
        set { }
    }
    public DouYin()
    {
        if (!Directory.Exists(base.LogPath)) Directory.CreateDirectory(base.LogPath);
        Task.Run(GetCookie);
    }

    public async Task CheckLiveTimer()
    {
        var idsStr = GetConfig("RoomId").Result;
        if (string.IsNullOrWhiteSpace(idsStr)) return;
        var roomList = idsStr.Split(',').ToList();
        foreach (var item in roomList)
        {
            var res = await CheckLive(item);
            if (res != null)
            {
                var qqs = await GetConfig("Users");
                var list = qqs.ToListStr().Select(x => x.ToLong());
                foreach (var qq in list)
                {
                    await SendPrivateMsg(qq, res);
                }
            }
        }
    }

    public override async Task FriendMessage(PrivateReceiver fmr)
    {
        var text = fmr.Message?.GetPlainText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > 4 && text[..4] == "抖音直播")
        {
            var uid = text[4..];
            await fmr.SendMessage((await CheckLive(uid))!);
        }
        if (text.Length > 4 && text[..4] == "抖音关注")
        {
            var uid = text[4..];
            List<string> list = (await GetConfig("RoomId")).ToListStr();
            if (list.Count == 0 || !list.Contains(uid))
            {
                list.Add(uid);
                await fmr.SendMessage("已关注");
            }
            else
            {
                list.Remove(uid);
                await fmr.SendMessage("已取消关注");
            }
            await SaveConfig("RoomId", list.ListToStr());
        }
        if (text.Length > 4 && text[..4] == "抖音通知")
        {
            var uid = text[4..];
            var list = (await GetConfig("Users")).ToListStr();
            if (list.Count == 0 || !list.Contains(uid))
            {
                list.Add(uid);
                await fmr.SendMessage("已添加用户");
            }
            else
            {
                list.Remove(uid);
                await fmr.SendMessage("已删除用户");
            }
            await SaveConfig("Users", list.ListToStr());
        }
    }

    private Dictionary<string, string> Headers()
    {
        return new Dictionary<string, string> {
            { "accept","application/json" },
            { "accept-language","zh-CN,zh;q=0.9" },
            { "cache-control","no-cache" },
            { "pragma","no-cache" },
            { "referer","https://www.iesdouyin.com/" },
            { "sec-fetch-mode","cors" },
            { "sec-fetch-site","same-site" },
            { "x-requested-with","XMLHttpRequest" },
            { "Accept", "*/*" },
            { "User-Agent", "PostmanRuntime-ApipostRuntime/1.1.0" },
            { "Connection", "keep-alive" },
        };
    }
    private async Task<Dictionary<string, string>> LiveHeaders()
    {
        return new Dictionary<string, string> {
            { "accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3" },
            { "accept-language", "zh-CN,zh;q=0.9" },
            { "cache-control", "no-cache" },
            { "pragma", "no-cache" },
            { "sec-fetch-mode", "navigate" },
            { "sec-fetch-site", "same-site" },
            { "sec-fetch-user", "?1" },
            { "upgrade-insecure-requests", "1" },
            { "Accept", "*/*" },
            { "User-Agent", "PostmanRuntime-ApipostRuntime/1.1.0" },
            { "Connection", "keep-alive" },
            {"Cookie",await GetCookie() },
        };
    }
    private static string LiveQuery(string uid)
    {
        StringBuilder query = new("?");
        query.Append("aid=6383");
        query.Append("&device_platform=web");
        query.Append("&enter_from=web_live");
        query.Append("&cookie_enabled=true");
        query.Append("&browser_language=zh-CN");
        query.Append("&browser_platform=Win32");
        query.Append("&browser_name=Chrome");
        query.Append("&browser_version=109.0.0.0");
        query.Append("&web_rid=" + uid);
        return query.ToString();
    }
    private async Task<string> GetCookie()
    {
        var cookie = await GetConfig("Cookie");
        if (cookie.IsNullOrWhiteSpace())
        {
            cookie = await Generatettwid();
            await SaveConfig("Cookie", cookie);
        }
        return cookie;
    }

    /// <summary>
    /// 直播状态
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public async Task<MessageChain?> CheckLive(string uid, bool timer = false)
    {
        try
        {
            var url = "https://live.douyin.com/webcast/room/web/enter/" + LiveQuery(uid);
            HttpClient client = new();
            foreach (var item in LiveHeaders().Result)
                client.DefaultRequestHeaders.Add(item.Key, item.Value);
            var res = await client.GetStringAsync(url);
            if (res.Fetch<int>("status_code") != 0) throw new Exception();
            var msg = new MessageChainBuild();
            var roomRoot = res.Fetch("data");
            if (roomRoot.IsNullOrWhiteSpace())
            {
                msg.Text("该抖音用户未开通直播间");
                if (timer) return null;
                else return msg.Build();
            }
            var liveStatus = roomRoot.Fetch<int>("room_status");
            if (liveStatus != 0)
            {
                msg.Text(res.Fetch("data:user:nickname") + "暂未开播");
                if (timer) return null;
                else return msg.Build();
            }
            var roomInfo = roomRoot.Fetch<JArray>("data")[0].ToString();
            msg.Text("主播：" + roomRoot.Fetch("user:nickname") + "正在直播");
            msg.Text("\n标题：" + roomInfo.Fetch("title"));
            msg.Text("\n连接：" + $"https://live.douyin.com/{uid}");
            try
            {
                var cover = roomInfo.Fetch<List<string>>("cover:url_list")[0];
                msg.ImageByUrl("\n封面：" + cover);
            }
            catch
            {
            }
            return msg.Build();
        }
        catch (Exception e)
        {
            await File.AppendAllLinesAsync(LogPath, [e.Message]);
            return new MessageChainBuild().Text("直播查询失败，可能是缓存过期，已刷新，可再次尝试！").Build();
        }
    }

    /// <summary>
    /// 生成ttwid
    /// </summary>
    /// <returns></returns>
    public async Task<string> Generatettwid()
    {
        try
        {
            var url = "https://ttwid.bytedance.com/ttwid/union/register/";
            string body = "{\"region\": \"cn\",\"aid\": 1768,\"needFid\": false,\"service\": \"www.ixigua.com\",\"migrate_info\": {\t\"ticket\": \"\",\t\"source\": \"node\"},\"cbUrlProtocol\": \"https\",\"union\": true\n}";
            HttpClient client = new();
            foreach (var item in Headers())
                client.DefaultRequestHeaders.Add(item.Key, item.Value);
            var response = await client.PostAsync(url, new StringContent(body));
            var res = await response.Content.ReadAsStringAsync();
            if (res.Fetch<int>("status_code") == 0)
            {
                var resHeaders = response.Headers.GetValues("Set-Cookie");
                if (resHeaders.Any())
                    return resHeaders.First();
            }
            return "";
        }
        catch
        {
            return "";
        }
    }
}
