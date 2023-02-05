using OsEngine.Market;
using OsEngine.Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.MyEntity
{
    public class ExChange: BaseVM
    {
        public ExChange(ServerType type)
        {
            Server = type;
        }

        public ServerType Server
        {
            get => _server;
            set
            {
                _server = value;
                OnPropertyChanged(nameof(Server));
            }
        }
        public ServerType _server;
    }
}
