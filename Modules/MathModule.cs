using Discord.Commands;
using YADB.Preconditions;
using System.Linq;
using System.Threading.Tasks;

namespace YADB.Modules
{
    [Name("Math Commands")]
    public class MathModule : ModuleBase<SocketCommandContext>
    {
        //[Command("isinteger")]
        //[Remarks("Check if the input text is a whole number.")]
        //[MinPermissions(AccessLevel.User)]
        //public async Task IsInteger(int number)
        //{
        //    await ReplyAsync("The text "+number+" is a number!");
        //}

        [Command("#mult")]
        [Remarks("Get the product of all the numbers.")]
        [MinPermissions(AccessLevel.User)]
        public async Task Mult(params float[] numbers)
        {
            float product = 1;
            foreach (float n in numbers) product *= n;
            await ReplyAsync(product + " is the product of " + string.Join(", ", numbers));
        }

        [Command("#avg")]
        [Remarks("Compute mean value of all the numbers")]
        [MinPermissions(AccessLevel.User)]
        public async Task Avg(params float[] numbers)
        {
            float result = numbers.Sum() / numbers.Length;
            await ReplyAsync(result + " is the average of " + string.Join(", ", numbers));
        }

        [Command("#sum")]
        [Remarks("Add all the numbers")]
        [MinPermissions(AccessLevel.User)]
        public async Task Sum(params float[] numbers)
        {
            float sum = numbers.Sum();
            await ReplyAsync(sum + " is the sum of " + string.Join(", ", numbers));
        }
    }
}
