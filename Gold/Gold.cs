using IPluginBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnifyBot.Receiver.MessageReceiver;
using UnifyBot.Utils;

namespace Gold;
public class Gold : PluginBase
{
    public override string Name { get; set; } = "gold";
    public override string Desc { get; set; } = "今日金价";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "输入【今日金价】";
    public async Task<string> GetGoldPrice()
    {
        try
        {
            string url = "https://api.pearktrue.cn/api/goldprice/";
            var response = await UnifyBot.Utils.Tools.GetAsync(url);
            var data = response.ToJObject();
            if (data["code"]!.ToString() == "200")
            {
                var list = JsonConvert.DeserializeObject<JArray>(data["data"]!.ToString()) ?? [];
                string msg = "";
                foreach (var item in list)
                {
                    msg += "类型:" + item["title"] + " 价格:" + item["price"] + "元/克";
                }
                return msg;
            }
            if (data["code"]!.ToString() == "201")
            {
                return data["msg"]!.ToString(); ;
            }
            return data["msg"]?.ToString() ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    public override async Task FriendMessage(PrivateReceiver pr)
    {
        try
        {
            var text = pr.Message?.GetPlainText();
            if (string.IsNullOrWhiteSpace(text)) return;
            if (text.Length <= 4 || text[..4] != "今日金价") return;
            var province = text[4..].Trim();
            await pr.SendMessage(await GetGoldPrice());
        }
        catch (Exception)
        {
            return;
        }
    }
}
