using MudakCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mudak
{
    public class IdiotAI : IAI
    {
        List<Card> hand;
        List<Card> field;
        Card trump;
        int deckCards, opponentCards;
        string name;
        Random r;

        public string Name()
        {
            return name;
        }

        public IdiotAI(string name)
        {
            hand = new List<Card>();
            this.name = name;
            r = new Random();
        }
        public void DoMove(List<Card> hand, List<Card> field, Card trump, int deckCards, int opponentCards, bool myTurn, out List<Card> playCards, out bool takeHome, out bool endTurn)
        {
            this.hand = hand;
            this.field = field;
            this.trump = trump;
            this.deckCards = deckCards;
            this.opponentCards = opponentCards;
            playCards = new List<Card>();
            takeHome = false;
            endTurn = false;

            if (myTurn)
            {
                if (hand.Count == 0) { endTurn = true; return; }
                if (UnbeatenCardsExist) return;
                List<Card> toss;

                //check if field empty or it's time to damest
                if (field.Count == 0) toss = SelectCardsToToss(hand);
                else toss = SelectCardsToDamest(hand);
                while (toss.Count > opponentCards) toss.RemoveAt(0);

                if (toss == null || toss.Count == 0)
                {
                    endTurn = true;
                    return;
                }
                else
                {
                    foreach (Card c in toss)
                    {
                        playCards.Add(c);
                    }
                }
                return;
            }
            else
            {
                if (field.Count == 0) return;

                if (!UnbeatenCardsExist)
                {
                    endTurn = true;
                    return;
                }

                List<Card> counter = SelectCardsToCounter(hand);
                if(counter==null || counter.Count<UnbeatenCards.Count())
                {
                    takeHome = true;
                    return;
                }
                playCards = counter;
                return;
            }
            return;
        }

        List<Card> SelectCardsToDamest(List<Card> hand)
        {
            List<Card> ret = new List<Card>();
            List<Card> whatsLeft = new List<Card>();
            whatsLeft.AddRange(hand);
            foreach (Card c in WholeField)
            {
                var take = whatsLeft.Where(x => x.nominal == c.nominal).ToList();
                ret.AddRange(take);
                foreach (Card z in take) whatsLeft.Remove(z);
            }
            return ret;
        }
        List<Card> SelectCardsToToss(List<Card> hand, IEnumerable<Card> limitNominals=null)
        {
            List<Card> ret = new List<Card>();
            ret.Add(hand[r.Next(0, hand.Count)]);

            return ret;
        }

        bool UnbeatenCardsExist
        {
            get { return field.Any(x => x.killer == null); }
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

        IEnumerable<Card> UnbeatenCards
        {
            get { return field.Where(x => x.killer == null); }
        }

        public bool CanKill(Card card, Card otherCard)
        {
            return (card.type==otherCard.type || IsTrump(card)) && KillingValue(card) > KillingValue(otherCard);
        }

        int KillingValue(Card c)
        {
            if (IsTrump(c)) return c.nominal + 15; else return c.nominal;
        }

        bool IsTrump(Card c)
        {
            return c.type == trump.type;
        }

        List<Card> SelectCardsToCounter(List<Card> hand)
        {
            List<Card> ret = new List<Card>();
            List<Card> availableByKillingPower = hand.OrderBy(x => KillingValue(x)).ToList();
            foreach(Card c in UnbeatenCards)
            {
                bool found = false;
                for(int i=0;i<availableByKillingPower.Count;i++)
                {
                    if(CanKill(availableByKillingPower[i], c))
                    {
                        ret.Add(availableByKillingPower[i]);
                        availableByKillingPower.RemoveAt(i);
                        found = true;
                        break;
                    }
                }
                if (!found) return null;
            }
            return ret;
        }
    }
}
