using IPluginBase;


namespace Bilibili;

public class Bilibili : PluginBase
{
    public override string Name { get; set; } = "小破站";
    public override string Desc { get; set; } = "监听小破站新动态和直播";
    public override string Version { get; set; } = "0.0.1";
    public override string Useage { get; set; } = "b站直播+用户id";
}
