using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using YADB.Common;
using YADB.Preconditions;
using static YADB.Common.Constants;

namespace YADB.Modules
{
    [Name("Moderator Commands")]
    [RequireContext(ContextType.Guild)]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        [Command("#kick")]
        [Remarks("Kick the specified user.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Kick([Remainder]SocketGuildUser user)
        {
            await ReplyAsync("cya " + user.Mention + " :wave:");
            await user.KickAsync();
        }

        #region
                
        [Command("#botnick")]
        [Remarks("Tell the bot to select a new nickname.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task BotNick()
        {
            //var self = await Context.Guild.GetCurrentUserAsync();
            var allUsers = Context.Guild.Users;

            SocketGuildUser self = Context.Guild.CurrentUser;

            int index = rnd.Next(allUsers.Count);
            string selectedUser = allUsers.Skip(index).First().Username;
            string newNick = Constants.ToFancyName(selectedUser);

            await self.ModifyAsync(x => x.Nickname = newNick);
            await ReplyAsync(self.Nickname + " becomes " + newNick);
        }
        //}

        #endregion
    }
}
