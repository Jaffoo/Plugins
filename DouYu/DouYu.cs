﻿using PluginServer;
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
    public override string LogPath { get => base.LogPath + "DouYu.log"; set => base.LogPath = value; }

    public DouYu()
    {
        SetTimer("DouYu", async () => await CheckLiveTimer(), x => x.WithName("DouYu").ToRunEvery(1).Minutes());
    }

    public async Task CheckLiveTimer()
    {
        var idsStr = GetConfig("RoomId").Result;
        if (string.IsNullOrWhiteSpace(idsStr)) return;
        var roomList = idsStr.Split(',').ToList();
        foreach (var item in roomList)
        {
            var res = await CheckLiveV2(item);
            if (res != null)
                await SendPrivateMsg(1615842006, res);
        }
    }

    public async Task<MessageChain?> CheckLiveV2(string roomid)
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
        var timeDifference = Math.Abs(DateTime.Now.ToTimeStamp(false) - liveTimeSpan);
        File.AppendAllLines(LogPath, ["-----------------------------"]);
        File.AppendAllLines(LogPath, ["主播：" + room.Fetch("nickname")]);
        File.AppendAllLines(LogPath, ["开播时间：" + liveTimeSpan]);
        File.AppendAllLines(LogPath, ["当前时间：" + DateTime.Now.ToTimeStamp()]);
        File.AppendAllLines(LogPath, ["时间差：" + timeDifference]);
        File.AppendAllLines(LogPath, ["-----------------------------"]);
        if (timeDifference >= 60)
        {
            return null;
        }
        var msg = new MessageChainBuild();
        msg.Text("主播：" + room.Fetch("nickname") + "正在直播");
        msg.Text("\n标题：" + room.Fetch("room_name"));
        msg.Text("\n封面：").ImageByUrl(room.Fetch("room_pic"));
        return msg.Build();
    }

    public async Task<MessageChain?> CheckLive(string roomid)
    {
        string url = "https://www.douyu.com/betard/" + roomid;
        HttpClient http = new();
        var res = await http.GetStringAsync(url);
        var room = res.Fetch("room");
        var isLive = room.Fetch<int>("show_status") == 1;
        var loop = room.Fetch<int>("videoLoop");
        if (loop == 1) isLive = false;
        var msg = new MessageChainBuild();
        if (!isLive)
        {
            msg.Text("主播" + room.Fetch("nickname") + "暂未开播");
        }
        else
        {
            msg.Text("主播：" + room.Fetch("nickname") + "正在直播");
            msg.Text("\n标题：" + room.Fetch("room_name"));
            msg.Text("\n开播时间：" + Tools.TimeStampToDate(room.Fetch("show_time"), 10));
            msg.Text("\n封面：").ImageByUrl(room.Fetch("room_pic"));
        }
        return msg.Build();
    }

    public override async Task FriendMessage(PrivateReceiver fmr)
    {
        var text = fmr.Message?.GetPlainText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Length > 4 && text[..4] == "斗鱼直播")
        {
            var roomId = text[4..];
            var msg = await CheckLive(roomId);
            if (msg == null) return;
            await fmr.SendMessage(msg);
        }
        if (text.Length > 4 && text[..4] == "斗鱼关注")
        {
            var roomId = text[4..];
            var roomIdStr = await GetConfig("RoomId");
            var list = string.IsNullOrWhiteSpace(roomIdStr) ? [] : roomIdStr.Split(",").ToList();
            list.Add(roomId);
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
