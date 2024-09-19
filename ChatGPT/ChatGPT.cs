using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text;
using System.Xml;
using TBC.CommonLib;
using PluginServer;
using UnifyBot.Receiver.MessageReceiver;

namespace Plugins;
public class ChatGPT : BasePlugin
{

    public static Dictionary<string, List<object>> LastMsg = new();
    public string SecretKey
    {
        get
        {
            XmlDocument doc = new();
            doc.Load(ConfPath);
            var node = doc.SelectSingleNode("/Root/SecretKey");
            if (node != null)
                return node.InnerText;
            return "";
        }
    }
    public string Mode
    {
        get
        {
            XmlDocument doc = new();
            doc.Load(ConfPath);
            var node = doc.SelectSingleNode("/Root/Mode");
            if (node != null)
                return node.InnerText;
            return "gpt-4o-mini";
        }
    }

    public override string Name { get; set; } = "ChatGPT";
    public override string Desc { get; set; } = "免费的ChatGPT插件";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "输入？+你的问题。问号是中文的问号哦！";
    public override string ConfPath
    {
        get
        {
            var path = base.ConfPath + "ChatGPT.xml";
            if (!Directory.Exists(base.ConfPath)) Directory.CreateDirectory(base.ConfPath);
            return path;
        }
        set { }
    }
    public override string LogPath
    {
        get
        {
            var path = base.ConfPath + "ChatGPT.log";
            if (!Directory.Exists(base.ConfPath)) Directory.CreateDirectory(base.ConfPath);
            return path;
        }
        set { }
    }

    public ChatGPT()
    {
        try
        {
            if (!File.Exists(ConfPath))
            {
                // 创建一个新的 XmlDocument
                XmlDocument doc = new();

                // 创建 XML 声明
                XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(xmlDeclaration);

                // 创建根节点
                XmlElement rootNode = doc.CreateElement("Root");
                doc.AppendChild(rootNode);

                // 创建 Secrets 节点
                XmlElement secretsNode = doc.CreateElement("SecretKey");
                secretsNode.InnerText = "key值填入这，填入前请删除内容";
                rootNode.AppendChild(secretsNode); // 添加到根节点下

                // 创建注释
                XmlComment comment = doc.CreateComment("请到（https://github.com/chatanywhere/GPT_API_free）申请内测免费key后填入SecretKey中");

                // 将注释添加到 Secrets 节点之前
                rootNode.InsertBefore(comment, secretsNode);

                // 创建 模型 节点
                XmlElement mode = doc.CreateElement("Mode");
                mode.InnerText = "gpt-4o-mini";
                rootNode.AppendChild(mode); // 添加到根节点下 // 创建注释
                XmlComment comment1 = doc.CreateComment("AI模型，默认gpt-4o-mini");
                rootNode.InsertBefore(comment1, mode);

                // 将注释添加到 Secrets 节点之前
                rootNode.InsertBefore(comment, secretsNode);

                doc.Save(ConfPath);
            }
        }
        catch (Exception e)
        {
            File.AppendAllLines(LogPath, [e.Message]);
            return;
        }
    }
    public override async Task FriendMessage(PrivateReceiver gmr)
    {
        var text = gmr.Message?.GetPlainText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text[0] == '？')
        {
            var question = text.Replace("？", "");
            if (gmr.Sender != null)
            {
                var answer = await GetAnswer(question, gmr.Sender.QQ.ToString());
                await gmr.SendMessage(answer);
            }
        }
        if (text.Contains("设置模型#"))
        {
            var mode = text.Replace("设置模型#", "");
            if (mode.IsNullOrWhiteSpace())
                await gmr.SendMessage("请输入模型");
            else
                await gmr.SendMessage(Save("Mode", mode));

        }
        if (text.Contains("设置密钥#"))
        {
            var key = text.Replace("设置密钥#", "");
            if (key.IsNullOrWhiteSpace())
                await gmr.SendMessage("请输入密钥");
            else
                await gmr.SendMessage(Save("SecretKey", key));
        }
    }

    public async Task<string> GetAnswer(string question, string qq)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SecretKey)) return "请配置密钥";
            if (string.IsNullOrWhiteSpace(question)) return "请输入问题！";
            var url = "https://api.chatanywhere.com.cn/v1/chat/completions";
            var objs = new List<object>();
            if (LastMsg.TryGetValue(qq, out List<object>? value)) objs.AddRange(value);
            objs.Add(new
            {
                role = "user",
                content = question
            });
            var obj = new
            {
                model = Mode,
                messages = objs
            };
            var body = JsonConvert.SerializeObject(obj);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = new StringContent(body)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SecretKey}");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Apifox/1.0.0 (https://apifox.com)");

            var response = await client.SendAsync(request);
            var res = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(res);
            StringBuilder str = new();
            if (data.ContainsKey("choices"))
            {
                if (data["choices"] != null)
                {
                    var list = JArray.FromObject(data["choices"]!);
                    if (list.Count > 0)
                    {
                        foreach (JObject item in list.Cast<JObject>())
                        {
                            str.Append(item["message"]!["content"]!.ToString());
                            if (LastMsg.ContainsKey(qq))
                            {
                                if (LastMsg[qq].Count > 10) LastMsg[qq].Clear();
                                LastMsg[qq].Add(new
                                {
                                    role = "assistant",
                                    content = item["message"]!["content"]
                                });
                            }
                            else
                            {
                                LastMsg.Add(qq, []);
                                LastMsg[qq].Add(new
                                {
                                    role = "assistant",
                                    content = item["message"]!["content"]
                                });
                            }
                        }
                    }
                }
            }
            return str.ToString();
        }
        catch (Exception e)
        {
            await File.AppendAllLinesAsync(LogPath, [e.Message]);
            return "";
        }
    }

    public string Save(string key, string value)
    {
        XmlDocument doc = new();
        doc.Load(ConfPath);
        // 查找 Mode 节点
        XmlNode? modeNode = doc.SelectSingleNode("/Root/" + key);
        // 检查节点是否存在
        if (modeNode != null)
        {
            // 给 Mode 节点赋值
            modeNode.InnerText = value;
        }
        else
        {
            return "未找到配置项。";
        }

        // 保存更改
        doc.Save(ConfPath);
        return "设置成功。";
    }
}