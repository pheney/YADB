using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YADB.Common;
using YADB.Preconditions;

namespace YADB.Modules
{
    [Name("Notes")]
    public class NoteModule : ModuleBase<SocketCommandContext>
    {
        [Command("#Note"), Alias("#n")]
        [Remarks("Note functions (CRUD)")]
        [MinPermissions(AccessLevel.BotOwner)]
        public async Task ParseNoteCommand([Remainder]string input = null)
        {
            if (NoteData.Get == null) NoteData.Init();
            string helpResponse = "Note commands: add, edit, delete, list, help, or <key>";
            string errorResponse = "";

            //  When input is empty, show help.
            //  When input is 'help', show help.
            if (string.IsNullOrWhiteSpace(input) 
                || input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                await Context.Channel.SendMessageAsync(helpResponse);
                return;
            }

            //  when input has content, check for commands
            string[] commands = new string[] { "add", "edit", "delete", "del", "list" };
            string cmd = input.Split(' ')[0].ToLower();
            if (commands.Contains(cmd)){
                string parameters = input.Split(' ').JoinWith(" ", startIndex: 1);
                switch (cmd)
                {
                    case "add":
                        string[] newNote = SplitInput(parameters);
                        if (newNote != null) await AddNote(newNote[0], newNote[1]);
                        else errorResponse = "Invalid or insufficient input";
                        break;
                    case "edit":
                        string[] changeNote = SplitInput(parameters);
                        if (changeNote != null) await EditNote(changeNote[0], changeNote[1]);
                        else errorResponse = "Invalid or insufficient input";
                        break;
                    case "delete":
                    case "del":
                        await DeleteNote(parameters);
                        break;
                    case "list":
                        await ListNotes();
                        break;
                }
            }

            //  when no commands are found, check for keys
            else
            {
                await ShowNote(input);
            }            

            if (!string.IsNullOrWhiteSpace(errorResponse))
            {
                await Context.Channel.SendMessageAsync(errorResponse);
            }
        }

        private string[] SplitInput(string input)
        {
            string key = "", value = "";

            if (input.Split(' ').Length < 2) return null;

            if (input[0].Equals('"'))
            {
                string[] phrases = input.Split('"');
                if (phrases.Length < 3) return null;

                key = phrases[1];
                value = phrases.JoinWith(" ", startIndex: 2);
            }
            else
            {
                string[] words = input.Split(' ');
                key = words[0];
                value = words.JoinWith(" ", startIndex: 1);
            }

            return new string[] { key, value };
        }

        private async Task AddNote(string key, string data)
        {
            string response;

            if (NoteData.Get.info.ContainsKey(key))
            {
                response = "There is already a note with key \"" + key + "\"\n"
                    + "Try using a different key to save this note.";
            }
            else
            {
                NoteData.Get.info.Add(key, data);
                await FileOperations.SaveAsJson(NoteData.Get);
                response = "Added note.\n**" + key + "** : _" + data + "_";
            }

            await Context.Channel.SendMessageAsync(response);
        }

        private async Task EditNote(string key, string data)
        {
            string response = "";
            if (!NoteData.Get.info.ContainsKey(key))
            {
                await AddNote(key, data);
            }
            else
            {
                response = "Note **" + key + "**\n";
                response += "From : _";
                response += NoteData.Get.info[key] + "_\n";
                response +="To : _";
                NoteData.Get.info[key] = data;
                response += NoteData.Get.info[key] + "_";
                await FileOperations.SaveAsJson(NoteData.Get);
            }
            if (!string.IsNullOrWhiteSpace(response))
            {
                await Context.Channel.SendMessageAsync(response);
            }
        }

        private async Task DeleteNote(string input)
        {
            string result = "No keys matching keys found";

            //  check for 'key' provided as index number
            if (input[0].Equals('#'))
            {
                string indexRequest = input.Substring(1);
                int index;
                if (int.TryParse(indexRequest, out index))
                {
                    await DeleteNoteByIndex(index - 1);
                    return;
                }
            }

            List<string> keys = PotentialKeys(input);
            foreach (string key in keys)
            {
                if (NoteData.Get.info.ContainsKey(key))
                {
                    NoteData.Get.info.Remove(key);
                    await FileOperations.SaveAsJson(NoteData.Get);
                    result = "Note \"" + key + "\" deleted.";
                    break;
                }
            }

            await Context.Channel.SendMessageAsync(result);
        }

        private async Task DeleteNoteByIndex(int index)
        {
            string key = GetKeyByIndex(index);
            if (key == null)
            {
                string message = "There are " + NoteData.Get.info.Count + " notes. ";
                message += "When using note indicies, you must use a number between 1 and the number ";
                message += "of notes.";
                await Context.Channel.SendMessageAsync(message);
            }
            else
            {
                await DeleteNote(key);
            }
        }

        private async Task ShowNote(string input)
        {
            string result = "No keys matching \""+input+"\" found";

            //  check for 'key' provided as index number
            if (input[0].Equals('#'))
            {
                string indexRequest = input.Substring(1);
                int index;
                if (int.TryParse(indexRequest, out index))
                {
                    await ShowNoteByIndex(index-1);
                    return;
                }
            }

            List<string> keys = PotentialKeys(input);
            foreach (string key in keys)
            {
                if (NoteData.Get.info.ContainsKey(key))
                {
                    result = "**" + key + "** : " + NoteData.Get.info[key];
                    break;
                }
            }

            await Context.Channel.SendMessageAsync(result);
        }

        private async Task ShowNoteByIndex(int index)
        {
            string key = GetKeyByIndex(index);
            if (key == null){
                string message = "There are " + NoteData.Get.info.Count + " notes. ";
                message += "When using note indicies, you must use a number between 1 and the number ";
                message += "of notes.";
                await Context.Channel.SendMessageAsync(message);
            }else
            {
                await ShowNote(key);
            }
        }

        private async Task ListNotes()
        {
            string response = "No notes found";
            if (NoteData.Get.info.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("**Notes**\n");
                List<string> keys = NoteData.Get.info.Keys.ToList();
                keys.Sort();
                for(int i =0;i<keys.Count;i++)
                {
                    sb.Append("**"+(i+1)+"** : _" + keys[i] + "_\n");
                }
                response = sb.ToString();
            }
            await Context.Channel.SendMessageAsync(response);
        }

        private List<string> PotentialKeys(string input)
        {
            string[] words = input.Split(' ');
            List<string> keys = new List<string>();
            for (int i = 0; i < words.Length+1; i++)
            {
                keys.Add(words.JoinWith(" ", 0, i));
            }

            //  order from longest to shortest
            keys.Sort((left, right) => { return -left.CompareTo(right); });
            return keys;
        }

        private string GetKeyByIndex(int index)
        {
            if (index >= NoteData.Get.info.Count() || index < 0)
            {
                return null;
            }
            else
            {
                List<string> keys = NoteData.Get.info.Keys.ToList();
                keys.Sort();
                return keys[index];
            }
        }

        [Serializable]
        private class NoteData
        {
            [JsonIgnore]
            public static NoteData Get;

            #region Data

            public Dictionary<string, string> info;

            #endregion 

            public static void Init()
            {
                string FileName = new NoteData().GetFilename();
                string file = FileOperations.PathToFile(FileName);

                // When the file does NOT exists, create it
                if (!FileOperations.Exists(FileName))
                {
                    //  Create a new configuration object
                    var data = new NoteData();
                    data.info = new Dictionary<string, string>();

                    //  Save the configuration object
                    FileOperations.SaveAsJson(data);
                }

                NoteData.Get = FileOperations.Load<NoteData>();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(FileName + " loaded");
                Console.ResetColor();
            }
        }
    }
}
