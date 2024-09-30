using Plugins;
using System.Reactive.Linq;
using TBC.CommonLib;
using UnifyBot;
using UnifyBot.Receiver.MessageReceiver;

namespace Test
{
    internal class Program
    {
        static async Task Main()
        {
          await  new DouYin().CheckLive("Tc5258");

            var bot = new UnifyBot.Bot(new UnifyBot.Model.Connect("www.zink.asia", 3001, 3000, "523366"));
            await bot.StartAsync();
            bot.MessageReceived.OfType<PrivateReceiver>().Subscribe(async x =>
            {
                await new DouYin().FriendMessage(x);
            });
            while (true)
            {
                Thread.Sleep(10);
            }
        }
    }
}
