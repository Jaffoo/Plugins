using PluginServer;
using TBC.CommonLib;
using UnifyBot.Message.Chain;
using UnifyBot.Receiver.MessageReceiver;

namespace Plugins;

public class DouYu : BasePlugin
{
    public override string Name { get; set; } = "DouYuLive";
    public override string Version { get; set; } = "0.0.1";
    public override string Desc { get; set; } = "直播查询";
    public override string Useage { get; set; } = "输入【斗鱼直播+房间号】，例如斗鱼直播111";
    public override string LogPath
    {
        get
        {
            var path = Path.Combine(base.LogPath, "DouYu.log");
            return path;
        }
        set { }
    }

    public DouYu()
    {
        if (!Directory.Exists(base.LogPath)) Directory.CreateDirectory(base.LogPath);
        SetTimer("DouYu", async () => await CheckLiveTimer(), x => x.WithName("DouYu").ToRunEvery(1).Minutes());
    }

    private async Task SaveLiveStatus(string uid, bool liveStatus)
    {
        var data = await GetConfig("LiveStatus");
        data = data.Replace(uid + "-true", uid + '-' + liveStatus);
        data = data.Replace(uid + "-false", uid + '-' + liveStatus);
        await SaveConfig("LiveStatus", data);
    }

    private async Task<bool> UserLiveStatus(string uid)
    {
        var data = await GetConfig("LiveStatus");
        if (data.Contains(uid + "-true")) return true;
        return false;
    }
    public async Task CheckLiveTimer()
    {
        var idsStr = GetConfig("RoomId").Result;
        if (string.IsNullOrWhiteSpace(idsStr)) return;
        var roomList = idsStr.Split(',').ToList();
        foreach (var item in roomList)
        {
            var (msg, isLive) = await CheckLive(item);
            if (isLive)
            {
                var currStatus = await UserLiveStatus(item);
                if (currStatus == isLive) continue;
                var qqs = await GetConfig("Users");
                var list = qqs.ToListStr().Select(x => x.ToLong());
                foreach (var qq in list)
                {
                    await SendPrivateMsg(qq, msg);
                }
                await SaveLiveStatus(item, isLive);
            }
        }
    }

    public async Task<(MessageChain msg, bool isLive)> CheckLive(string roomid)
    {
        var msg = new MessageChainBuild();
        string url = "https://www.douyu.com/betard/" + roomid;
        HttpClient http = new();
        var res = await http.GetStringAsync(url);
        var room = res.Fetch("room");
        var isLive = room.Fetch<int>("show_status") == 1;
        var loop = room.Fetch<int>("videoLoop");
        if (loop == 1 || !isLive)
        {
            msg.Text(room.Fetch("nickname") + "暂未开播");
            return (msg.Build(), false);
        }

        msg.Text("主播：" + room.Fetch("nickname") + "正在直播");
        msg.Text("\n标题：" + room.Fetch("room_name"));
        msg.Text("\n开播时间：" + Tools.TimeStampToDate(room.Fetch("show_time")).ToString("yyyy/MM/dd HH:mm:ss"));
        msg.Text("\n封面：").ImageByUrl(room.Fetch("room_pic"));
        return (msg.Build(), true);
    }

    public override async Task FriendMessage(PrivateReceiver fmr)
    {
        var text = fmr.Message?.GetPlainText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > 4 && text[..4] == "斗鱼直播")
        {
            var roomId = text[4..];
            var (msg, _) = await CheckLive(roomId);
            await fmr.SendMessage(msg);
        }
        if (text == "斗鱼关注")
        {
            var roomIdStr = await GetConfig("RoomId");
            if (roomIdStr != null)
            {
                await fmr.SendMessage(roomIdStr);
            }

        }
        if (text.Length > 4 && text[..4] == "斗鱼通知")
        {
            var qq = text[4..];
            var qqs = await GetConfig("Users");
            var list = qqs.IsNullOrWhiteSpace() ? [] : qqs.ToListStr();
            if (!list.Contains(qq))
            {
                list.Add(qq);
                _ = await SaveConfig("Users", list.ListToStr());
                await fmr.SendMessage("添加成功");
            }
            else
            {
                list.Remove(qq);
                _ = await SaveConfig("Users", list.ListToStr());
                await fmr.SendMessage("删除成功");
            }
        }
        if (text.Length > 4 && text[..4] == "斗鱼关注")
        {
            var roomId = text[4..];
            var roomIdStr = await GetConfig("RoomId");
            var list = string.IsNullOrWhiteSpace(roomIdStr) ? [] : roomIdStr.Split(",").ToList();
            if (!list.Contains(roomId)) list.Add(roomId);
            var b = await SaveConfig("RoomId", string.Join(",", list));
            await fmr.SendMessage(b ? "添加成功" : "添加失败");
        }
        if (text.Length > 4 && text[..4] == "斗鱼取关")
        {
            var roomId = text[4..];
            var roomIdStr = await GetConfig("RoomId");
            var list = string.IsNullOrWhiteSpace(roomIdStr) ? [] : roomIdStr.Split(",").ToList();
            list.Remove(roomId);
            var b = await SaveConfig("RoomId", string.Join(",", list));
            await fmr.SendMessage(b ? "取关成功" : "取关失败");
        }
    }
}
