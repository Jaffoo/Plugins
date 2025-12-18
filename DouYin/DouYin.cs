using AngleSharp.Html.Parser;
using IPluginBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        SetTimer("DouYin", async () => await CheckLiveTimer(), x => x.WithName("DouYin").ToRunEvery(3).Minutes());
    }

    private async Task SaveLiveStatus(string uid, bool liveStatus)
    {
        if (string.IsNullOrWhiteSpace(uid)) return;
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
        var idsStr = await GetConfig("RoomId");
        if (string.IsNullOrWhiteSpace(idsStr)) return;
        var roomList = idsStr.Split(',').ToList();
        foreach (var item in roomList)
        {
            try
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
            catch
            {
                return;
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
            var uid = text[4..].Trim();
            await fmr.SendMessage((await CheckLive(uid)).msg);
        }
        if (text.Length > 4 && text[..4] == "抖音关注")
        {
            var uid = text[4..].Trim();
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
            var uid = text[4..].Trim();
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

    public override string LogPath
    {
        get
        {
            if (!Directory.Exists(base.LogPath)) Directory.CreateDirectory(base.LogPath);
            var file = Path.Combine(base.LogPath, "DouYin.log");
            if (!File.Exists(file)) File.Create(file).Close();
            return file;
        }
        set { }
    }

    /// <summary>
    /// 直播状态
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public async Task<(MessageChain msg, bool isLive)> CheckLive(string uid)
    {
        var jsonStr = "";
        try
        {
            var msg = new MessageChainBuild();
            var roomInfo = await GetRoomInfo(uid);
            if (roomInfo == null) return (msg.Build(), false);
            var room = roomInfo["room"];
            var anchor = roomInfo["anchor"];
            if (room == null || anchor == null) return (msg.Build(), false);
            var liveStatus = room["status"]!.ToString().ToInt();
            if (liveStatus != 2)
            {
                msg.Text(anchor["nickname"] + "暂未开播");
                return (msg.Build(), false);
            }
            msg.Text("主播：" + anchor["nickname"]);
            msg.Text("标题：" + room["title"]);
            msg.Text("连接：" + $"https://live.douyin.com/{uid}");
            var cover = JArray.FromObject(room["cover"]!["url_list"]!)[0].ToString();
            msg.Text("封面：");
            msg.ImageByUrl(cover);
            return (msg.Build(), true);
        }
        catch
        {
            await File.AppendAllLinesAsync(LogPath, [jsonStr]);
            throw;
        }
    }

    public async Task<JObject?> GetRoomInfo(string uid)
    {
        var url = "https://live.douyin.com/" + uid;
        var response = await new HttpClient().GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        var parser = new HtmlParser();
        var document = parser.ParseDocument(content);

        var scripts = document.QuerySelectorAll("script[nonce]");
        foreach (var script in scripts)
        {
            var scriptText = script.TextContent;
            if (scriptText.IsNullOrWhiteSpace()) continue;
            if (!scriptText.Contains("roomInfo")) continue;
            if (!scriptText.Contains(@"c:[")) continue;
            var jsonStr = scriptText.Replace("self.__pace_f.push(", "");
            jsonStr = jsonStr.Substring(0, jsonStr.Length - 1);
            jsonStr = jsonStr.Replace(@"\n", "");
            var arr = JsonConvert.DeserializeObject<JArray>(jsonStr);
            if (arr == null || arr.Count <= 1) continue;
            var cStr = arr[1].ToString();
            var cObj = JsonConvert.DeserializeObject<JObject>("{" + cStr + "}");
            if (cObj == null) continue;
            var cArr = cObj["c"];
            if (cArr == null || cArr.Count() < 4) continue;
            var obj = cArr[3];
            if (obj == null) continue;
            var state = obj["state"];
            if (state == null) continue;
            var roomStore = state["roomStore"];
            if (roomStore == null) continue;
            var roomInfo = roomStore["roomInfo"];
            if (roomInfo == null) continue;
            return JObject.FromObject(roomInfo);
        }
        return null;
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
