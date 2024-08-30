using Plugins;
using System.Reactive.Linq;
using UnifyBot;
using UnifyBot.Receiver.MessageReceiver;

namespace Test
{
    internal class Program
    {
        static async Task Main()
        {
            Bot bot = new(new("192.168.1.101", 3001, 3000, "523366"));
            await bot.StartAsync();
            bot.MessageReceived.OfType<PrivateReceiver>().Subscribe(async x =>
            {
                await new SixtySeeWorld().FriendMessage(x);
                await new Oil().FriendMessage(x);
            });
            while (true)
            {
                Thread.Sleep(10);
            }
        }
    }
}
