using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.MyRobots.ViewModels
{
    public class VMMyRobot : BaseVM
    {
        public VMMyRobot(MyRobot robot)
        {
            _robot = robot;

        }
        private MyRobot _robot;

        public decimal Lot
        {
            get => ((StrategyParameterDecimal)_robot.Parameters[1]).ValueDecimal;

            set
            {
                OnPropertyChanged(nameof(Lot));
                ((StrategyParameterDecimal)_robot.Parameters[1]).ValueDecimal = value;
            }
        }

        public int Take
        {
            get => ((StrategyParameterInt)_robot.Parameters[3]).ValueInt;

            set
            {
                OnPropertyChanged(nameof(Take));
                ((StrategyParameterInt)_robot.Parameters[3]).ValueInt = value;
            }
        }

        public int Stop
        {
            get => ((StrategyParameterInt)_robot.Parameters[2]).ValueInt;

            set
            {
                OnPropertyChanged(nameof(Stop));
                ((StrategyParameterInt)_robot.Parameters[2]).ValueInt = value;
            }
        }

    }
}
