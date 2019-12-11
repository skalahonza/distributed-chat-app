using System;
using System.Threading.Tasks;

namespace DSVA.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Random();
            Console.WriteLine("Hello World!");
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine("Message");
                Task.Delay(TimeSpan.FromMilliseconds(r.Next(1000)));
            }
        }
    }
}
