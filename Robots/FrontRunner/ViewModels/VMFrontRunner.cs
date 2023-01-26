using OsEngine.Commands;
using OsEngine.Entity;
using OsEngine.Robots.FrontRunner.Models;
using OsEngine.Robots.MyRobots.ViewModels;
using ru.micexrts.cgate.message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OsEngine.Robots.FrontRunner.ViewModels
{
    public class VMFrontRunner : BaseVM
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
            get => _bot.Offset;                         // Считываем начальное значение
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

        public string BigAsk { get => _bot.BigAsk; }
        public string Ask0 { get => _bot.Ask0; }
        public string AskNum { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.positionsS[0].Number.ToString(); } }
        public string AskState { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.positionsS[0].State.ToString(); } }
        public string AskEntry { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.positionsS[0].EntryPrice.ToStringWithNoEndZero(); } }
        public string AskVolume { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.positionsS[0].OpenVolume.ToStringWithNoEndZero(); } }

        public string AskNow { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.Ask0; } }
        public string AskProfit { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.positionsS[0].ProfitPortfolioPunkt.ToStringWithNoEndZero(); } }
        public string AskTake { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.positionsS[0].ProfitOrderPrice.ToStringWithNoEndZero(); } }
        public string AskStop { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsS.Count == 0) return "";
                else return _bot.positionsS[0].StopOrderRedLine.ToStringWithNoEndZero(); } }
 
        public string BigBid { get => _bot.BigBid; }
        public string Bid0 { get => _bot.Bid0; }
        public string BidNum { get { if (_bot.positionsL == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.positionsL[0].Number.ToString(); } }
        public string BidState { get { if (_bot.positionsL == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.positionsL[0].State.ToString(); } }
        public string BidEntry { get { if (_bot.positionsL == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.positionsL[0].EntryPrice.ToStringWithNoEndZero(); } }
        public string BidVolume { get { if (_bot.positionsL == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.positionsL[0].OpenVolume.ToString(); } }

        public string BidNow { get { if (_bot.positionsS == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.Bid0; } }
        public string BidProfit { get { if (_bot.positionsL == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.positionsL[0].ProfitPortfolioPunkt.ToStringWithNoEndZero(); } }
        public string BidTake { get { if (_bot.positionsL == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.positionsL[0].ProfitOrderPrice.ToStringWithNoEndZero(); } }
        public string BidStop { get { if (_bot.positionsL == null) return "";
                else if (_bot.positionsL.Count == 0) return "";
                else return _bot.positionsL[0].StopOrderRedLine.ToStringWithNoEndZero(); } }


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
            // Значение полей передаем в window

            //if (BigAsk != "" && BigAsk != null) OnPropertyChanged(nameof(BigAsk));
            //if (Ask0 != "" && Ask0 != null) OnPropertyChanged(nameof(Ask0));
            //if (AskNum != "" && AskNum != null) OnPropertyChanged(nameof(AskNum));
            //if (AskState != "" && AskState != null) OnPropertyChanged(nameof(AskState));
            //if (AskEntry != "" && AskEntry != null) OnPropertyChanged(nameof(AskEntry));
            //if (AskVolume != "" && AskVolume != null) OnPropertyChanged(nameof(AskVolume));
            //if (AskProfit != "" && AskProfit != null) OnPropertyChanged(nameof(AskProfit));
            //if (AskTake != "" && AskTake != null) OnPropertyChanged(nameof(AskTake));
            //if (AskStop != "" && AskStop != null) OnPropertyChanged(nameof(AskStop));

            OnPropertyChanged(nameof(BigAsk));
            OnPropertyChanged(nameof(Ask0));
            OnPropertyChanged(nameof(AskNum));
            OnPropertyChanged(nameof(AskState));
            OnPropertyChanged(nameof(AskEntry));
            OnPropertyChanged(nameof(AskVolume));
            OnPropertyChanged(nameof(AskNow));
            OnPropertyChanged(nameof(AskProfit));
            OnPropertyChanged(nameof(AskTake));
            OnPropertyChanged(nameof(AskStop));

            //if (BigBid != "" && BigBid != null) OnPropertyChanged(nameof(BigBid));
            //if (Bid0 != "" && Bid0 != null) OnPropertyChanged(nameof(Bid0));
            //if (BidNum != "" && BidNum != null) OnPropertyChanged(nameof(BidNum));
            //if (BidState != "" && BidState != null) OnPropertyChanged(nameof(BidState));
            //if (BidEntry != "" && BidEntry != null) OnPropertyChanged(nameof(BidEntry));
            //if (BidVolume != "" && BidVolume != null) OnPropertyChanged(nameof(BidVolume));
            //if (BidProfit != "" && BidProfit != null) OnPropertyChanged(nameof(BidProfit));
            //if (BidTake != "" && BidTake != null) OnPropertyChanged(nameof(BidTake));
            //if (BidStop != "" && BidStop != null) OnPropertyChanged(nameof(BidStop));

            OnPropertyChanged(nameof(BigBid));
            OnPropertyChanged(nameof(Bid0));
            OnPropertyChanged(nameof(BidNum));
            OnPropertyChanged(nameof(BidState));
            OnPropertyChanged(nameof(BidEntry));
            OnPropertyChanged(nameof(BidVolume));
            OnPropertyChanged(nameof(BidNow));
            OnPropertyChanged(nameof(BidProfit));
            OnPropertyChanged(nameof(BidTake));
            OnPropertyChanged(nameof(BidStop));
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
