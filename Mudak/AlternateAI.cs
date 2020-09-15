using MudakCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mudak
{
    public class AlternateAI : IAI
    {
        List<Card> hand;
        List<Card> field, lastField;
        Card trump;
        int deckCards, opponentCards, lastOpponentCards;
        string name;
        List<Card> seenCards, checkDeck;
        bool opponentTookHome, lastMyTurn;

        public static readonly char[] cardSymbols = { '♠', '♣', '♥', '♦' };
        public static readonly string[] cardNominal = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        public string Name()
        {
            return name;
        }

        public AlternateAI(string name)
        {
            hand = new List<Card>();
            this.name = name;
            Random r = new Random();
            checkDeck = new List<Card>();
            for (int i = 0; i < cardSymbols.Length; i++)
            {
                for (int j = 0; j < cardNominal.Length; j++)
                {
                    checkDeck.Add(new Card(i, j));
                }
            }
            checkDeck = checkDeck.OrderBy(x => r.Next()).ToList();
            seenCards = new List<Card>();
        }

        void RememberAllCards()
        {
            List<Card> everythingNow = new List<Card>();
            everythingNow.AddRange(hand);
            everythingNow.AddRange(WholeField);
            foreach(Card c in everythingNow)
            {
                if(!seenCards.Contains(c))
                {
                    seenCards.Add(c);
                }
            }
        }

        void Log(string message)
        {
            Logger.Log(name + ": " + message);
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
            if(lastField!=null)
            {
                opponentTookHome = lastMyTurn == myTurn && opponentCards > lastOpponentCards;
            }
            lastMyTurn = myTurn;
            lastOpponentCards = opponentCards;

            RememberAllCards();

            if (myTurn)
            {
                if (UnbeatenCardsExist) goto Done;
                List<Card> toss;

                //check if field empty or it's time to damest
                if (field.Count == 0) toss = SelectCardsToToss(hand);
                else toss = SelectCardsToDamest(hand);
                while (toss.Count > opponentCards) toss.RemoveAt(0);

                if (toss == null || toss.Count == 0)
                {
                    endTurn = true;
                    goto Done;
                }
                else
                {
                    foreach (Card c in toss)
                    {
                        playCards.Add(c);
                    }
                }
                goto Done;
            }
            else
            {
                if (field.Count == 0) goto Done;

                if (!UnbeatenCardsExist)
                {
                    endTurn = true;
                    goto Done;
                }

                List<Card> counter = SelectCardsToCounter(hand);
                if(counter==null || counter.Count<UnbeatenCards.Count())
                {
                    takeHome = true;
                    goto Done;
                }
                playCards = counter;
                goto Done;
            }
        Done:
            lastField = field.ToList();
        }

        bool ShouldTossCard(Card c)
        {
            if (hand.Count == 1) return true;
            if (IsTrump(c)) {
                if (!hand.Any(x=>KillingValue(x)>KillingValue(c)) && (!hand.Any(x => !IsTrump(x)) || (deckCards == 0)))
                {
                    return true;
                }
            }else
            {
                return true;
            }
            return false;
        }

        List<Card> SelectCardsToDamest(List<Card> hand)
        {
            List<Card> ret = new List<Card>();

            List<Card> whatsLeft = new List<Card>();
            whatsLeft.AddRange(hand);
            foreach (Card c in WholeField)
            {
                var take = whatsLeft.Where(x => x.nominal == c.nominal && ShouldTossCard(x)).ToList();
                ret.AddRange(take);
                foreach (Card z in take) whatsLeft.Remove(z);
            }
            return ret;
        }

        bool IsTrump(Card c)
        {
            return c.type == trump.type;
        }

        int KillingValue(Card c)
        {
            if (IsTrump(c)) return c.nominal + 15; else return c.nominal;
        }

        IEnumerable<Card> CardsStillInGame()
        {
            return checkDeck.Where(x => !seenCards.Any(z => z.nominal == x.nominal && z.type == x.type));
        }

        bool WillEndUpWithMoreCardsNextTurn(int tossing)
        {
            if (deckCards >= 12) return false;
            int handAfterToss = hand.Count - tossing;
            int opponentCardsAfterCountering = opponentCards - tossing;
            int deckthing = deckCards;
            int taking = 6-handAfterToss;
            if(deckthing-taking<0)
            {
                handAfterToss += deckthing;
                deckthing = 0;
            }else
            {
                handAfterToss += taking;
                deckthing -= taking;
            }
            taking = 6 - opponentCardsAfterCountering;
            if (deckthing - taking < 0)
            {
                opponentCardsAfterCountering += deckthing;
                deckthing = 0;
            }else
            {
                opponentCardsAfterCountering += taking;
                deckthing -= taking;
            }
            return opponentCardsAfterCountering < handAfterToss;
        }

        bool CardStillUseful(Card c)
        {
            var csig = CardsStillInGame();
            return (csig.Any(x => CanKill(c, x)));
        }

        int CardUsefulness(Card c)
        {
            var csig = CardsStillInGame();
            return (csig.Count(x => CanKill(c, x)));
        }

        float OpponentCanCounter(Card c)
        {
            //rubbish
            var csig = CardsStillInGame();
            csig = csig.Where(x => !hand.Contains(x) && CanKill(x, c));
            if (!csig.Any()) return 0;
            return ((float)opponentCards) / ((float)csig.Count());
        }

        int CardRank(Card c)
        {
            var csig = CardsStillInGame().Where(x=>CanKill(x, c));
            return csig.Count();
        }

        List<Card> SelectCardsToToss(List<Card> hand, IEnumerable<Card> limitNominals=null)
        {
            List<Card> ret = new List<Card>();
            if (hand.Count == 0) return ret;

            Card lowestOrMultiple = null;
            int mostCardsOfNominal = 1;
            int lowest = 100;
            List<Card> shouldToss = hand.Where(x => ShouldTossCard(x)).ToList();

            if(opponentCards==1)
            {
                shouldToss.Clear();
                shouldToss.Add(hand.OrderByDescending(x => KillingValue(x)).First());
                Log("Opponent holds 1 card.");
            }

            if(deckCards==0 && opponentCards>hand.Count)
            {
                var cannotCounter = shouldToss.Where(x => OpponentCanCounter(x) == 0);
                if(cannotCounter.Count()>0)
                {
                    cannotCounter=cannotCounter.OrderByDescending(x => KillingValue(x));
                    var nominal = cannotCounter.First().nominal;
                    shouldToss = cannotCounter.Where(x => x.nominal == nominal).ToList();
                    Log("No cards in deck and opponent has more. selecting stuff he can't counter");
                }
            }

            //If has any non trump types which make up 50%+ of the available shit, prio those (haha nope.)
            /*for(int i =0;i<4;i++)
            {
                var checkDat = shouldToss.Where(x => !x.IsTrump && x.type == i);
                if (checkDat.Count()>=((float)shouldToss.Count/2f))
                {
                    shouldToss = checkDat.ToList();
                    break;
                }
            }*/

            //Tosses card with <30% chance of being blocked (whoa this sux)
            /*var lowestBlockability = shouldToss.OrderBy(x => OpponentCanCounter(x)).First();
            if(OpponentCanCounter(lowestBlockability)<30)
            {
                shouldToss.Clear();
                shouldToss.Add(lowestBlockability);
            }*/

            //Tosses types which opponent took home last turn
            /*if (opponentTookHome)
            {
                List<int> typesTookHome = lastField.Where(x => x.killer == null && ShouldTossCard(x)).Select(z => z.type).ToList();
                if (typesTookHome.Any())
                {
                    var possibleStuff=shouldToss.Where(x => typesTookHome.Any(z => x.type == z));
                    if (possibleStuff.Any()) shouldToss = possibleStuff.ToList();
                }
            }*/

            //Drops first least counter-able card at the last 3
            /*if(deckCards==0 && hand.Count<=3)
            {
                var cantCounter = hand.OrderBy(x => OpponentCanCounter(x));
                shouldToss.Clear();
                shouldToss.Add(cantCounter.First());
            }*/

            //Checks if any cards are useless, prio to those
            /*var useless = shouldToss.Where(x => CardUsefulness(x)>0);
            if (useless.Any()) shouldToss = useless.ToList();*/

            //Checks if opponent used trump to kill any cards, and uses that type of card this turn
            /*if(lastField!=null)
            {
                var killedByTrump = lastField.Where(x => x.killer!=null && x.killer.IsTrump && !x.IsTrump);
                if(killedByTrump.Any())
                {
                    var gotSome = shouldToss.Where(x => killedByTrump.Any(z => z.type == x.type));
                    if(gotSome.Any())
                    {
                        shouldToss = gotSome.ToList();
                    }
                }
            }*/
            Log("Cards I should toss: " + shouldToss.Select(x => x.ToString()).Aggregate((i, j) => i + ' ' + j));
            foreach (Card c in shouldToss)
            {
                if (c.nominal < lowest && (limitNominals==null || limitNominals.Any(z=>c.nominal==z.nominal))) lowest = c.nominal;
                var possibleCards = shouldToss.Where(x => x.nominal == c.nominal);
                if (possibleCards.Count() > mostCardsOfNominal)
                {
                    mostCardsOfNominal = possibleCards.Count();
                    lowestOrMultiple = c;
                }
            }

            //maybe shouldn't multiple-toss cards better than average left in game (nah)
            /*if (lowestOrMultiple!=null)
            {
                var multiples=shouldToss.Where(x => x.nominal == lowestOrMultiple.nominal);

                if(CardRank(multiples.OrderByDescending(x=>x.KillingValue).First())<(deckCards+hand.Count+opponentCards))
                {
                    lowestOrMultiple = null;
                }
            }*/

            //doesn't multiple-toss high rank items early (nah)
            //if(lowestOrMultiple!=null) if (((float)CardRank(lowestOrMultiple) / (float)(CardsStillInGame().Count())) < 0.2) lowestOrMultiple = null;

            //didn't find any multiples
            if (lowestOrMultiple == null)
            {
                ret.Add(shouldToss.First(x => x.nominal == lowest));
                Log("No multiples to toss. Selecting lowest: "+ret.First().ToString());
            }
            else
            {
                ret.AddRange(shouldToss.Where(x => x.nominal == lowestOrMultiple.nominal));
                Log("Multiples found: " + ret.Select(x => x.ToString()).Aggregate((i, j) => i + ' ' + j));
            }
            if (ret.Count > opponentCards)
            {
                ret = ret.Take(opponentCards).ToList();
                if(ret.Any()) Log("Opponent has less cards than I'm tossing. Tossing less: " + ret.Select(x => x.ToString()).Aggregate((i, j) => i + ' ' + j));
            }

            return ret.OrderBy(x => KillingValue(x)).ToList();
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

        bool ShouldUseCounter(Card c)
        {
            if(CardRank(c)>opponentCards+deckCards+1)
            {
                return false;
            }
            return true;
        }

        List<Card> SelectCardsToCounter(List<Card> hand)
        {
            List<Card> ret = new List<Card>();
            List<Card> availableByKillingPower = hand.Where(z=>ShouldUseCounter(z)).OrderBy(x => KillingValue(x)).ToList();
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
