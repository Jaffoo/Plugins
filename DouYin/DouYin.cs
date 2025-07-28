using Newtonsoft.Json.Linq;
using IPluginBase;
using System.Text;
using UnifyBot.Message.Chain;
using UnifyBot.Receiver.MessageReceiver;
using UnifyBot.Utils;

namespace DouYin;

public class DouYin : PluginBase
{
    public override string Name { get; set; } = "DouYin";
    public override string Desc { get; set; } = "监听新动态和直播";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "输入抖音直播/抖音关注/抖音通知+抖音号";

    public DouYin()
    {
        SetTimer("DouYinCookie", async () => await GetCookie(), x => x.WithName("DouYinCookie").ToRunEvery(1).Days().At(9,0));
        SetTimer("DouYin", async () => await CheckLiveTimer(), x => x.WithName("DouYin").ToRunEvery(1).Minutes());
    }

    private async Task SaveLiveStatus(string uid, bool liveStatus)
    {
        var data = await GetConfig("LiveStatus");
        if (data.Contains(uid + "-true;") || data.Contains(uid + "-false;"))
        {
            data = data.Replace(uid + "-true", uid + '-' + liveStatus.ToString().ToLower());
            data = data.Replace(uid + "-false", uid + '-' + liveStatus.ToString().ToLower());
        }
        else
        {
            data += uid + '-' + liveStatus.ToString().ToLower() + ";";
        }
        await SaveConfig("LiveStatus", data);
    }

    private async Task RemoveLiveStatus(string uid)
    {
        var data = await GetConfig("LiveStatus");
        if (data.Contains(uid + "-true;") || data.Contains(uid + "-false;"))
        {
            data = data.Replace(uid + "-true;", "");
            data = data.Replace(uid + "-false;", "");
            await SaveConfig("LiveStatus", data);
        }
    }

    private async Task<bool> UserLiveStatus(string uid)
    {
        var data = await GetConfig("LiveStatus");
        return data.Contains(uid + "-true;");
    }

    public async Task CheckLiveTimer()
    {
        var idsStr = GetConfig("RoomId").Result;
        if (string.IsNullOrWhiteSpace(idsStr)) return;
        var roomList = idsStr.Split(',').ToList();
        foreach (var item in roomList)
        {
            var (msg, isLive) = await CheckLive(item);
            var currStatus = await UserLiveStatus(item);
            if (currStatus == isLive) continue;
            await SaveLiveStatus(item, isLive);
            if (isLive)
            {
                var qqs = await GetConfig("Users");
                var list = ToListStr(qqs).Select(x => long.Parse(x));
                foreach (var qq in list)
                {
                    await SendPrivateMsg(qq, msg);
                }
            }
        }
    }

    public override async Task FriendMessage(PrivateReceiver fmr)
    {
        var text = fmr.Message.GetPlainText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text == "抖音关注")
        {
            var roomIdStr = await GetConfig("RoomId");
            if (roomIdStr.IsNullOrWhiteSpace())
                await fmr.SendMessage("未关注任何博主");
            else
                await fmr.SendMessage(roomIdStr);


        }
        if (text.Length > 4 && text[..4] == "抖音直播")
        {
            var uid = text[4..];
            await fmr.SendMessage((await CheckLive(uid)).msg);
        }
        if (text.Length > 4 && text[..4] == "抖音关注")
        {
            var uid = text[4..];
            var rooms = await GetConfig("RoomId");
            List<string> list = rooms.IsNullOrWhiteSpace() ? [] : ToListStr(rooms);
            if (list.Count == 0 || !list.Contains(uid))
            {
                list.Add(uid);
                await fmr.SendMessage("已关注");
            }
            else
            {
                list.Remove(uid);
                await RemoveLiveStatus(uid);
                await fmr.SendMessage("已取消关注");
            }
            await SaveConfig("RoomId", ListToStr(list));
        }
        if (text.Length > 4 && text[..4] == "抖音通知")
        {
            var uid = text[4..];
            var users = await GetConfig("Users");
            List<string> list = users.IsNullOrWhiteSpace() ? [] : ToListStr(users);
            if (list.Count == 0 || !list.Contains(uid))
            {
                list.Add(uid);
                await fmr.SendMessage("已添加通知用户");
            }
            else
            {
                list.Remove(uid);
                await fmr.SendMessage("已删除通知用户");
            }
            await SaveConfig("Users", ListToStr(list));
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
            { "Cookie", @"ttwid=1%7CtafTnqpDMcicD2oIhPsVFz4SPqUNquS4jQX33cMFgT0%7C1753620790%7Cfc0f94ae18e0c77a135f56667bfb9386abf57db03a88b530802947742cf568ce; Path=/; Domain=bytedance.com; Max-Age=31536000; HttpOnly; Secure" },
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
    public async Task<(MessageChain msg, bool isLive)> CheckLive(string uid)
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
                return (msg.Build(), false);
            }
            var liveStatus = roomRoot.Fetch<int>("room_status");
            if (liveStatus != 0)
            {
                msg.Text(res.Fetch("data:user:nickname") + "暂未开播");
                return (msg.Build(), false);
            }
            var roomInfo = roomRoot.Fetch<JArray>("data")[0].ToString();
            msg.Text("主播：" + roomRoot.Fetch("user:nickname") + "正在直播");
            msg.Text("标题：" + roomInfo.Fetch("title"));
            msg.Text("连接：" + $"https://live.douyin.com/{uid}");
            try
            {
                var cover = roomInfo.Fetch<List<string>>("cover:url_list")[0];
                msg.Text("封面：");
                msg.ImageByUrl(cover);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return (msg.Build(), true);
        }
        catch (Exception)
        {
            return (new MessageChainBuild().Text("直播查询失败，可能是缓存过期，已刷新，可再次尝试！").Build(), false);
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
    public string ListToStr<T>(List<T> list, string cr = ",")
    {
        return string.Join(cr, list);
    }
    public List<string> ToListStr(string str, char splitCahr = ',')
    {
        if (str.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(str));
        if (str.IsNullOrWhiteSpace()) return new List<string>();
        var list = str.Split(splitCahr).ToList();
        return list ?? throw new Exception("转换结果为空");
    }
}
