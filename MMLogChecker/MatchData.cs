using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLogChecker {
    public class MatchData {
        public string mode = "";    //None,Ranked,Friend,Draft,RankedTeam,FriendTeam,TavernBrawl,TavernBrawlTeam,AdventureTeam
        public bool lobby = false;
        public string date = "";
        public string winner = "";// 0or1
        public List<PlayerEntity> team0 = new List<PlayerEntity>();
        public List<PlayerEntity> team1 = new List<PlayerEntity>();
        public MatchData() { }

        public void SetMode(string m) {
            mode = m;
            if (m == "RankedTeam" && lobby == true) {
                mode = "PremadeTeam";
            }
            if (m == "FriendTeam") {    //確認できてないけど念のため
                mode = "PremadeTeam";
            }
        }
    }
}
