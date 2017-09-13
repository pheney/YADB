using Discord.Commands;
using YADB.Preconditions;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace YADB.Modules
{
    [Name("Math Commands")]
    public class MathModule : ModuleBase<SocketCommandContext>
    {
        [Command(".mult")]
        [Remarks("Get the product of all the numbers.")]
        [MinPermissions(AccessLevel.User)]
        public async Task Mult(params float[] numbers)
        {
            float product = 1;
            foreach (float n in numbers) product *= n;
            await ReplyAsync(product + " is the product of " + string.Join(", ", numbers));
        }

        [Command(".avg")]
        [Remarks("Compute mean value of all the numbers")]
        [MinPermissions(AccessLevel.User)]
        public async Task Avg(params float[] numbers)
        {
            await ReplyAsync((numbers.Sum() / numbers.Length)
                + " is the average of " + string.Join(", ", numbers));
        }

        [Command(".sum")]
        [Remarks("Add all the numbers")]
        [MinPermissions(AccessLevel.User)]
        public async Task Sum(params float[] numbers)
        {
            await ReplyAsync(numbers.Sum() + " is the sum of " + string.Join(", ", numbers));
        }

        [Command(".std")]
        [Remarks("Add all the numbers")]
        [MinPermissions(AccessLevel.User)]
        public async Task StandardDeviation(params float[] numbers)
        {
            await ReplyAsync(StandardDev(numbers) + " is the Standard Deviation of " + string.Join(", ", numbers));
        }

        [Command(".var")]
        [Remarks("Add all the numbers")]
        [MinPermissions(AccessLevel.User)]
        public async Task Variance(params float[] numbers)
        {
            await ReplyAsync(Variance(numbers) + " is the Variance of " + string.Join(", ", numbers));
        }

        private static double StandardDev(float[] numbers, bool sample = false)
        {
            return Math.Sqrt(Variance(numbers, sample));
        }

        private static double Variance(float[] numbers, bool sample=false)
        {
            float mean = numbers.Sum() / numbers.Length;
            double variance = 0;
            foreach (float n in numbers) variance += Math.Pow(n - mean, 2);
            variance /= sample ? numbers.Length + 1 : numbers.Length;
            return variance;
        }
    }
}
