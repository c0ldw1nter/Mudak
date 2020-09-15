using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudakCore
{
    public class Core
    {
        public static readonly char[] cardSymbols = { '♠', '♣', '♥', '♦' };
        public static readonly string[] cardNominal = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        public List<Card> deck, field;
        public  int turn;
        public Random r = new Random();
        public string warning = "";
        public IAI[] players = new IAI[2];
        public List<Card>[] hands = new List<Card>[2];
        public Card lastTrump;
        public bool showOpponentCards = false;
        public bool playerInputEntered = true;
        bool endGame;
        int winner;

        public Core(IAI p1, IAI p2)
        {
            players[0] = p1;
            players[1] = p2;
        }

        public Card Trump
        {
            get
            {
                if (deck.Any()) lastTrump = deck.Last();
                return lastTrump;
            }
        }

        void MakeStandardShuffledDeck()
        {
            deck = new List<Card>();
            hands[0] = new List<Card>();
            hands[1] = new List<Card>();
            for (int i = 0; i < cardSymbols.Length; i++)
            {
                for (int j = 0; j < cardNominal.Length; j++)
                {
                    deck.Add(new Card(i, j));
                }
            }
            deck = deck.OrderBy(x => r.Next()).ToList();
        }

        void DrawEnough(List<Card> hand)
        {
            while (hand.Count < 6)
            {
                if (deck.Count == 0) return;
                hand.Add(deck.First());
                deck.Remove(deck.First());
            }
        }

        public void StartNewGame()
        {
            endGame = false;
            warning = "";
            MakeStandardShuffledDeck();
            field = new List<Card>();
            turn = r.Next(0, 2);
            DrawEnough(hands[0]);
            DrawEnough(hands[1]);
        }

        void EndTurn(int player, bool tookHome)
        {
            if (turn != player)
            {
                warning = $"Player {player + 1} can't end turn as it isn't his turn.";
                return;
            }
            if (!tookHome) turn = 1 - turn;
            field = new List<Card>();
            if (player == 1)
            {
                DrawEnough(hands[1]);
                DrawEnough(hands[0]);
            }
            else
            {
                DrawEnough(hands[0]);
                DrawEnough(hands[1]);
            }
            CheckEndGame();
        }

        void CheckEndGame()
        {
            if (hands[1].Count == 0 && hands[0].Count == 0)
            {
                warning = "It's a draw.";
                winner = -1;
                endGame = true;
                return;
            }
            for (int i = 0; i < hands.Length; i++)
            {
                if (!hands[i].Any())
                {
                    warning = players[i].Name() + " wins";
                    winner = i;
                    endGame = true;
                    break;
                }
            }
        }

        public List<Card> WholeField
        {
            get
            {
                var c = new List<Card>();
                foreach (Card z in field)
                {
                    c.Add(z);
                    if (z.killer != null)
                    {
                        c.Add(z.killer);
                    }
                }
                return c;
            }
        }

        void TakeHome(List<Card> hand)
        {
            foreach (Card c in WholeField)
            {
                hand.Add(c);
            }
            foreach (Card c in hand)
            {
                c.killer = null;
            }
            field.Clear();
            EndTurn(turn, true);
        }

        bool UnbeatenCardsExist
        {
            get { return field.Any(x => x.killer == null); }
        }

        IEnumerable<Card> UnbeatenCards
        {
            get { return field.Where(x => x.killer == null); }
        }

        bool CheckValidity(int player, List<Card> cards, bool takeHome, bool endTurn)
        {
            if (endGame) return true;
            if (takeHome && (cards.Any() || player == turn))
            {
                warning = "Can't take home cards during your turn OR playing any cards";
                return false;
            }
            if (endTurn && turn != player)
            {
                warning = "Player can't end turn as it isn't his turn";
                return false;
            }
            if (player == turn)
            {
                if (field.Count == 0)
                {
                    if (cards == null || cards.Count == 0)
                    {
                        warning = "Must play some cards during your turn.";
                        return false;
                    }
                    if (cards.Any(x => x.nominal != cards.First().nominal))
                    {
                        warning = "Card nominals don't match.";
                        return false;
                    }
                    if (cards.Count > hands[1 - player].Count)
                    {
                        warning = "Cannot toss more cards than your opponent has in their hand.";
                        return false;
                    }
                }
                else
                {
                    if (cards.Any(x => !WholeField.Any(z => z.nominal == x.nominal)))
                    {
                        warning = "Invalid cards to damest.";
                        return false;
                    }
                }
            }
            else
            {
                if (UnbeatenCardsExist)
                {
                    if (takeHome)
                    {
                        return true;
                    }
                    if (cards?.Count != UnbeatenCards.Count())
                    {
                        warning = "Invalid amount of cards to counter.";
                        return false;
                    }
                    for (int i = 0; i < UnbeatenCards.Count(); i++)
                    {
                        if (!CanKill(cards[i], UnbeatenCards.ElementAt(i)))
                        {
                            warning = $"{cards[i].ToString()} can't kill {UnbeatenCards.ElementAt(i).ToString()}";
                            return false;
                        }
                    }
                }
                else
                {
                    if (cards != null && cards.Any())
                    {
                        warning = $"Player {player} Cannot play cards at this stage.";
                        return false;
                    }
                }
            }
            return true;
        }

        bool IsTrump(Card c)
        {
            return c.type == Trump.type;
        }

        int KillingValue(Card c)
        {
            if (IsTrump(c)) return c.nominal + 15; else return c.nominal;
        }

        bool CanKill(Card card, Card otherCard)
        {
            return (card.type == otherCard.type || IsTrump(card)) && KillingValue(card) > KillingValue(otherCard);
        }

        void PerformTurn(int player, List<Card> cards)
        {
            if (cards == null) return;
            if (!cards.Any()) return;

            List<Card> hand = hands[player];
            if (turn == player)
            {
                field.AddRange(cards);
                foreach (Card c in cards) hand.Remove(c);
            }
            else
            {
                var unbeaten = UnbeatenCards.ToList();
                for (int i = 0; i < cards.Count; i++)
                {
                    unbeaten[i].killer = cards[i];
                    hand.Remove(cards[i]);
                }
            }
        }

        public GameState State
        {
            get
            {
                return new GameState()
                {
                    deck = deck,
                    field = field,
                    hands = hands,
                    trump = Trump,
                    warning = warning,
                    turn = turn,
                    gameOver = endGame,
                    winner = winner
                };
            }
        }

        public GameState DoTurn()
        {
            bool takeHome, endTurn;
            List<Card> playCards;
            bool validTurn = true;
            for (int p = 0; p < players.Length; p++)
            {
                do
                {
                    validTurn = true;
                    warning = "";
                    players[p].DoMove(hands[p].ToList(), field.ToList(), Trump, deck.Count, hands[1 - p].Count, turn == p, out playCards, out takeHome, out endTurn);
                    if (!CheckValidity(p, playCards, takeHome, endTurn))
                    {
                        //bad
                        validTurn = false;
                        if(string.IsNullOrWhiteSpace(warning)) warning = "Invalid turn";
                        goto EndThis;
                    }
                    if (takeHome)
                    {
                        TakeHome(hands[p]);
                    }
                    else if (endTurn)
                    {
                        EndTurn(turn, false);
                    }
                    else
                    {
                        PerformTurn(p, playCards);
                    }
                } while (!validTurn);
            }
            EndThis:
            return new GameState()
            {
                deck = deck,
                field = field,
                hands = hands,
                trump = Trump,
                warning = warning,
                turn = turn,
                gameOver=endGame,
                winner=winner
            };
        }
    }

    public static class CardExtensions
    {
        public static bool IsTrump(this Card c, Core core)
        {
            return c.type == core.Trump.type;
        }
    }
}
