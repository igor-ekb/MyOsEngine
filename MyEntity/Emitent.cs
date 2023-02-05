using OsEngine.Entity;
using OsEngine.Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.MyEntity
{
    public class Emitent : BaseVM
    {
        public Emitent(Security security)
        {
            Security = security;
        }
        public Security Security;

        public string NameSec
        {
            get => Security.Name;
        }

        public string NameId
        {
            get => Security.NameId;
        }

        public string NameFull
        {
            get => Security.NameFull;
        }

        public string NameClass
        {
            get => Security.NameClass;
        }

        public decimal PriceStep
        {
            get => Security.PriceStep;
        }

        public decimal PriceStepCost
        {
            get => Security.PriceStepCost;
        }

        public int DecimalPrice
        {
            get => Security.Decimals;
        }

        public int DecimalVolume
        {
            get => Security.DecimalsVolume;
        }
    }
}
