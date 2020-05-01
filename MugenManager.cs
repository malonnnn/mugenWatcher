using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MugenWatcher
{
    class MugenManager
    {
        private static readonly int MEMORY_READ_INTERVAL = 100;
        private static readonly String LOG_FILE = "MugenWatcher.log";

        private MatchStats matchStats;

        public readonly Process mugen;
        public readonly int mugenHandle;

        public bool Running { get; private set; }

        public MugenManager(Process mugen)
        {
            if (Program.VERBOSE_ACTIVE) Console.WriteLine("Tracking process: " + mugen.ProcessName);
            this.mugen = mugen;
            this.mugenHandle = MemoryReader.OpenMugenProcess(mugen);
            this.matchStats = new MatchStats(mugen, mugenHandle);
            Thread thread = new Thread(new ThreadStart(this.Run));
            thread.Start();
            this.Running = true;
        }

        public void Run()
        {
            while (true)
            {
                if (mugen.HasExited)
                {
                    LogResults();
                    return;
                }

                if (MemoryReader.ReadMatchMemory(matchStats))
                {
                    LogResults();
                    if (Program.MATCH_ACTIVE) return;
                    matchStats = new MatchStats(mugen, mugenHandle);
                }
                Thread.Sleep(MEMORY_READ_INTERVAL);
            }
        }

        private void LogResults()
        {
            if (matchStats.WinsLeft > 0 || matchStats.WinsRight > 0)
            {
                Console.WriteLine(Environment.NewLine + "[" + DateTime.Now + "] RESULT: " + matchStats.WinsLeft + " - " + matchStats.WinsRight + Environment.NewLine);
                File.AppendAllText(LOG_FILE, Stopwatch.GetTimestamp() + "," + mugen.Id + "," + matchStats.WinsLeft + "," + matchStats.WinsRight + Environment.NewLine);
            }
        }
    }
}
