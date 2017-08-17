﻿using Discord.Commands;
using Discord.WebSocket;
using Example.Preconditions;
using System.Threading.Tasks;

namespace Example.Modules
{
    [Name("Moderator")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("kick")]
        [Remarks("Kick the specified user.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Kick([Remainder]SocketGuildUser user)
        {
            await ReplyAsync("cya "+user.Mention+" :wave:");
            await user.KickAsync();
        }
    }
}
