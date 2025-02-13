﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MugenWatcher
{
    class Program
    {
        private static readonly int PROCESS_FIND_INTERVAL = 100;

        public static bool VERBOSE_ACTIVE;
        public static bool MATCH_ACTIVE;
        public static bool PROCESS_ACTIVE;

        private static List<MugenManager> managers = new List<MugenManager>();
        public static bool mugenFound = false;
        

        static void Main(string[] args)
        {
            VERBOSE_ACTIVE = args.Contains("-v") || args.Contains("--verbose");
            MATCH_ACTIVE = args.Contains("-m") || args.Contains("--match");
            PROCESS_ACTIVE = args.Contains("-p") || args.Contains("--process");


            Challonge ch = new Challonge();
            Run(ch);
        }

        private static void Run(Challonge ch)
        {
            while (true)
            {
                foreach (MugenManager mm in managers)
                {
                    if (!mm.Running)
                    {
                        managers.Remove(mm);
                        if (MATCH_ACTIVE || PROCESS_ACTIVE) return;
                    }
                }

                if (!mugenFound)
                {
                    List<Process> mugens = FindUntrackedMugens(ch);
                    if (mugens.Count > 0)
                    {
                        foreach (Process p in mugens)
                        {
                            managers.Add(new MugenManager(p,ch));
                            if (MATCH_ACTIVE || PROCESS_ACTIVE)
                            {
                                mugenFound = true;
                                break;
                            }
                        }
                    }
                }
                Thread.Sleep(PROCESS_FIND_INTERVAL);
            }
        }

        private static List<Process> FindUntrackedMugens(Challonge ch)
        {
            Process[] mugens = MemoryReader.GetMugenHandles();
            List<Process> untracked = new List<Process>();
            int counter = 0;
            foreach (Process p in mugens)
            {
                counter++;
                bool tracked = false;
                foreach (MugenManager mm in managers)
                {
                    if (mm.mugen.Id == p.Id)
                    {
                        tracked = true;
                        break;
                    }
                }
                if (!tracked)
                {
                    untracked.Add(p);
                }
            }
            if(counter == 0)
            {
                ch.startTournament().Wait();
                ch.getNextMatch().Wait();
                ch.startMugen();
            }
            return untracked;
        }
    }
}
