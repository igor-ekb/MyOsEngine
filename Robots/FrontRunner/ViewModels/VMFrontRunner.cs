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
    public class VMFrontRunner: BaseVM
    {
        public VMFrontRunner(Models.FrontRunner bot)
        {
            _bot = bot;

            // Подписываемся на событие в роботе (считывание полей для вывода в window)
            _bot.EventMD += _bot_EventMD;
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

        public string BigBid
        {
            get => _bot.BigBid;
            set
            {
                _bot.BigBid = value;
            }
        }

        public string BigAsk
        {
            get => _bot.BigAsk;
            set
            {
                _bot.BigAsk = value;
            }
        }

        public YesNo UseLong
        {
            get => _bot.UseLong;
            set
            {
                _bot.UseLong = value;

                OnPropertyChanged(nameof(UseLong));

            }
        }

        public YesNo UseShort
        {
            get => _bot.UseShort;
            set
            {
                _bot.UseShort = value;

                OnPropertyChanged(nameof(UseShort));
            }
        }


        //public string CheckPos
        //{
        //    get => ((StrategyParameterString)_bot.Parameters[0]).ValueString;
        //    set
        //    {
        //        OnPropertyChanged(nameof(CheckPos));
        //        ((StrategyParameterString)_bot.Parameters[0]).ValueString = value;
        //    }
        //}

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

        private DelegateCommand commandLong;
        public ICommand CommandLong
        {
            get
            {
                if (commandLong == null)
                {
                    commandLong = new DelegateCommand(StartLong);
                }

                return commandLong;
            }
        }

        private DelegateCommand commandShort;
        public ICommand CommandShort
        {
            get
            {
                if (commandShort == null)
                {
                    commandShort = new DelegateCommand(StartShort);
                }

                return commandShort;
            }
        }

        #endregion


        #region Methods ================================================================

        private void _bot_EventMD()
        {
            // Значение полей Вытягиваем из бота и передаем в window
 
            //OnPropertyChanged(nameof(BigBid));

            if (_bot.BigBid != ""
                && _bot.BigBid != null)
            {
                string strBid = _bot.BigBid;

                OnPropertyChanged(nameof(BigBid));
            }
 
            //OnPropertyChanged(nameof(BigAsk));

            if (_bot.BigAsk != ""
                && _bot.BigAsk != null)
            {
                string strAsk = _bot.BigAsk;

                OnPropertyChanged(nameof(BigAsk));
            }
        }

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

        private void StartLong(object obj)
        {
            if (UseLong == YesNo.Yes)
            {
                UseLong = YesNo.No;
            }
            else
            {
                UseLong = YesNo.Yes;
            }
        }

        private void StartShort(object obj)
        {
            if (UseShort == YesNo.Yes)
            {
                UseShort = YesNo.No;
            }
            else
            {
                UseShort = YesNo.Yes;
            }
        }

        #endregion
    }

    public enum Edit
    {
        Start,

        Stop
    }

    public enum YesNo
    {
        Yes,

        No
    }
}
