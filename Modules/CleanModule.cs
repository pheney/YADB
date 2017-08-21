using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YADB.Preconditions;

namespace YADB.Modules
{
    [Group("#Clean"), Alias ("#cl"), Name("Clean Commands")]
    [RequireContext(ContextType.Guild)]
    [Summary("Clean messages from a channel.")]
    public class CleanModule : ModuleBase<SocketCommandContext>
    {
        [Command("yours")]
        [Summary("Clean all recent messages")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task CleanAsync(int history = 100)
        {
            var self = Context.Guild.CurrentUser;
            var messages = (await GetMessageAsync(history)).Where(x => x.Author.Id == self.Id);

            if (self.GetPermissions(Context.Channel as SocketGuildChannel).ManageMessages)
                await DeleteMessagesAsync(messages);
            else
                foreach (var msg in messages)
                    await msg.DeleteAsync();

            var reply = await ReplyAsync($"Deleted **{messages.Count()}** message(s)");
            await DelayDeleteMessageAsync(reply);
        }

        [Command("all")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Clean all recent messages")]
        [MinPermissions(AccessLevel.ServerAdmin)]
        public async Task AllAsync(int history = 25)
        {
            var messages = await GetMessageAsync(history);
            await DeleteMessagesAsync(messages);

            var reply = await ReplyAsync($"Deleted **{messages.Count()}** message(s)");
            await DelayDeleteMessageAsync(reply);
        }

        [Command("user")]
        //[RequireUserPermission(ChannelPermission.ManageMessages)]
        //[RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Clean all recent messages from the specified user")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task UserAsync(SocketUser user, int history = 25)
        {
            var messages = (await GetMessageAsync(history)).Where(x => x.Author.Id == user.Id);
            await DeleteMessagesAsync(messages);

            var reply = await ReplyAsync($"Deleted **{messages.Count()}** message(s) by **{user}**");
            await DelayDeleteMessageAsync(reply);
        }

        [Command("bot")]
        //[RequireUserPermission(ChannelPermission.ManageMessages)]
        //[RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Clean all recent messages made by bots")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task BotsAsync(int history = 25)
        {
            var messages = (await GetMessageAsync(history)).Where(x => x.Author.IsBot);
            await DeleteMessagesAsync(messages);

            var reply = await ReplyAsync($"Deleted **{messages.Count()}** message(s) by bots");
            await DelayDeleteMessageAsync(reply);
        }

        [Command("contains")]
        //[RequireUserPermission(ChannelPermission.ManageMessages)]
        //[RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Clean all recent messages that contain a certain phrase")]
        [MinPermissions(AccessLevel.ServerMod)]
        public async Task ContainsAsync(string text, int history = 25)
        {
            var messages = (await GetMessageAsync(history)).Where(x => x.Content.ToLower().Contains(text.ToLower()));
            await DeleteMessagesAsync(messages);

            var reply = await ReplyAsync($"Deleted **{messages.Count()}** message(s) containing `{text}`.");
            await DelayDeleteMessageAsync(reply);
        }

        //[Command("attachments")]
        //[RequireUserPermission(ChannelPermission.ManageMessages)]
        //[RequireBotPermission(ChannelPermission.ManageMessages)]
        //[Summary("Clean all recent messages with attachments")]
        //public async Task AttachmentsAsync(int history = 25)
        //{
        //    var messages = (await GetMessageAsync(history)).Where(x => x.Attachments.Count() != 0);
        //    await DeleteMessagesAsync(messages);

        //    var reply = await ReplyAsync($"Deleted **{messages.Count()}** message(s) with attachments.");
        //    await DelayDeleteMessageAsync(reply);
        //}

        private Task<IEnumerable<IMessage>> GetMessageAsync(int count)
            => Context.Channel.GetMessagesAsync(count).Flatten();

        private Task DeleteMessagesAsync(IEnumerable<IMessage> messages)
            => Context.Channel.DeleteMessagesAsync(messages);

        private async Task DelayDeleteMessageAsync(IMessage message, int ms = 5000)
        {
            await Task.Delay(ms);
            await message.DeleteAsync();
        }
    }
}