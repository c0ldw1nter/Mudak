using MudakCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mudak
{
    public class Program
    {
        public static bool showOpponentCards = false;
        public static bool playerInputEntered = true;
        public static bool showEveryMove = false;
        public static int[] wins = new int[2];
        public static int draws;
        public static int gamesLeft;
        public static bool automatedGame = false;
        public static int autoGames = 1000;
        public static Core core;
        public static IAI player1, player2;
        public static string warning;

        static string WinPercentage(int player)
        {
            decimal playa;
            if (player == -1) playa = draws;
            else playa = wins[player];
            decimal total = wins[0] + wins[1] + draws;
            if (total == 0) return "0";

            return string.Format("{0:0.00}", (playa / total) * 100);
        }

        public static void StartNewGame()
        {

        }

        static void WriteLineCards(string str, params Card[] cards)
        {
            var kards = cards.ToList();
            string normalTxt = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '$' && kards.Any())
                {
                    Console.Write(normalTxt);
                    Console.ForegroundColor = kards.First().type < 2 ? ConsoleColor.DarkGray : ConsoleColor.Red;
                    Console.Write(kards.First().ToString());
                    Console.ForegroundColor = ConsoleColor.Gray;
                    kards.RemoveAt(0);
                    normalTxt = "";
                }
                else normalTxt += str[i];
            }
            Console.WriteLine(normalTxt);
        }

        public static void PrintSituation(GameState gs)
        {
            Console.Clear();
            if (!automatedGame || showEveryMove)
            {
                List<Card> cards = new List<Card>();
                WriteLineCards("Trump: $ | Deck: " + gs.deck.Count + "| Turn: " + (gs.turn == 1 ? player2.Name() : player1.Name()), gs.trump);
                int three = 1;
                if (!showOpponentCards) Console.WriteLine("O: " + String.Concat(Enumerable.Repeat("|", gs.hands[1].Count)));
                else
                {
                    string ohand = "O: ";
                    foreach (Card c in gs.hands[1])
                    {
                        ohand += c.ToString() + " ";
                        if (three == 3) { ohand += " "; three = 1; } else three++;
                    }
                    Console.WriteLine(ohand);
                }

                Console.WriteLine();
                string fieldString = "F: ";
                for (int i = 0; i < gs.field.Count; i++)
                {
                    fieldString += '$';
                    cards.Add(gs.field[i]);
                    if (gs.field[i].killer != null)
                    {
                        fieldString += "/" + "$";
                        cards.Add(gs.field[i].killer);
                    }
                    fieldString += ' ';
                }
                WriteLineCards(fieldString, cards.ToArray());
                cards.Clear();
                Console.WriteLine();
                gs.hands[0] = gs.hands[0].OrderBy(x => KillingValue(x)).ToList();
                string phand = "P: ";
                three = 1;
                foreach (Card c in gs.hands[0])
                {
                    phand += '$' + " ";
                    cards.Add(c);
                    if (three == 3) { phand += " "; three = 1; } else three++;
                }
                WriteLineCards(phand, cards.ToArray());
            }
            else if (automatedGame || gs.gameOver)
            {
                if (gamesLeft > 0)
                {
                    Console.WriteLine($"{autoGames-gamesLeft}/{autoGames}");
                }
                Console.WriteLine($"{player1.Name()} won: {wins[0]} ({WinPercentage(0)}%)");
                Console.WriteLine($"{player2.Name()} won: {wins[1]} ({WinPercentage(1)}%)");
                Console.WriteLine($"Draws: {draws} ({WinPercentage(-1)}%)");
            }
            if (!String.IsNullOrEmpty(gs.warning)) Console.WriteLine(Environment.NewLine + gs.warning);
            if (!String.IsNullOrEmpty(warning)) Console.WriteLine(Environment.NewLine + warning);
        }

        static bool IsTrump(Card c)
        {
            return c.type == core.Trump.type;
        }

        static int KillingValue(Card c)
        {
            if (IsTrump(c)) return c.nominal + 15; else return c.nominal;
        }

        static DateTime lastAutoUpdate = DateTime.Now;
        static TimeSpan autoUpdateInterval = TimeSpan.FromMilliseconds(200);

        static void ReadArgs(string[] args)
        {
            if (args.Any(x => x.ToLower().Equals("-a")))
            {
                var games = args.FirstOrDefault(x => x.ToLower().StartsWith("-g")).Split(':');
                if (games.Length == 2 && Int32.TryParse(games[1], out autoGames))
                {
                    automatedGame = true;
                    gamesLeft = autoGames;
                    showOpponentCards = true;
                    player1 = new DefaultAI("Default AI");
                    player2 = new AlternateAI("Alternate AI");
                    //players[1] = new IdiotAI("Idiot AI");
                    core = new Core(player1, player2);
                    goto EndThis;
                }
            }
            player1 = new Player();
            player2 = new AlternateAI("CPU");
            core = new Core(player1, player2);
        EndThis:
            Logger.Log($"Mode: automatic={automatedGame}, autoGames={autoGames}, Player 1: {player1.Name()}, Player 2: {player2.Name()}");
        }

        static void Main(string[] args)
        {
            ReadArgs(args);
            for (int i = 0; i < wins.Length; i++) wins[i] = 0;
            draws = 0;
            Console.OutputEncoding = Encoding.UTF8;
            core.StartNewGame();
            Logger.Log("Started new game");
            PrintSituation(core.State);
            while (true)
            {
                var state = core.DoTurn();

                if (automatedGame)
                {
                    if (DateTime.Now - lastAutoUpdate > autoUpdateInterval)
                    {
                        PrintSituation(state);
                        lastAutoUpdate = DateTime.Now;
                    }
                }
                else
                    PrintSituation(state);

                if (state.gameOver)
                {
                    if (state.winner == -1) draws++;
                    else wins[state.winner]++;
                    

                    if (automatedGame)
                    {
                        if (gamesLeft > 0)
                        {
                            core.StartNewGame();
                            Logger.Log("Started new game");
                            gamesLeft--;
                        }
                        else
                        {
                            PrintSituation(state);
                            Console.WriteLine("Exiting...");
                            Console.ReadKey();
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Press enter to restart.");
                        Console.ReadKey();
                        core.StartNewGame();
                        Logger.Log("Started new game");
                        PrintSituation(core.State);
                    }
                }
            }
        }
    }
}
