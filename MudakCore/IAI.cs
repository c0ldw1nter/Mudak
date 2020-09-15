using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudakCore
{
    public interface IAI
    {
        void DoMove(List<Card> hand, List<Card> field, Card trump, int deckCards, int opponentCards, bool myTurn, out List<Card> playCards, out bool takeHome, out bool endTurn);
        string Name();
    }
}
