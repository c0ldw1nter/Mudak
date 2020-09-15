using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudakCore
{
    public class GameState
    {
        public List<Card> deck, field;
        public List<Card>[] hands;
        public Card trump;
        public string warning;
        public int turn;
        public bool gameOver;
        public int winner;
    }
}
