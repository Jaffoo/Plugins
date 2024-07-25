using Newtonsoft.Json.Linq;
using TBC.CommonLib;
using UmoBot.PluginServer;
using UnifyBot.Receiver.MessageReceiver;

namespace Plugins
{
    public class SpeakWell : BasePlugin
    {
        public override string Name { get; set; } = "SpeakWell";
        public override string Version { get; set; } = "0.0.1";
        public override string Desc { get; set; } = "能不能好好说话，缩写查询。";
        public override string Useage { get; set; } = "打出英文问号+缩写，例如?yyds";

        public override async Task FriendMessage(PrivateReceiver pr)
        {
            var text = pr.Message?.GetPlainText();
            if (string.IsNullOrWhiteSpace(text)) return;
            var first = text[..1];
            if (string.IsNullOrWhiteSpace(first)) return;
            if (first != "?") return;
            var pinyin = text[1..];
            if (string.IsNullOrWhiteSpace(pinyin)) return;
            var res = await Abbreviations(pinyin);
            if (string.IsNullOrWhiteSpace(res)) return;
            await pr.SendMessage(res);
        }

        public async Task<string> Abbreviations(string words)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(words)) return "请输入缩写！";
                string url = "https://api.pearktrue.cn/api/suoxie/?word=" + words;
                var response = await Tools.GetAsync(url);
                var data = response.ToJObject();
                if (data["code"]!.ToString() == "200")
                {
                    return string.Join(",", data["data"] ?? new JArray());
                }
                if (data["code"]!.ToString() == "201")
                {
                    return data["msg"]!.ToString();
                }
                return data["msg"]?.ToString() ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
