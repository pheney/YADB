using Discord;
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
        [Command("#kick"), Alias("#k")]
        [Remarks("Kick the specified user.")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task Kick([Remainder]SocketGuildUser user)
        {
            SocketGuildUser self = Context.Guild.CurrentUser;
            await ReplyAsync("_"+self.Nickname + " removed "+ user.Mention + " from the room._ :wave:");
            await user.KickAsync();
        }

        #region

        [Command("#botnick"), Alias("#bn")]
        [Remarks("Tell the bot to select a new nickname.")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task BotNick()
        {            
            var allUsers = Context.Guild.Users;

            int index = rnd.Next(allUsers.Count);
            string selectedUser = allUsers.Skip(index).First().Username;
            string newNick = Constants.ToFancyName(selectedUser);

            SocketGuildUser self = Context.Guild.CurrentUser;            
            await self.ModifyAsync(x => x.Nickname = newNick);
            await ReplyAsync(self.Nickname + " becomes " + newNick);
        }

        #endregion
    }
}
