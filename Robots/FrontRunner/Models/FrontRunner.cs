using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.FrontRunner.ViewModels;
using OsEngine.Robots.FrontRunner.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OsEngine.Robots.FrontRunner.Models
{
    public class FrontRunner: BotPanel
    {
        public FrontRunner(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            UseLong = this.CreateParameter("Use Long ?", "No", new[] { "No", "Yes" });

            UseShort = this.CreateParameter("Use Short ?", "No", new[] { "No", "Yes" });

            CheckPos = this.CreateParameter("Check Position ?", "No", new[] { "No", "Yes" });

            _tab = TabsSimple[0];

            _tab.MarketDepthUpdateEvent += _tab_MarketDepthUpdateEvent;

            _tab.PositionOpeningSuccesEvent += _tab_PositionChangeEvent;

            _tab.PositionClosingSuccesEvent += _tab_PositionChangeEvent;

            _tab.PositionOpeningFailEvent += _tab_PositionOpeningFailEvent;

        }

        #region Fields =================================================================

        public decimal BigVolume = 10000m;

        public int Offset = 2;

        public int Take = 4;

        public decimal Lot = 1m;

        public StrategyParameterString UseLong;   // Работа с Long (BID)

        public StrategyParameterString UseShort;   // Работа с Long (BID)

        public StrategyParameterString CheckPos;   // Чистка позиций

        public string BidBig;

        //public Position Position = null;

        private BotTabSimple _tab;

        private int _stateBid = 0;

        private int _stateAsk = 0;

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
                    }

                    // в поиске ошибки закомментируем и заменим способ закрытия ордеров
                    //_tab.CloseAllOrderInSystem();

                    _stateAsk = 0;
                    _stateBid = 0;

                }
            }
        }

        public Edit _edit = ViewModels.Edit.Stop;

        #endregion


        #region Methods ================================================================

        private void _tab_PositionOpeningFailEvent(Position pos)
        {

            //_tab.CloseAllOrderToPosition(pos);

        }

        private void _tab_PositionChangeEvent(Position pos)
        {

            // в поиске ошибки закрытия ордеров закомментируем
            //_tab.CloseAllOrderInSystem();

            if (pos.Direction == Side.Sell)
            {
                switch (pos.State)  
                {
                    case PositionStateType.Opening:
                        _stateAsk = 1;
                        break;

                    case PositionStateType.Open:
                        _stateAsk = 2;

                        // Выставляем StopLoss
                        decimal StopActive = pos.EntryPrice - Offset * _tab.Securiti.PriceStep;
                        decimal StopOrderPrice = pos.EntryPrice - (Offset + 100) * _tab.Securiti.PriceStep;

                        _tab.CloseAtStop(pos, StopActive, StopOrderPrice, "StopLoss");

                        // Выставляем Take Profit
                        decimal takePrice = pos.EntryPrice - Take * _tab.Securiti.PriceStep;
                        _tab.CloseAtProfit(pos, takePrice, takePrice, "TakeProfit");

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
                        decimal StopActive = pos.EntryPrice - Offset * _tab.Securiti.PriceStep;
                        decimal StopOrderPrice = pos.EntryPrice - (Offset + 100) * _tab.Securiti.PriceStep;
                        _tab.CloseAtStop(pos, StopActive, StopOrderPrice, "StopLoss");

                        // Выставляем Take Profit
                        decimal takePrice = pos.EntryPrice + Take * _tab.Securiti.PriceStep;
                        _tab.CloseAtProfit(pos, takePrice, takePrice, "TakeProfit");

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

            /// <summary>
            /// Проверка ошибочных состояний, восстановление Take, Закрытие лишних позиий; !! Автовосстановление статусов _stateAsk/Bid
            /// </summary>
            if (CheckPos.ValueString == "Yes")
            {
                CheckPositions();
            }

            if (UseShort.ValueString == "Yes")
            {
                for (int i = 0; i < marketDepth.Asks.Count; i++)
                {
                    if (_stateAsk == 0)
                    {
                        /// Выставление лимитной заявки на открытие позиции SELL для BigVolume
                        if (marketDepth.Asks[i].Ask >= BigVolume)
                        {
                            decimal price = marketDepth.Asks[i].Price - Offset * _tab.Securiti.PriceStep;
                            _tab.SellAtLimit(Lot, price, "BigVolume");

                            _stateAsk = 1;

                            List<Position> positionS = _tab.PositionOpenShort;

                            break;
                        }
                        continue ;
                    }

                    List<Position> positions = _tab.PositionOpenShort;

                    foreach (Position pos in positions)
                    {
                        /// !! Стоп-ситуация исчез BigVolume - Прописываем закрытие позиции(снятие ордера на открытие позиции) 
                        if (pos.Direction == Side.Sell
                            && marketDepth.Asks[i].Price == pos.EntryPrice + Offset * _tab.Securiti.PriceStep
                            && marketDepth.Asks[i].Ask < BigVolume / 2)     // Возможно делитель 2 стоит вывести в параметр ???
                        {
                            // Начинаем закрывать заявку - исчез BigVolume
                            _tab.CloseAllOrderToPosition(pos, "order small Big");

                            if (pos.State == PositionStateType.Open)
                            {
                                // Начинаем закрывать позицию - исчез BigVolume
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "close small Big");
                            }
                        }
                        // Если обнаружили более близкий к спреду Big Volume
                        else if (pos.State == PositionStateType.Opening
                                && marketDepth.Asks[i].Ask > BigVolume
                                && marketDepth.Asks[i].Price < pos.EntryPrice + Offset * _tab.Securiti.PriceStep)
                        {
                            // в поиске ошибки закомментируем и заменим способ закрытия всех ордеров в системе
                            //_tab.CloseAllOrderInSystem();

                            // Начинаем закрывать заявку - появился новый BigVolume
                            _tab.CloseAllOrderToPosition(pos,"Order to New Big");

                            if (pos.State == PositionStateType.Open)
                            {
                                // Начинаем закрывать позицию - появился новый BigVolume
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "Close to New Big");
                            }
                            break;
                        }
                    }
                }
            }

            if (UseLong.ValueString == "Yes")
            {
                for (int i = 0; i < marketDepth.Bids.Count; i++)
                {
                    if (_stateBid == 0)
                    {
                        /// Выставление лимитной заявки на открытие позиции для BigVolume
                        if (marketDepth.Bids[i].Bid >= BigVolume)
                        {
                            BidBig = marketDepth.Bids[i].Price.ToString();

                            decimal price = marketDepth.Bids[i].Price + Offset * _tab.Securiti.PriceStep;
                            _tab.BuyAtLimit(Lot, price, "BigVolume");

                            _stateBid = 1;

                            List<Position> positionL = _tab.PositionOpenLong;


                            break;
                        }
                        continue;
                    }

                    List<Position> positions = _tab.PositionOpenLong;

                    foreach (Position pos in positions)
                    {
                        /// !!! Стоп-ситуация исчез BigVolume -прописываем закрытие позиции(снятие ордера на открытие позиции) 
                        if (pos.Direction == Side.Buy
                            && marketDepth.Bids[i].Price == pos.EntryPrice - Offset * _tab.Securiti.PriceStep
                            && marketDepth.Bids[i].Bid < BigVolume / 2)
                        {
                            // Начинаем снимать заявку - исчез BigVolume для Buy
                            _tab.CloseAllOrderToPosition(pos, "order small Big");

                            BidBig = "";

                            if (pos.State == PositionStateType.Open)
                            {
                                // Начинаем закрывать позицию - исчез BigVolume для Buy
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "close small Big");
                            }

                            // в поиске ошибки закомментируем и заменим способ закрытия ордеров
                            //_tab.CloseAllOrderInSystem();
                        }

                        // Если обнаружили более близкий к уровню торговли Big Volume
                        else if (pos.State == PositionStateType.Opening
                                && marketDepth.Bids[i].Bid > BigVolume
                                && marketDepth.Bids[i].Price > pos.EntryPrice - Offset * _tab.Securiti.PriceStep)
                        {

                            // в поиске ошибки закомментируем и заменим способ закрытия ордеров
                            //_tab.CloseAllOrderInSystem();

                            // Начинаем снимать заявку - появился новый BigVolume для Buy
                            _tab.CloseAllOrderToPosition(pos, "order to New Big");
                            BidBig = "";

                            if (pos.State == PositionStateType.Open)
                            {
                                // Начинаем закрывать позицию - появился новый BigVolume для Buy
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "close to New Big");
                            }
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

            List<Position> positions = _tab.PositionsOpenAll;

            _stateAsk = 0;
            _stateBid = 0;

            foreach (Position pos in positions)
            {
                if (pos.State == PositionStateType.Open)
                {
                    if (pos.Direction == Side.Sell)
                    {
                        switch (_stateAsk)
                        {
                            case 2:
                                _tab.CloseAtMarket(pos, pos.OpenVolume, "CheckPositions");

                                break;

                            default:
                                // Блокируем выставление ордеров на новые позиции
                                _stateAsk = 2;

                                decimal takePrice = pos.EntryPrice - Take * _tab.Securiti.PriceStep;

                                _tab.CloseAtProfit(pos, takePrice, takePrice);

                                // Уровни StopLoss
                                //decimal StopActive = pos.EntryPrice + Offset * _tab.Securiti.PriceStep;
                                //decimal StopOrderPrice = pos.EntryPrice + (Offset + 100) * _tab.Securiti.PriceStep;
                                //_tab.CloseAtStop(pos, StopActive, StopOrderPrice);

                                break;
                        }
                    }
                    else if (pos.Direction == Side.Buy)
                    {
                        switch (_stateBid)
                        {
                            case 2:
                                _tab.CloseAtMarket(pos, pos.OpenVolume, "CheckPositions");
                                break;

                            default:
                                // Блокируем выставление ордеров на новые позиции
                                _stateBid = 2;

                                decimal takePrice = pos.EntryPrice + Take * _tab.Securiti.PriceStep;

                                _tab.CloseAtProfit(pos, takePrice, takePrice);

                                // Уровни StopLoss
                                //decimal StopActive = pos.EntryPrice - Offset * _tab.Securiti.PriceStep;
                                //decimal StopOrderPrice = pos.EntryPrice - (Offset + 100) * _tab.Securiti.PriceStep;
                                //_tab.CloseAtStop(pos, StopActive, StopOrderPrice);

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

            window.ShowDialog();
        }

        #endregion
    }
}
