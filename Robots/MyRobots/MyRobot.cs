using OsEngine.Entity;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.OsTrader.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.MyRobots
{
    public class MyRobot : BotPanel
    {
        public MyRobot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            this.TabCreate(BotTabType.Simple);

            _tab = TabsSimple[0];

            this.CreateParameter("Mode", "Edit", new[] { "Edit", "Trade" });

            this.CreateParameter("Lot", 1m, 1m, 100m, 1m);

            this.CreateParameter("Stop", 38, 1, 100, 1);

            this.CreateParameter("Take", 40, 1, 100, 1);

        }

        #region Fields ==================================================================

        private BotTabSimple _tab;

        #endregion


        #region Methods =================================================================
        public override string GetNameStrategyType()
        {
            return nameof(MyRobot);
        }

        public override void ShowIndividualSettingsDialog()
        {
            WindowMyRobot window = new WindowMyRobot(this);

            window.ShowDialog();
        }

        #endregion

    }
}