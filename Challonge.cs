using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Threading;

namespace MugenWatcher
{
    class Challonge
    {
        public string apiKey;
        public string tournamentID;
        public string p1Name;
        public string p2Name;
        public int p1ID = 0;
        public int p2ID = 0;

        dynamic upcomingMatches;

        public Challonge()
        {
            apiKey = "Ya5yvSd17FBnbIQ51aOq4ayMEz66DXSljdkK0BcA";
            tournamentID = "bm6vuprz";
        }

        public async Task startTournament()
        {
            // start the tournament
            var client = new RestClient("https://api.challonge.com");
            var request = new RestRequest("/v1/tournaments/" + tournamentID + "/start.json", Method.POST);
            request.AddJsonBody(new
            {
                api_key = apiKey,
                include_participants = 0,
                include_matches = 0
            });
            Task<IRestResponse> t = client.ExecuteAsync(request);
            t.Wait();
            //var restResponse = await t;

        }

        public async Task getNextMatch()
        {

            // get the player ID's of the 1st match
            // needed so we can get their names
            Glitch:
            getAllMatches().Wait();

            p1ID = upcomingMatches[0]["match"]["player1_id"];
            p2ID = upcomingMatches[0]["match"]["player2_id"];

            // get player 1 name
            var client = new RestClient("https://api.challonge.com");
            var request = new RestRequest("/v1/tournaments/" + tournamentID + "/participants/" + p1ID + ".json", Method.GET);
            request.AddParameter("api_key", apiKey, ParameterType.QueryString);
            Task<IRestResponse> t = client.ExecuteAsync(request);
            t.Wait();
            var response = await t;
            dynamic responseArray = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
            Console.WriteLine("got P1 Name");
            
            string temp = responseArray["participant"]["name"];


            // get player 2 name
            client = new RestClient("https://api.challonge.com");
            request = new RestRequest("/v1/tournaments/" + tournamentID + "/participants/" + p2ID + ".json", Method.GET);
            request.AddParameter("api_key", apiKey, ParameterType.QueryString);
            t = client.ExecuteAsync(request);
            t.Wait();
            response = await t;
            responseArray = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
            Console.WriteLine("got P2 Name");
            if(responseArray["participant"]["name"] ==  p2Name && temp == p1Name)
            {
                Console.WriteLine("** GLITCHED **");
                Thread.Sleep(1000);
                goto Glitch;
            }
            else
            {
                p1Name = temp;
                p2Name = responseArray["participant"]["name"];
            }

            Console.WriteLine(p1Name + " vs " + p2Name);
        }

        public void startMugen()
        {
            var proc = new Process();
            proc.StartInfo.FileName = "C:\\Users\\Owner\\Downloads\\mugen-1.1b1\\mugen.exe";
            proc.StartInfo.WorkingDirectory = "C:\\Users\\Owner\\Downloads\\mugen-1.1b1\\";
            proc.StartInfo.Arguments = "-p1 \"" + p1Name + "\" -p2 \"" + p2Name + "\" -s Super_Chikara_End.def -p1.color 11 -p2.color 2 -p1.ai 8 -p2.ai 8 -rounds 1";
            proc.Start();
            proc.Close();
            Console.WriteLine("loading " + p1Name + " and " + p2Name + "...");
        }

        public async Task recordWinner(int p1Score, int p2Score)
        {
            int matchID = upcomingMatches[0]["match"]["id"];
            var winnerID = 0;

            if (p1Score > p2Score)
            {
                winnerID = p1ID;
                Console.WriteLine(p1Name + " wins");
                Console.WriteLine("");
            }
            else
            {
                winnerID = p2ID;
                Console.WriteLine(p2Name + " wins");
                Console.WriteLine("");
            }

            var client = new RestClient("https://api.challonge.com");
            var request = new RestRequest("/v1/tournaments/" + tournamentID + "/matches/" + matchID + ".json", Method.PUT);
            request.AddParameter("api_key", apiKey);
            request.AddParameter("match[scores_csv]", p1Score + "-" + p2Score);
            request.AddParameter("match[winner_id]", winnerID);
            Task<IRestResponse> t = client.ExecuteAsync(request);
            t.Wait();
            var response = await t;

            Console.WriteLine("matches length: " + upcomingMatches.Count);
            if (upcomingMatches.Count == 0)
            {
                client = new RestClient("https://api.challonge.com");
                request = new RestRequest("/v1/tournaments/" + tournamentID + "/finalize.json", Method.POST);
                request.AddJsonBody(new
                {
                    api_key = apiKey
                });
                t = client.ExecuteAsync(request);
                t.Wait();
                response = await t;
                var responseArray = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                Console.WriteLine("tournament over!");
            }
        }

        public async Task getAllMatches()
        {
            var client = new RestClient("https://api.challonge.com");
            var request = new RestRequest("/v1/tournaments/" + tournamentID + "/matches.json", Method.GET);
            request.AddParameter("api_key", apiKey, ParameterType.QueryString);
            request.AddParameter("state", "open", ParameterType.QueryString);

            Task<IRestResponse> t = client.ExecuteAsync(request);
            t.Wait();
            var response = await t;
            upcomingMatches = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
            Console.WriteLine("got all matches");
        }
    }
}