using Newtonsoft.Json;
using IPluginBase;
using UnifyBot.Receiver.MessageReceiver;
using UnifyBot.Utils;

namespace Oil;
public class Oil : PluginBase
{
    public override string Name { get; set; } = "Oil";
    public override string Desc { get; set; } = "今日油价";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "输入【今日油价+省份】";
    public async Task<string> OilProvince(string province)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(province)) return "省份不能为空！";
            string url = "https://api.pearktrue.cn/api/oil";
            var response = await UnifyBot.Utils.Tools.GetAsync(url);
            var data = response.ToJObject();
            if (data["code"]!.ToString() == "200")
            {
                var list = JsonConvert.DeserializeObject<List<OilPrice>>(data["data"]!.ToString()) ?? [];
                var item = list.FirstOrDefault(x => x.Province == province.Replace("省", ""));
                if (item == null) return "无" + province + "省份的油价信息！";
                string msg = province + "今日油价如下：";
                foreach (var price in item.Prices)
                {
                    msg += "\n" + price.Key + "号汽油" + price.Value + "升/元";
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
            if (text.Length <= 4 || text[..4] != "今日油价") return;
            var province = text[4..].Trim();
            await pr.SendMessage(await OilProvince(province));
        }
        catch (Exception)
        {
            return;
        }
    }
}

public class OilPrice
{
    public string Province { get; set; } = "";
    public Dictionary<string, string> Prices { get; set; } = [];
}
