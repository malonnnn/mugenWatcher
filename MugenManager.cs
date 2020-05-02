using System;
using System.Collections.Generic;
using System.Data;
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

        public Challonge ch;

        public bool Running { get; private set; }

        public MugenManager(Process mugen, Challonge ch)
        {
            if (Program.VERBOSE_ACTIVE) Console.WriteLine("Tracking process: " + mugen.ProcessName);
            this.mugen = mugen;
            this.mugenHandle = MemoryReader.OpenMugenProcess(mugen);
            this.matchStats = new MatchStats(mugen, mugenHandle);
            Thread thread = new Thread(new ThreadStart(this.Run));
            thread.Start();
            this.Running = true;
            this.ch = ch;
        }

        public void Run()
        {
            while (true)
            {
                if (mugen.HasExited)
                {
                    LogResults(ref ch);
                    return;
                }

                if (MemoryReader.ReadMatchMemory(matchStats))
                {
                    LogResults(ref ch);
                    if (Program.MATCH_ACTIVE) return;
                    matchStats = new MatchStats(mugen, mugenHandle);
                }
                Thread.Sleep(MEMORY_READ_INTERVAL);
            }
        }

        private void LogResults(ref Challonge ch)
        {
            if (matchStats.WinsLeft > 0 || matchStats.WinsRight > 0)
            {
                ch.recordWinner(matchStats.WinsLeft, matchStats.WinsRight);
                //Console.WriteLine(Environment.NewLine + "[" + DateTime.Now + "] RESULT: " + matchStats.WinsLeft + " - " + matchStats.WinsRight + Environment.NewLine);
                File.AppendAllText(LOG_FILE, Stopwatch.GetTimestamp() + "," + mugen.Id + "," + matchStats.WinsLeft + "," + matchStats.WinsRight + Environment.NewLine);
            }
        }
    }
}
