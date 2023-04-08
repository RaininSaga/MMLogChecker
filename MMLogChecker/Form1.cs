using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace MMLogChecker {
    public partial class Form1 : Form {

        static readonly string filePath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\..\LocalLow\BetaDwarf ApS\Minion Masters\Player.log";
        static long nowSize = 0;
        static Encoding utf8 = Encoding.UTF8;

        static List<string> players = new List<string>();
        static Dictionary<string, string> pDic = new Dictionary<string, string>();
        static MatchData matchData = new MatchData();


        public Form1() {
            InitializeComponent();

            try {
                nowSize = new FileInfo(filePath).Length;    //初期のバイト数取得
            } catch (Exception ex) {
                //ファイルがないとか
            }

            System.Windows.Forms.Timer timer = new Timer();
            timer.Interval = 3000;
            timer.Tick += new EventHandler(Timer_ReadLog);

            timer.Start();
        }



        private void Timer_ReadLog(object sender, EventArgs e) {

            if (!File.Exists(filePath)) {
                nowSize = 0;
                label1.Text = "ログファイルが見つからない";
                return;
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    string line = null;
                    if (stream.Length <= nowSize) {
                        nowSize = stream.Length;  //変更なし、またはファイルが削除からの再生成されてるかもしれない
                        return;
                    }
                    reader.BaseStream.Seek(nowSize + 2, SeekOrigin.Begin);//前回読み込んだ位置（行）まで飛ばす
                    while ((line = reader.ReadLine()) != null) {
                        nowSize += utf8.GetByteCount(line) + 2;     //読み込んだバイト数を記録
                        CheckLine(line);
                    }
                }
            }
        }



        private void CheckLine(string line) {

            if (line.StartsWith("LobbyPlayerDataReceived: BetaDwarf.Network.Messaging.LobbyPlayerData")) {
                // プリメ用ロビー、ロビーがあるときはいろんなタイミングで発生する。
                matchData.lobby = true;
            }
            if (line.StartsWith("[APP] OnLeftPlatformLobby")) {
                // ロビーから抜ける時。直前にLobbyPlayerDataReceivedが発生するので注意
                matchData.lobby = false;
            }
            if (line.StartsWith("OnSetPlayerData From GameData. playerId: ")) {         // playeridと名前
                //OnSetPlayerData From GameData. playerId: 3 Name: saga Deck Size: 10
                //decksizeアリ→なしの順でデータがくるので後者で上書きする
                //ただし、"Players: id,id,id,id"で表示されるidの順番はdecksizeアリの方なので１回目のデータ順を記憶する
                //名前は重複する可能性がゼロではないのでキーにしない
                string[] sp = line.Split(' ');
                string id = sp[4];
                string name = line.Substring(49);

                if (pDic.ContainsKey(id)) {
                    pDic[id] = name;
                } else {
                    pDic.Add(id, name);
                }

                players.Add(id);//２回分のデータが入るけど最初の方をみるようにする
                label1.Text = "試合中...";
            }

            if (line.StartsWith("Players: ")) {         // "Players: id,id,id,id"　idがいくつあるかわからん （2v1のcoopアドベンチャーとか)
                if (pDic.Count == 0) {
                    Reset();
                    return;
                }
                string[] sp = line.Substring(9).Split(',');
                int pNum = sp.Length;
                for (int i = 0; i < pNum; i++) {

                    string name = pDic[players[i]];
                    PlayerEntity p = new PlayerEntity(sp[i], name, players[i]);

                    if (p.team == "0") {
                        matchData.team0.Add(p);
                    } else {
                        matchData.team1.Add(p);
                    }
                }


            }

            if (line.StartsWith("Match Type: ")) {         // RankedTeam、AdventureTeamとかもある
                matchData.SetMode(line.Substring(12));
            }
            if (line.StartsWith("GameDataReceived Done. Time: ")) {         // 2023/04/01 0:49:58
                string s = line.Substring(29);
                matchData.date = StringToDate(s);
            }
            if (line.StartsWith("TeamWon: ")) {         // 0or1　１試合分で３回くらい出てくるけど気にしない
                matchData.winner = line.Substring(9);
            }
            if (line.StartsWith("Results received winners")) {         // これを試合終了の目安にしたい


                //チーム平均Eloの計算
                double aveTeam0 = 0;
                foreach (PlayerEntity p in matchData.team0) { aveTeam0 += int.Parse(p.GetElo(matchData.mode)); }
                aveTeam0 /= matchData.team0.Count;
                double aveTeam1 = 0;
                foreach (PlayerEntity p in matchData.team1) { aveTeam1 += int.Parse(p.GetElo(matchData.mode)); }
                aveTeam1 /= matchData.team1.Count;
                foreach (PlayerEntity p in matchData.team0) { p.EloCal(aveTeam1, matchData); }
                foreach (PlayerEntity p in matchData.team1) { p.EloCal(aveTeam0, matchData); }

                string av0 = "  Ave: " + aveTeam0;
                string av1 = "  Ave: " + aveTeam1;
                if (matchData.mode == "Ranked") {   //ソロだからやっぱり平均要らないわ
                    av0 = "";
                    av1 = "";
                }

                string s = "";
                s += "\r\n";
                s += "-------------------------\r\n";
                s += "#Match " + matchData.date + "\r\n";
                s += "Mode: " + matchData.mode + "\r\n";
                s += "Winner: Team" + matchData.winner + "\r\n";
                s += "\r\n";
                s += "[Team0]" + av0 + "\r\n";
                foreach (PlayerEntity p in matchData.team0) {
                    s += "  " + p.name + "\r\n";
                    s += "    Elo: " + p.GetElo(matchData.mode) + " → (" + p.changeElo + ")    ID: " + p.userId + "\r\n";
                }
                s += "\r\n";
                s += "[Team1]" + av1 + "\r\n";
                foreach (PlayerEntity p in matchData.team1) {
                    s += "  " + p.name + "\r\n";
                    s += "    Elo: " + p.GetElo(matchData.mode) + " → (" + p.changeElo + ")    ID: " + p.userId + "\r\n";
                }


                textBox1.AppendText(s);
                Reset();
                label1.Text = "待機中";
            }




        }
        private void Reset() {
            matchData = new MatchData();
            players.Clear();
            pDic.Clear();
        }



        public string StringToDate(String input) {
            DateTime utcTime = DateTime.ParseExact(input, "yyyy/MM/dd H:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
            return localTime.ToString();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e) {
            if (checkBox1.Checked) {
                this.TopMost = true;
            } else {
                this.TopMost = false;
            }
        }
    }






}
