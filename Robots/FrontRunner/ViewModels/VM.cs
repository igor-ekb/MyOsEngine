using OsEngine.Commands;
using OsEngine.Entity;
using OsEngine.Robots.FrontRunner.Models;
using OsEngine.Robots.MyRobots.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OsEngine.Robots.FrontRunner.ViewModels
{
    public class VM: BaseVM
    {
        public VM(Models.FrontRunner bot)
        {
            _bot = bot;
        }

        #region Fields =================================================================

        private Models.FrontRunner _bot;

        #endregion

        #region Properties ===============================================================

        public decimal BigVolume
        {
            get => _bot.BigVolume;
            set
            {
                _bot.BigVolume = value;

                OnPropertyChanged(nameof(BigVolume));
            }
        }
        

        public int Offset
        {
            get => _bot.Offset;
            set
            {
                _bot.Offset = value;

                OnPropertyChanged(nameof(Offset));
            }
        }

        public int Take
        {
            get => _bot.Take;
            set
            {
                _bot.Take = value;

                OnPropertyChanged(nameof(Take));
            }
        }

        public decimal Lot
        {
            get => _bot.Lot;
            set
            {
                _bot.Lot = value;

                OnPropertyChanged(nameof(Lot));
            }
        }

        public Edit Edit
        {
            get => _bot.Edit;

            set
            {
                _bot.Edit = value;

                OnPropertyChanged(nameof(Edit));
            }
        }

        public string BidBig
        {
            get => _bot.BidBig;
            set
            {
                _bot.BidBig = value;

                OnPropertyChanged(nameof(BidBig));
            }
        }

        public string UseLong
        {
            get => ((StrategyParameterString)_bot.Parameters[0]).ValueString;
            set
            {
                OnPropertyChanged(nameof(UseLong));
                ((StrategyParameterString)_bot.Parameters[0]).ValueString = value;
            }
        }

        public string UseShort
        {
            get => ((StrategyParameterString)_bot.Parameters[1]).ValueString;
            set
            {
                OnPropertyChanged(nameof(UseShort));
                ((StrategyParameterString)_bot.Parameters[1]).ValueString = value;
            }
        }


        public string CheckPos
        {
            get => ((StrategyParameterString)_bot.Parameters[2]).ValueString;
            set
            {
                OnPropertyChanged(nameof(UseShort));
                ((StrategyParameterString)_bot.Parameters[2]).ValueString = value;
            }
        }

        #endregion

        #region Commands ==================================================================

        private DelegateCommand commandStart;

        public ICommand CommandStart
        {
            get
            {
                if (commandStart == null)
                {
                    commandStart = new DelegateCommand(Start);
                }

                return commandStart;
            }
        }

        #endregion


        #region Methods ================================================================

        private void Start(object obj)
        {
            if (Edit == Edit.Start)
            {
                Edit = Edit.Stop;
            }
            else
            {
                Edit = Edit.Start;
            }
        }

        #endregion
    }

    public enum Edit
    {
        Start,

        Stop
    }

}
