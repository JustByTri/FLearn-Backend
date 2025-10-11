using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Type
{
    public enum ConversationSessionStatus
    {
        Active = 1,
        Completed = 2,
        Paused = 3,
        Abandoned = 4,
        Evaluated = 5
    }
}
