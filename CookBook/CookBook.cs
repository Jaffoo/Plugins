using Newtonsoft.Json;
using IPluginBase;
using UnifyBot.Message.Chain;
using UnifyBot.Receiver.MessageReceiver;
using UnifyBot.Utils;
using System.Text.RegularExpressions;

namespace CookBook;
public class CookBook : PluginBase
{
    private static List<Cooks> Menu = new();
    private static string CookName = string.Empty;
    public override string Name { get; set; } = "CookBook";
    public override string Desc { get; set; } = "菜谱";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "输入【菜谱+菜名/菜名+怎么做】";
    public async Task Cook(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string url = "https://api.pearktrue.cn/api/cookbook/?search=" + name;
            var response = await UnifyBot.Utils.Tools.GetAsync(url);
            var data = response.ToJObject();
            if (data["code"]!.ToString() == "200")
            {
                Menu = JsonConvert.DeserializeObject<List<Cooks>>(data["data"]!.ToString()) ?? [];
            }
            if (data["code"]!.ToString() == "201")
            {
                return;
            }
            return;
        }
        catch (Exception)
        {
            return;
        }
    }

    public override async Task FriendMessage(PrivateReceiver pr)
    {
        try
        {
            var text = pr.Message?.GetPlainText();
            if (string.IsNullOrWhiteSpace(text)) return;
            if (!CookName.IsNullOrWhiteSpace() && text.Contains(CookName) && Menu.Count > 0)
            {
                var indexStr = text.Replace(CookName, "");
                if (IsNumber(indexStr))
                {
                    var index1 = indexStr.ToInt() - 1;
                    var mcb = new MessageChainBuild()
                        .Text("材料：" + ListToStr(Menu[index1].Materials))
                        .Text("\n步骤：\n" + ListToStr(Menu[index1].Practice, "\n"));
                    await pr.SendMessage(mcb.Build());
                }
            }

            if (!(text.Contains("菜谱") || text.Contains("怎么做"))) return;
            var name = "";
            if (text[..2] == "菜谱") name = text.Replace("菜谱", "");
            if (text[^3..] == "怎么做") name = text.Replace("怎么做", "");
            CookName = name;
            await Cook(name);
            int index = 1;
            foreach (var item in Menu)
            {
                var mcb = new MessageChainBuild().Text(index + ".").Text(item.Name).ImageByUrl(item.Image);
                await pr.SendMessage(mcb.Build());
                index++;
            }
        }
        catch (Exception)
        {
            return;
        }
    }
    public bool IsNumber(string input)
    {
        // 正则表达式：匹配整数
        string pattern = @"^\d+$";
        return Regex.IsMatch(input, pattern);
    }

    public string ListToStr<T>(List<T> list, string cr = ",")
    {
        return string.Join(cr, list);
    }
}

public class Cooks
{
    public string Name { get; set; } = "";
    public string Desc { get; set; } = "";
    public string Image { get; set; } = "";
    public List<string> Materials { get; set; } = [];
    public List<string> Practice { get; set; } = [];
}
