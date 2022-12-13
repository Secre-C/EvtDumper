using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EvtDumper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage:EvtDumper {Path to EvtTool} {Path to unpacked CPKs}");
                return;
            }
            var timer = new Stopwatch();
            timer.Start();
            EvtDump(args, timer);
            timer.Stop();
            Console.WriteLine($"Dump files saved to {args[1]}");
            Console.WriteLine($"Finished in {(float)timer.ElapsedMilliseconds/1000:f8}");
        }

        static void EvtDump(string[] args, Stopwatch timer)
        {
            List<Task> tasks = new();
            Console.WriteLine("Finding Files...");

            tasks.Add(Task.Run(() => DumpEvt(args, timer, "Evt")));
            tasks.Add(Task.Run(() => DumpEvt(args, timer, "Ecs")));
            tasks.Add(Task.Run(() => DumpEvt(args, timer, "Lsd")));

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch
            {
                throw new Exception();
            }
        }

        static Task DumpEvt(string[] args, Stopwatch timer, string evtType)
        {
            string evtDump = $"/{evtType}Dump.txt";

            List<Task> dumpEvt = new();

            List<string> evtFileNames = Directory.GetFiles($"{args[1]}", $"*.{evtType}", SearchOption.AllDirectories).ToList();

            Console.WriteLine($"Decompiling {evtType}...");

            var asSpan = CollectionsMarshal.AsSpan(evtFileNames);
            foreach (string evtFile in asSpan)
            {
                dumpEvt.Add(Task.Run(() =>
                {
                    var evtTool = new Process();
                    evtTool.StartInfo.FileName = args[0];
                    evtTool.StartInfo.Arguments = $"\"{evtFile}\"";
                    evtTool.Start();
                    evtTool.WaitForExit();
#if DEBUG
                    Console.WriteLine(evtFile);
#endif
                }));
            }

            Task.WaitAll(dumpEvt.ToArray());

            Console.WriteLine($"Writing Dumped {evtType} File Contents...");

            using (StreamWriter newFile = File.CreateText($"{args[1]}{evtDump}"))
            {
                newFile.WriteLine($"{evtType} Dump Created by SecreC.");

                foreach (string evtFile in asSpan)
                {
                    string fileText = File.ReadAllText($"{evtFile}.json");
                    newFile.WriteLine("\n==================================");
                    newFile.WriteLine($"{Path.GetFileName(evtFile)}:");
                    newFile.WriteLine("==================================\n");
                    newFile.Write(fileText + "\n");
                    File.Delete($"{evtFile}.json");
                }
            }
            Console.WriteLine($"Finished {evtType} in {(float)timer.ElapsedMilliseconds / 1000:f8}");
            return Task.CompletedTask;
        }
    }
}