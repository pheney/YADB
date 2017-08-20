using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using YADB.Common;

namespace YADB.Modules
{
    [Name("Help Commands")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;

        public HelpModule(CommandService service)           // Create a constructor for the commandservice dependency
        {
            _service = service;
        }

        /// <summary>
        /// 2017-8-20
        /// Display a menu of commands the bot understands.
        /// This display is PM'd to the user.
        /// The user is sent a message in their channel notifying them of the PM.
        /// </summary>
        [Command("#help"), Alias("help", "#h")]
        public async Task HelpMenuAsync([Remainder]string command=null)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                await HelpCommandAsync(command);
                return;
            }

            var builder = new EmbedBuilder()
            {
                Color = Constants.SlateBlue,
                Description = "These are the commands I understand."
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += cmd.Aliases.First() + "\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            builder.AddField(x =>
            {
                x.Name = "--";
                x.Value = "Or you can have a conversation with me. "
                + "Just make sure to always address me, so I know who you are talking to.";
                x.IsInline = false;
            });

            //  When this is a DM channel, direct the user 
            //  to look at their direct-messages.
            if ((Context.Channel as IDMChannel) == null)
            {
                await Context.Channel.SendMessageAsync("I PM'd you some help.");
            }

            //  Send help via direct-message to the user            
            var dmchannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync("", false, builder.Build());
        }

        private async Task HelpCommandAsync(string command)
        {
            var result = _service.Search(Context, command);
            IDMChannel dmchannel;

            if (!result.IsSuccess)
            {
                //  Send help via direct-message to the user            
                dmchannel = await Context.User.GetOrCreateDMChannelAsync();
                await dmchannel.SendMessageAsync("Sorry, I couldn't find a command like **" + command + "**.");
                return;
            }

            var builder = new EmbedBuilder()
            {
                Color = Constants.SlateBlue,
                Description = "Here are some commands like **" + command + "**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = "Parameters: " + string.Join(", ", cmd.Parameters.Select(p => p.Name)) + "\n" +
                              "Remarks: " + cmd.Remarks;
                    x.IsInline = false;
                });
            }

            //  Direct the user to look at their direct-messages
            await Context.Channel.SendMessageAsync("I PM'd you some help on that command.");

            //  Send help via direct-message to the user            
            dmchannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmchannel.SendMessageAsync("", false, builder.Build());
        }
    }
}
