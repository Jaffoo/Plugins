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
            new ChatGPT().GetAnswer("123","123");
            Bot bot = new(new("www.zink.asia", 3001, 3000));
            await bot.StartAsync();
            bot.MessageReceived.OfType<PrivateReceiver>().Subscribe(async x =>
            {
                await new ChatGPT().FriendMessage(x);
                await new SixtySeeWorld().FriendMessage(x);
                await new Oil().FriendMessage(x);
                await new DouYu().FriendMessage(x);
            });
            while (true)
            {
                Thread.Sleep(10);
            }
        }
    }
}
