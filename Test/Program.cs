using Plugins;
using System.Reactive.Linq;
using UnifyBot.Receiver.MessageReceiver;

namespace Test
{
    internal class Program
    {
        static async Task Main()
        {

            var bot = new UnifyBot.Bot(new UnifyBot.Model.Connect("www.zink.asia", 3001, 3000));
            await bot.StartAsync();

            var msg = (await new DouYin.DouYin().CheckLive("Tc5258")).msg;
            var res = msg.SendPrivate(bot, 1737678289);
            msg = (await new DouYu().CheckLive("9999")).msg;
            res = msg.SendPrivate(bot, 1737678289);
            bot.MessageReceived.OfType<PrivateReceiver>().Subscribe(x =>
            {
                Console.WriteLine(x.Message);
            });
            while (true)
            {
                Thread.Sleep(10);
            }
        }
    }
}
