﻿using Discord.Commands;
using Discord.WebSocket;
using YADB.Preconditions;
using System.Threading.Tasks;

namespace YADB.Modules
{
    [Name("Example")]
    public class ExampleModule : ModuleBase<SocketCommandContext>
    {
        [Command("say"), Alias("s")]
        [Remarks("Make the bot say something")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task Say([Remainder]string text)
        {
            await ReplyAsync(text);
        }

        [Group("set"), Name("Example")]
        public class Set : ModuleBase
        {
            [Command("nick")]
            [Remarks("Ask the bot to change your nickname")]
            [MinPermissions(AccessLevel.User)]
            public async Task Nick([Remainder]string name)
            {
                var user = Context.User as SocketGuildUser;
                await user.ModifyAsync(x => x.Nickname = name);
                await ReplyAsync(user.Mention + " I changed your name to **"+name+"**");
            }

            [Command("botnick")]
            [Remarks("Change the bot's nickname")]
            [MinPermissions(AccessLevel.BotOwner)]
            public async Task BotNick([Remainder]string name)
            {
                var self = await Context.Guild.GetCurrentUserAsync();
                await self.ModifyAsync(x => x.Nickname = name);
                await ReplyAsync("I changed my name to **"+name+"**");
            }
        }
    }
}
