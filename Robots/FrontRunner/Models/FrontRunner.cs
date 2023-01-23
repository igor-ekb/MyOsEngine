using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.FrontRunner.ViewModels;
using OsEngine.Robots.FrontRunner.Views;
using QuikSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OsEngine.Robots.FrontRunner.Models
{
    public class FrontRunner: BotPanel
    {
        public FrontRunner(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            //UseLong = this.CreateParameter("Use Long ?", "No", new[] { "No", "Yes" });

            //UseShort = this.CreateParameter("Use Short ?", "No", new[] { "No", "Yes" });

            //CheckPos = this.CreateParameter("Check Position ?", "No", new[] { "No", "Yes" });

            _tab = TabsSimple[0];

            _tab.MarketDepthUpdateEvent += _tab_MarketDepthUpdateEvent;

            _tab.PositionOpeningSuccesEvent += _tab_PositionChangeEvent;

            //_tab.PositionClosingSuccesEvent += _tab_PositionChangeEvent;

            _tab.PositionOpeningFailEvent += _tab_PositionOpeningFailEvent;

        }

        #region Fields =================================================================

        /// <summary>
        /// Создаем для  робота FrontRunner собственное событие
        /// </summary>
        // Объявляем делегат
        public delegate void eventMD();
        // и событие
        public event eventMD EventMD;

        
        public decimal BigVolume = 10000m;

        public int Offset = 2;

        public int Take = 6;

        public decimal Lot = 1m;

        //public StrategyParameterString UseLong;     // Работа с Long (BID)
        //public StrategyParameterString UseShort;     // Работа с Short (BID)
        //public StrategyParameterString CheckPos;   // Выравнивание статусов и кол-ва позиций ?

        //public Position Position = null;

        public string BigBid;

        public string BigAsk;

        private BotTabSimple _tab;

        private int _stateBid = 0;                  // Статус позиций Long

        private int _stateAsk = 0;                  // Статус позиций Short

        #endregion


        #region Properties =============================================================

        public Edit Edit
        {
            get => _edit;

            set
            {
                _edit = value;

                List<Position> positions = _tab.PositionsOpenAll;

                if (Edit == Edit.Stop)
                {
                    foreach (Position pos in positions)
                    {
                        if (pos.State == PositionStateType.Opening)
                        {
                            _tab.CloseAllOrderToPosition(pos, "Edit_Stoped");
                        }
                        else if (pos.State == PositionStateType.Open)
                        {
                            _tab.CloseAtMarket(pos, pos.OpenVolume, "Edit_Stoped");
                        }
                    }
                    _stateAsk = 0;
                    _stateBid = 0;
                    BigBid = "";
                    BigAsk = "";
                }
                else if (Edit == Edit.Start)
                {
                    CheckPositions();
                }
            }
        }
        public Edit _edit = ViewModels.Edit.Stop;


        public YesNo UseShort
        {
            get => _useShort;
            set
            {
                _useShort = value;
            }
        }
        public YesNo _useShort = ViewModels.YesNo.Yes;

        public YesNo UseLong
        {
            get => _useLong;
            set
            {
                _useLong = value;
            }
        }
        public YesNo _useLong = ViewModels.YesNo.Yes;


        #endregion


        #region Methods ================================================================

        private void _tab_PositionOpeningFailEvent(Position pos)
        {

            //_tab.CloseAllOrderToPosition(pos);

        }

        private void _tab_PositionChangeEvent(Position pos)
        {
            if (pos.Direction == Side.Sell)
            {
                Log("posChanged 4 = ", pos);
                switch (pos.State)  
                {
                    case PositionStateType.Opening:
                        _stateAsk = 1;
                        break;

                    case PositionStateType.Open:
                        _stateAsk = 2;

                        // Выставляем StopLoss
                        //decimal StopActive = pos.EntryPrice - Offset * _tab.Securiti.PriceStep;
                        //decimal StopOrderPrice = pos.EntryPrice - (Offset + 100) * _tab.Securiti.PriceStep;
                        //_tab.CloseAtStop(pos, StopActive, StopOrderPrice, "StopLoss");

                        // Выставляем Take Profit
                        decimal takePrice = pos.EntryPrice - Take * _tab.Securiti.PriceStep;
                        _tab.CloseAtProfit(pos, takePrice, takePrice, "TakeProfit");
                        break;

                    case PositionStateType.OpeningFail:
                        _stateAsk = 0;
                        break;

                    case PositionStateType.Done:
                        _stateAsk = 0;
                        break;

                    default:
                        _stateAsk = 0;
                        break;
                }
            }

            else if (pos.Direction == Side.Buy)
            {
                switch (pos.State)
                {
                    case PositionStateType.Opening:
                        _stateBid = 1;
                        break;
 
                    case PositionStateType.Open:
                        _stateBid = 2;

                        // Выставляем StopLoss
                        //decimal StopActive = pos.EntryPrice - Offset * _tab.Securiti.PriceStep;
                        //decimal StopOrderPrice = pos.EntryPrice - (Offset + 100) * _tab.Securiti.PriceStep;
                        //_tab.CloseAtStop(pos, StopActive, StopOrderPrice, "StopLoss");

                        // Выставляем Take Profit
                        decimal takePrice = pos.EntryPrice + Take * _tab.Securiti.PriceStep;
                        _tab.CloseAtProfit(pos, takePrice, takePrice, "TakeProfit");

                        break;

                    case PositionStateType.OpeningFail:
                        _stateBid = 0;
                        break;

                    case PositionStateType.Done:
                        _stateBid = 0;
                        break;

                    default:
                        _stateBid = 0;
                        break;
                }
            }
        }

        private void _tab_MarketDepthUpdateEvent(MarketDepth marketDepth)
        {
            if (Edit == Edit.Stop)
            {
                return;
            }
            
            if (marketDepth.SecurityNameCode != _tab.Securiti.Name)
            {
                return;
            }

            // Генерируем событие EventMD
            EventMD?.Invoke();
            
            /// <summary>
            /// Проверка ошибочных состояний, восстановление Take, Закрытие лишних позиий; !! Автовосстановление статусов _stateAsk/Bid
            /// </summary>
            // if (CheckPos == YesNo.Yes)
            //{
            //CheckPositions();
            //}

            if (UseShort == YesNo.Yes)
            {
                for (int i = 0; i < marketDepth.Asks.Count; i++)
                {
                    if (_tab.PositionOpenShort.Count == 0)
                        //&& _stateAsk == 0 )
                    {
                        /// Выставление лимитной заявки на открытие позиции SELL для BigVolume
                        if (marketDepth.Asks[i].Ask >= BigVolume)
                        {
                            decimal price = marketDepth.Asks[i].Price - Offset * _tab.Securiti.PriceStep;
                            var pos = _tab.SellAtLimit(Lot, price, "BigVolume " + marketDepth.Asks[i].Price.ToStringWithNoEndZero());

                            BigAsk = marketDepth.Asks[i].Price.ToStringWithNoEndZero();

                            Log("order Short1          = ",pos);

                            List<Position> positionS = _tab.PositionOpenShort;

                            _stateAsk = 1;

                            break;
                        }
                        continue ;
                    }

                    List<Position> positions = _tab.PositionOpenShort;

                    foreach (Position pos in positions)
                    {
                        // Проверяем наличие Big Volume
                        if ( marketDepth.Asks[i].Price == pos.EntryPrice + Offset * _tab.Securiti.PriceStep
                            && marketDepth.Asks[i].Ask < BigVolume / 2)     // Возможно делитель 2 стоит вывести в параметр ???
                        {
                            /// !! Стоп-ситуация исчез BigVolume
                            if (pos.State == PositionStateType.Opening)
                            {
                                // закрые заявки на открытие позиции - исчез BigVolume
                                _tab.CloseAllOrderToPosition(pos, "order small " + marketDepth.Asks[i].Ask.ToStringWithNoEndZero());

                                Log("orderShort close2(" + positions.Count + ") = ", pos);

                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Закрывать позицию по рынку - исчез BigVolume
                                _tab.CloseAtMarket(pos, pos.OpenVolume, "close small " + marketDepth.Asks[i].Ask.ToStringWithNoEndZero());

                                Log("posShort close2(" + positions.Count + ") = ", pos);
                            }
                            BigAsk = "";
                            _stateAsk = 0;

                            // прtкращаем перебор marketDepth.Asks
                            i = marketDepth.Asks.Count;

                            break;
                        }
                        // Проверяем наличие более близкого к текущему уровню Big Volume
                        if ( marketDepth.Asks[i].Ask > BigVolume
                            && marketDepth.Asks[i].Price < pos.EntryPrice + Offset * _tab.Securiti.PriceStep)
                        {
                            if ( pos.State == PositionStateType.Opening )
                            {
                                // Инициализируем закрытие ордера(на открытие Short) - появился новый BigVolume
                                _tab.CloseAllOrderToPosition(pos, "Order to New " + marketDepth.Asks[i].Price.ToStringWithNoEndZero());
                                Log("orderShort close3(" + positions.Count + ")  = ", pos);
                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Инициализируем закрытие позиции - появился новый BigVolume
                                _tab.CloseAtMarket(pos, pos.OpenVolume, "Close to New " + marketDepth.Asks[i].Price.ToStringWithNoEndZero());
                                Log("posShort close 3(" + positions.Count + ")   = ", pos);
                            }
                            BigAsk = "";
                            _stateAsk = 0;

                            // прtкращаем перебор marketDepth.Asks
                            i = marketDepth.Asks.Count;

                            break;
                        }
                    }
                }
            }

            if (UseLong == YesNo.Yes)
            {
                for (int i = 0; i < marketDepth.Bids.Count; i++)
                {
                    if (_tab.PositionOpenLong.Count == 0 )
                        //_stateBid == 0 )
                    {
                        /// Выставление лимитной заявки на открытие позиции для BigVolume
                        if (marketDepth.Bids[i].Bid >= BigVolume)
                        {
                            decimal price = marketDepth.Bids[i].Price + Offset * _tab.Securiti.PriceStep;
                            var pos  = _tab.BuyAtLimit(Lot, price, "BigVolume " + marketDepth.Bids[i].Price.ToStringWithNoEndZero());

                            // Log("Long 1 = " + pos.GetStringForSave());

                            Log("order Long1           = ", pos);

                            BigBid = marketDepth.Bids[i].Price.ToStringWithNoEndZero();

                            List<Position> positionL = _tab.PositionOpenLong;

                            _stateBid = 1;

                            break;
                        }
                        continue;
                    }

                    List<Position> positions = _tab.PositionOpenLong;

                    foreach (Position pos in positions)
                    {
                        /// !!! Стоп-ситуация исчез BigVolume -инициируем снятие ордера на открытие позиции (+ закрытие позиции ) 
                        if ( marketDepth.Bids[i].Price == pos.EntryPrice - Offset * _tab.Securiti.PriceStep
                            && marketDepth.Bids[i].Bid < BigVolume / 2)
                        {
                            if (pos.State == PositionStateType.Opening)
                            {
                                //  Снимаем ордера на покупку - исчез BigVolume для Buy
                                _tab.CloseAllOrderToPosition(pos, "order small " + marketDepth.Bids[i].Bid.ToStringWithNoEndZero());
                                Log("orderLong close2(" + positions.Count + ")   = ", pos);
                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Инициируем состояние закрытие позиции по рынку - исчез BigVolume для Buy
                                _tab.CloseAtMarket(pos, pos.OpenVolume, "close small " + marketDepth.Bids[i].Bid.ToStringWithNoEndZero());
                                Log("posLong close2(" + positions.Count + ")     =    ", pos);
                            }
                            _stateBid = 0;
                            BigBid = "";

                            // прекращаем перебор marketDepth.Bids
                            i = marketDepth.Bids.Count;

                            break;
                        }

                        // Если обнаружили более близкий к уровню торговли Big Volume
                        if ( marketDepth.Bids[i].Bid > BigVolume
                            && marketDepth.Bids[i].Price > pos.EntryPrice - Offset * _tab.Securiti.PriceStep)
                        {
                            if ( pos.State == PositionStateType.Opening )
                            {
                                // снимаем заявку - появился новый BigVolume для Buy
                                _tab.CloseAllOrderToPosition(pos, "order to New " + marketDepth.Bids[i].Price.ToStringWithNoEndZero());
                                Log("orderLong close3(" + positions.Count + ")   = ", pos);
                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Начинаем закрывать позицию - появился новый BigVolume для Buy
                                _tab.CloseAtMarket(pos, pos.OpenVolume, "close to New " + marketDepth.Bids[i].Price.ToStringWithNoEndZero());
                                Log("posLong close3(" + positions.Count + ")      = ", pos);
                            }
                            BigBid = "";
                            _stateBid = 0;

                            // прекращаем перебор marketDepth.Bids
                            i = marketDepth.Bids.Count;

                            break;
                        }
                    }
                }
            }
        }

        private void CheckPositions()
        {
            // Защита, если была остановка/сбой робота : считываем список позиций и проверяем/выставляем TakeProfit
            // Восстанавливаем значения _stateAsk и _stateBid


            _stateAsk = 0;
            _stateBid = 0;

            List<Position> positions = _tab.PositionsOpenAll;

            foreach (Position pos in positions)
            {
                if (pos.State == PositionStateType.Open)
                {
                    if (pos.Direction == Side.Sell)
                    {
                        switch (_stateAsk)
                        {
                            case 2:
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "CheckPositions");

                                break;

                            default:
                                // Блокируем выставление ордеров на новые позиции
                                _stateAsk = 2;

                                if (pos.ProfitOrderRedLine == 0)
                                {
                                    // Выставляем Take Profit
                                    decimal takePrice = pos.EntryPrice - Take * _tab.Securiti.PriceStep;
                                    _tab.CloseAtProfit(pos, takePrice, takePrice);

                                    // Уровни StopLoss
                                    //decimal StopActive = pos.EntryPrice + Offset * _tab.Securiti.PriceStep;
                                    //decimal StopOrderPrice = pos.EntryPrice + (Offset + 100) * _tab.Securiti.PriceStep;
                                    //_tab.CloseAtStop(pos, StopActive, StopOrderPrice);
                                }
                                break;
                        }
                    }
                    else if (pos.Direction == Side.Buy)
                    {
                        switch (_stateBid)
                        {
                            case 2:
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "CheckPositions");
                                break;

                            default:
                                // Блокируем выставление ордеров на новые позиции
                                _stateBid = 2;

                                if (pos.ProfitOrderRedLine == 0)
                                {
                                    // Выставляем Take Profit
                                    decimal takePrice = pos.EntryPrice + Take * _tab.Securiti.PriceStep;
                                    _tab.CloseAtProfit(pos, takePrice, takePrice);

                                    // Уровни StopLoss
                                    //decimal StopActive = pos.EntryPrice - Offset * _tab.Securiti.PriceStep;
                                    //decimal StopOrderPrice = pos.EntryPrice - (Offset + 100) * _tab.Securiti.PriceStep;
                                    //_tab.CloseAtStop(pos, StopActive, StopOrderPrice);
                                }

                                break;
                            }
                    }
                }
                else if (pos.State == PositionStateType.Opening)
                {
                    if (pos.Direction == Side.Sell)
                    {
                        switch (_stateAsk)
                        {
                            case 0:
                                _stateAsk = 1;
                                break;

                            case 1:
                                break;

                            default:
                                _tab.CloseAllOrderToPosition(pos, "CheckPositions");
                                break;
                        }
                    }
                    else if (pos.Direction == Side.Buy)
                    {
                        switch (_stateBid)
                        {
                            case 0:
                                _stateBid = 1;
                                break;

                            case 1:
                                break;

                            default:
                                _tab.CloseAllOrderToPosition(pos, "CheckPositions");
                                break;
                        }
                    }
                }
            }
        }

        public override string GetNameStrategyType()
        {
            return nameof(FrontRunner);
        }

        public override void ShowIndividualSettingsDialog()
        {
            FrontRunnerUi window = new FrontRunnerUi(this);

            window.Show();
        }

        public void Log(string message, Position pos)
        {
            if (! Directory.Exists(@"Log"))
            {
                Directory.CreateDirectory(@"Log");
            }

            string name = "Log_FrontRunner_" + DateTime.Now.ToShortDateString() + ".txt";

            if (pos.SignalTypeOpen == null) pos.SignalTypeOpen = "";
            if (pos.SignalTypeClose == null) pos.SignalTypeClose = "";


            string posState = pos.Number.ToString() + "(" + PositionsCount + ") " 
                + pos.TimeCreate.ToShortTimeString() + "." + pos.TimeCreate.Second + ":" + pos.TimeCreate.Millisecond + " "
                + pos.TimeOpen.ToShortTimeString() + "." + pos.TimeOpen.Second + ":" + pos.TimeOpen.Millisecond + " " 
                + pos.TimeClose.ToShortTimeString() + "." + pos.TimeClose.Second + ":" + pos.TimeClose.Millisecond + " " 
                + pos.Direction.ToString() + " " + pos.State.ToString() + " Entry = " + pos.EntryPrice.ToStringWithNoEndZero() + " "
                + pos.SignalTypeOpen.ToString() + " " + pos.SignalTypeClose.ToString();

            try
            {
                using (StreamWriter writer = new StreamWriter(@"Log\" + name, true))
                {
                    writer.WriteLine(DateTime.Now.ToShortTimeString() + "." + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
                    writer.WriteLine(message + posState);
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Log File error = " + ex.Message);
            }
        }


        #endregion
    }
}
