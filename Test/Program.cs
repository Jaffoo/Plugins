using Plugins;
using System.Reactive.Linq;
using UnifyBot.Receiver.MessageReceiver;

namespace Test
{
    internal class Program
    {
        static async Task Main()
        {
            var dy = new DouYin.DouYin();
            var (msg, isLive) = await dy.CheckLive("cctv.com");
            Console.ReadKey();
        }
    }
}
