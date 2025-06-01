using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using System.IO;

namespace bot
{
    internal class Program
    {

        static void Main(string[] args)
        {
            const string token = "ur token";
            BotHelper botHelper = new BotHelper(token);
        }
    }
}
