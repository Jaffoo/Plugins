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
    public override string Useage { get; set; } = "输入【查询斗鱼+房间号】，例如查询斗鱼111";

    public async Task<MessageChain?> CheckLive(string roomid)
    {
        string url = "https://www.douyu.com/betard/" + roomid;
        HttpClient http = new();
        var res = await http.GetStringAsync(url);
        var room = res.Fetch("room");
        var isLive = room.Fetch<int>("show_status") == 1;
        var loop = room.Fetch<int>("videoLoop");
        if (loop == 1) isLive = false;
        if (!isLive) return null;
        var liveTimeSpan = room.Fetch<long>("show_time");
        var liveTime = Tools.TimeStampToDate(liveTimeSpan);
        var msg = new MessageChainBuild();
        msg.Text("主播：" + room.Fetch("nickname") + "正在直播");
        msg.Text("\n标题：" + room.Fetch("room_name"));
        msg.Text("\n封面：").ImageByUrl(room.Fetch("room_pic"));
        return msg.Build();
    }

    public override async Task FriendMessage(PrivateReceiver fmr)
    {
        var text = fmr.Message?.GetPlainText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > 4 && text[..4] == "查询斗鱼")
        {
            var roomId = text[4..];
            var msg = await CheckLive(roomId);
            if (msg == null) return;
            await fmr.SendMessage(msg);
        }
    }
}
