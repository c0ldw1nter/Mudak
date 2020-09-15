using MudakCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mudak
{
    class Player : IAI
    {
        List<Card> hand;
        List<Card> field;
        Card trump;
        int deckCards, opponentCards;
        bool myTurn;

        public void DoMove(List<Card> hand, List<Card> field, Card trump, int deckCards, int opponentCards, bool myTurn, out List<Card> playCards, out bool takeHome, out bool endTurn)
        {
            this.field = field;
            this.trump = trump;
            this.deckCards = deckCards;
            this.opponentCards = opponentCards;
            this.myTurn = myTurn;
            this.hand = hand;
            playCards = null;
            takeHome = endTurn = false;
            if (!myTurn && !UnbeatenCards.Any()) return;
            InterpretInput(Console.ReadLine(), out playCards, out takeHome, out endTurn);
            Program.playerInputEntered = true;
        }

        public Player()
        {
            hand = new List<Card>();
        }

        void TakeHome()
        {
            if (!myTurn)
            {
                foreach (Card c in field)
                {
                    if (c.killer != null) { hand.Add(c.killer); c.killer = null; }
                    hand.Add(c);
                }
                return;
            }
            Program.warning = "Can't take cards during your own turn.";
        }

        void InterpretInput (string input, out List<Card> playCards, out bool takeHome, out bool endTurn)
        {
            input = input.Trim();
            takeHome = false;
            endTurn = false;
            playCards = new List<Card>();
            Program.warning = "";
            if (input == "-")
            {
                takeHome = true;
                return;
            }
            else if (String.IsNullOrEmpty(input))
            {
                endTurn = myTurn && field.Count > 0;
                return;
            }
            else if (input.ToLower() == "n")
            {
                Program.StartNewGame();
                return;
            }
            else
            {
                var splice = input.Split(' ');
                bool fault = false;
                List<Card> toPlay = new List<Card>();
                foreach (string s in splice)
                {
                    if (!Int32.TryParse(s, out int index)) { fault = true; break; }
                    if (index > hand.Count || index < 1) { fault = true; break; }
                    index--;
                    Card theCard = hand[index];
                    if (toPlay.Contains(theCard)) { fault = true; break; }
                    toPlay.Add(theCard);
                }
                if (!fault)
                {
                    playCards = toPlay;
                    return;
                    //return PlayCards(toPlay);
                }
            }
            Program.warning = "Invalid input.";
            return;
        }



        IEnumerable<Card> UnbeatenCards
        {
            get { return field.Where(x => x.killer == null); }
        }

        /*int PlayCards(List<Card> cards)
        {
            if (!myTurn)
            {
                if (UnbeatenCards.Count() != cards.Count)
                {
                    Program.warning = "Invalid amount of cards played";
                    return 0;
                }

                var unbeaten = UnbeatenCards.ToList();
                for (int i = 0; i < cards.Count; i++)
                {
                    var toKill = unbeaten.ElementAt(i);
                    if (cards[i].KillingValue < toKill.KillingValue || (!cards[i].IsTrump && cards[i].type != toKill.type))
                    {
                        Program.warning = $"{cards[i].ToString()} can't kill {toKill.ToString()}";
                        return 0;
                    }
                }

                for (int i = 0; i < cards.Count; i++)
                {
                    var toKill = unbeaten.ElementAt(i);
                    toKill.killer = cards[i];
                    hand.Remove(cards[i]);
                }
                return 0;
            }
            else
            {
                if (field.Count == 0)
                {
                    int nom = cards[0].nominal;
                    for (int i = 0; i < cards.Count; i++)
                    {
                        if (cards[i].nominal != nom)
                        {
                            Program.warning = "Invalid cards played";
                            return InterpretInput(Console.ReadLine());
                        }
                    }
                    foreach (Card c in cards)
                    {
                        field.Add(c);
                        hand.Remove(c);
                    }
                }
                else
                {
                    foreach (Card c in cards)
                    {
                        if (!WholeField.Any(x => c.nominal == x.nominal))
                        {
                            Program.warning = "Invalid cards played";
                            return 0;
                        }
                    }
                    foreach (Card c in cards)
                    {
                        field.Add(c);
                        hand.Remove(c);
                    }
                }
                return 0;
            }
        }*/
        List<Card> WholeField
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

        public string Name()
        {
            return "Player";
        }
    }
}
