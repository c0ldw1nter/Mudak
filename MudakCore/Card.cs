using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MudakCore
{
    public class Card
    {
        public int type;
        public int nominal;
        public Card killer;

        public Card(int type, int nominal)
        {
            if (type < 0 || type > Core.cardSymbols.Length - 1 || nominal < 0 || nominal > Core.cardNominal.Length - 1) throw new Exception("WTF man.");
            this.type = type;
            this.nominal = nominal;
        }

        public override string ToString()
        {
            return Core.cardSymbols[type] + Core.cardNominal[nominal];
        }
    }
}
