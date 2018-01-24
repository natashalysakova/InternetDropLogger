using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DropClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var gamesList = new[] { "overwatch", "wow", "diablo" };
            var processes = Process.GetProcesses();
            //
            foreach (var item in processes)
            {
                foreach (var item2 in gamesList)
                {
                    if (item.ProcessName.Contains(item2))
                    {
                        Console.WriteLine(item.ProcessName);
                    }
                }
            }
            //
            var z = processes.AsParallel().Where(x => gamesList.AsParallel().Contains(x.ProcessName)).Select(x => x.ProcessName);
            z.ForAll(x => { Console.WriteLine(x); });

            Console.ReadKey();
                
        }
    }
}
