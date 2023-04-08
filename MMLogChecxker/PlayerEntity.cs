using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;

namespace MMLogChecker {
    public class PlayerEntity {
        public string userId = "";
        public string name = "";
        public string teamId = "";
        public string eloSolo = "0";
        public string eloRandom = "0";
        public string eloPremade = "0";
        public int changeElo = 0;
        public string team = "";
        public PlayerEntity() { }
        public PlayerEntity(string uid, string nm, string tid) {
            userId = uid;
            name = nm;
            teamId = tid;
            if (teamId == "0" || teamId == "2") { team = "0"; } else { team = "1"; }
            if (!(uid == "0" || uid == "")) {
                EloRequest(uid);
            }
        }

        private void EloRequest(string id) {
            string eloURL = "http://fdmfdm.nl/GetUserElo.php?userID=";

            //デバグで鯖に負荷かけないようにする用
            int debug = 0;
            if (debug == 1) {
                eloSolo = "1111";
                eloRandom = "2222";
                eloPremade = "3333";
                return;
            }

            try {
                using (WebClient client = new WebClient()) {
                    string res = client.DownloadString(eloURL + id);
                    Console.WriteLine(res);

                    JObject dataDict = JArray.Parse(res)[0] as JObject;

                    // 辞書から値を取得
                    //string Id = (string)dataDict["Id"];
                    //string User_id = (string)dataDict["User_id"];
                    eloSolo = (string)dataDict["Elo1v1"];
                    eloRandom = (string)dataDict["Elo2v2Solo"];
                    eloPremade = (string)dataDict["Elo2v2Team"];

                }
            } catch (WebException ex) {
                Console.WriteLine(ex.Message);
            }

        }
        public string GetElo(string mode) {
            switch (mode) {
                case "Ranked": return eloSolo;
                case "RankedTeam": return eloRandom;
                case "PremadeTeam": return eloPremade;
                default: return "0";
            }
            return "-";
        }

        public int EloCal(double opponentAve, MatchData match) {
            string myTeam = (int.Parse(teamId) % 2).ToString();
            int myElo = int.Parse(this.GetElo(match.mode));
            if (match.winner == myTeam) {
                changeElo = myElo + Cal(myElo, opponentAve);
            } else {
                changeElo = myElo - Cal(opponentAve, myElo);
            }
            return changeElo;
        }
        private static int Cal(double winnerElo, double loserElo) {
            int kFactor = 40;
            double aa = (kFactor * ((1.0 / ((1.0 + Math.Pow(10.0, (double)(winnerElo - loserElo) / 400.0))))));
            return (int)aa;
        }

    }
}
