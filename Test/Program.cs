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
            var res = await dy.CheckLive("Uliiiiiiii");
            Console.ReadKey();
        }
    }
}
