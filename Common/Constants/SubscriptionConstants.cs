using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Constants
{
    public static class SubscriptionConstants
    {
        public const string FREE = "Free";
        public const string BASIC_5 = "Basic5";
        public const string BASIC_10 = "Basic10";
        public const string BASIC_15 = "Basic15";
      

        public static Dictionary<string, int> SubscriptionQuotas = new()
        {
            { FREE, 2 },
            { BASIC_5, 5 },
            { BASIC_10, 10 },
            { BASIC_15, 15 },
         
        };

        public static Dictionary<string, decimal> SubscriptionPrices = new()
        {
            { FREE, 0 },
            { BASIC_5, 10},
            { BASIC_10, 28 },
            { BASIC_15, 35 },
     
        };
    }
}

