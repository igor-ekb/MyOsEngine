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
    public class FrontRunner : BotPanel
    {
        public FrontRunner(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            _tab = TabsSimple[0];

            _tab.MarketDepthUpdateEvent += _tab_MarketDepthUpdateEvent;

            _tab.PositionOpeningSuccesEvent += _tab_PositionChangeEvent;

            //_tab.PositionClosingSuccesEvent += _tab_PositionChangeEvent;

            _tab.PositionOpeningFailEvent += _tab_PositionOpeningFailEvent;

        }

        #region Fields =================================================================

        /// <summary>
        /// Создаем для  робота FrontRunner событие изменение данных EventMD
        /// </summary>
        // Объявляем делегат
        public delegate void eventMD();
        // и событие
        public event eventMD EventMD;

        public decimal BigVolume = 10000m;

        public int Offset = 2;

        public int Take = 20;

        public decimal Lot = 1m;

        public string BigBid;

        public string BigAsk;

        public string Ask0;

        public string Bid0;

        public List<Position> positionsS = null;

        public List<Position> positionsL = null;

        public List<Position> positions = null;

        public BotTabSimple _tab;

        #endregion


        #region Properties =============================================================

        public Edit Edit
        {
            get => _edit;

            set
            {
                _edit = value;

                positions = _tab.PositionsOpenAll;

                if (Edit == Edit.Stop)
                {

                    foreach (Position pos in positions)
                    {
                        if (pos.State == PositionStateType.Opening)
                        {
                            _tab.CloseAllOrderToPosition(pos, "order Edit_Stoped");
                            Log("order Edit.Stop5(" + positions.Count + ")   = ", pos);
                        }
                        else if (pos.State == PositionStateType.Open)
                        {
                            _tab.CloseAtMarket(pos, pos.OpenVolume, "pos Edit_Stoped");
                            Log("pos Edit.Stop5(" + positions.Count + ")     = ", pos);
                        }
                    }
                    positionsL = _tab.PositionOpenLong;
                    positionsS = _tab.PositionOpenShort;
                    BigBid = "";
                    BigAsk = "";
  
                    // Генерируем событие EventMD
                    EventMD?.Invoke();
                }
                else if (Edit == Edit.Start)
                {
                    positionsS = _tab.PositionOpenShort;
                    foreach (Position pos in positions)
                    {
                        BigAsk = pos.SignalTypeOpen;

                        if (pos.State == PositionStateType.Open
                            && pos.ProfitOrderRedLine == 0)
                        {
                            // Выставляем Take Profit
                            decimal takePrice = pos.EntryPrice - Take * _tab.Securiti.PriceStep;
                            _tab.CloseAtProfit(pos, takePrice, takePrice,"Edit.Take " + takePrice.ToStringWithNoEndZero());

                            // Уровни StopLoss
                            //decimal StopActive = Convert.ToDecimal(BigAsk);
                            //decimal StopOrderPrice = StopActive + 100 * _tab.Securiti.PriceStep;
                            //_tab.CloseAtStop(pos, StopActive, StopOrderPrice,"StopLoss");
                        }
                    }
 
                    positionsL = _tab.PositionOpenLong;
                    foreach (Position pos in positions)
                    {
                        BigBid = pos.SignalTypeOpen;

                        if (pos.State == PositionStateType.Open
                            && pos.ProfitOrderRedLine == 0)
                        {
                            // Выставляем Take Profit
                            decimal takePrice = pos.EntryPrice + Take * _tab.Securiti.PriceStep;
                            _tab.CloseAtProfit(pos, takePrice, takePrice, "Edit.Take " + takePrice.ToStringWithNoEndZero());

                            // Уровни StopLoss
                            //decimal StopActive = Convert.ToDecimal(BigBid);
                            //decimal StopOrderPrice = StopActive - 100 * _tab.Securiti.PriceStep;
                            //_tab.CloseAtStop(pos, StopActive, StopOrderPrice,"StopLoss");
                        }
                    }
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
                positionsS = _tab.PositionOpenShort;

                if (UseShort == YesNo.No)
                {
                    BigAsk = "";

                    foreach (Position pos in positionsS)
                    {
                        if (pos.State == PositionStateType.Opening)
                        {
                            _tab.CloseAllOrderToPosition(pos, "order UseShort Stoped");
                            Log("order close6(" + positions.Count + ")       = ", pos);
                        }
                        else if (pos.State == PositionStateType.Open)
                        {
                            _tab.CloseAtMarket(pos, pos.OpenVolume, "pos UseShort Stoped");
                            Log("pos close6(" + positions.Count + ")         = ", pos);
                        }
                    }
                }
                positions = _tab.PositionsOpenAll;
                positionsS = _tab.PositionOpenShort;

            }
        }
        public YesNo _useShort = ViewModels.YesNo.Yes;

        public YesNo UseLong
        {
            get => _useLong;
            set
            {
                _useLong = value;
                positionsL = _tab.PositionOpenLong;

                if (UseLong == YesNo.No)
                {
                    BigBid = "";

                    foreach (Position pos in positionsL)
                    {
                        if (pos.State == PositionStateType.Opening)
                        {
                            _tab.CloseAllOrderToPosition(pos, "order UseLong Stoped");
                            Log("order close6(" + positions.Count + ")       = ", pos);
                        }
                        else if (pos.State == PositionStateType.Open)
                        {
                            _tab.CloseAtMarket(pos, pos.OpenVolume, "pos UseLong Stoped");
                            Log("pos close6(" + positions.Count + ")         = ", pos);
                        }
                    }
                }
                positions = _tab.PositionsOpenAll;
                positionsL = _tab.PositionOpenLong;
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
            if (pos.State != PositionStateType.Open)
            {
                MessageBox.Show(pos.Number.ToString() + "Статус :" + pos.State.ToString());

                return;
            }

            if (pos.Direction == Side.Sell)
            {
                // Выставляем Take Profit
                decimal takePrice = pos.EntryPrice - Take * _tab.Securiti.PriceStep;
                _tab.CloseAtProfit(pos, takePrice, takePrice, "Take " + takePrice.ToStringWithNoEndZero());
 
                Log("posShort Take4        = ", pos);
            }

            else if (pos.Direction == Side.Buy)
            {
                // Выставляем Take Profit
                decimal takePrice = pos.EntryPrice + Take * _tab.Securiti.PriceStep;
                _tab.CloseAtProfit(pos, takePrice, takePrice, "Take " + takePrice.ToStringWithNoEndZero());

                Log("posLong Take4         = ", pos);
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

            positionsS = _tab.PositionOpenShort;
            positionsL = _tab.PositionOpenLong;

            // Генерируем событие EventMD
            EventMD?.Invoke();
            
            if (UseShort == YesNo.Yes)
            {
                for (int i = 0; i < marketDepth.Asks.Count; i++)
                {
                    Ask0 = marketDepth.Asks[0].Price.ToStringWithNoEndZero();

                    if (_tab.PositionOpenShort.Count == 0)
                    {
                        /// Выставление лимитной заявки на открытие позиции SELL для BigVolume
                        if (marketDepth.Asks[i].Ask >= BigVolume)
                        {
                            decimal price = marketDepth.Asks[i].Price - Offset * _tab.Securiti.PriceStep;

                            BigAsk = marketDepth.Asks[i].Price.ToStringWithNoEndZero();

                            var pos = _tab.SellAtLimit(Lot, price, BigAsk);

                            Log("order Short1          = ",pos);

                            break;
                        }
                        continue ;
                    }

                    positionsS = _tab.PositionOpenShort;

                    foreach (Position pos in positionsS)
                    {
                        BigAsk = pos.SignalTypeOpen;

                        // Проверяем ошибку !!! функциональности Take
                        if (pos.ProfitOrderRedLine > marketDepth.Bids[0].Price
                            && pos.State == PositionStateType.Open)
                        {

                            MessageBox.Show("!!! Не работает Take(Short) = " + pos.ProfitOrderRedLine.ToStringWithNoEndZero()
                                + "> Trade = " + marketDepth.Bids[0].Price.ToStringWithNoEndZero());

                            // Переносим Take
                            decimal takePrice = marketDepth.Asks[0].Price;
                            _tab.CloseAtProfit(pos, takePrice, takePrice, "Take " + takePrice.ToStringWithNoEndZero());
                        }

                        // Проверяем убыточность позиции
                        if (Convert.ToDecimal(BigAsk) <= marketDepth.Asks[0].Price
                            && pos.State == PositionStateType.Open
                            && marketDepth.Asks[0].Ask < BigVolume/2)
                        {
                            // Закрываем позицию по рынку
                            _tab.CloseAtMarket(pos, pos.OpenVolume, "StopLoss " + marketDepth.Asks[0].Price.ToStringWithNoEndZero());
                            Log("posShort close0(" + positions.Count + ")    = ", pos);

                            continue;
                        }

                        // Проверяем появление более близкого к трейду объема Big Volume
                        if (marketDepth.Asks[i].Price < Convert.ToDecimal(BigAsk)
                            && marketDepth.Asks[i].Ask > BigVolume )
                        {
                            if (pos.State == PositionStateType.Opening)
                            {
                                // Закрываем заявку на открытие для переоткрытия на новом BigAsk
                                _tab.CloseAllOrderToPosition(pos, "Order to New " + marketDepth.Asks[i].Price.ToStringWithNoEndZero());
                                Log("orderShort close2(" + positions.Count + ")  = ", pos);

                                BigAsk = "";
                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Переносим BigVolume на новый BigAsk
                                BigAsk = marketDepth.Asks[i].Price.ToStringWithNoEndZero();
                                pos.SignalTypeOpen = BigAsk;
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "Close to New " + marketDepth.Asks[i].Price.ToStringWithNoEndZero());

                                // Переносим Take
                                decimal takePrice = marketDepth.Asks[i].Price - Take * _tab.Securiti.PriceStep;
                                _tab.CloseAtProfit(pos, takePrice, takePrice, "Take " + takePrice.ToStringWithNoEndZero());

                                Log("posShort NewBig2(" + positions.Count + ")   = ", pos);
                            }
                            // прекращаем перебор marketDepth.Asks
                            i = marketDepth.Asks.Count;

                            break;
                        }

                        // Проверяем наличие BigVolume для BigAsk
                        BigAsk = pos.SignalTypeOpen;

                        if (marketDepth.Asks[i].Price == Convert.ToDecimal(BigAsk)
                            //&& marketDepth.Asks[i].Price == pos.EntryPrice + Offset * _tab.Securiti.PriceStep
                            && marketDepth.Asks[i].Ask < BigVolume / 2)     // Возможно делитель 2 стоит вывести в параметр ???
                        {
                            /// !! Стоп-ситуация исчез BigVolume
                            if (pos.State == PositionStateType.Opening)
                            {
                                // закрываем заявки на открытие позиции - исчез BigVolume
                                _tab.CloseAllOrderToPosition(pos, "order small " + marketDepth.Asks[i].Ask.ToStringWithNoEndZero());
                                Log("orderShort close3(" + positions.Count + ")  = ", pos);

                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Закрываем позицию по рынку - исчез BigVolume
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "close small " + marketDepth.Asks[i].Ask.ToStringWithNoEndZero());
                                //Log("posShort close3(" + positions.Count + ")    = ", pos);
                            }
                            BigAsk = "";
 
                            // прекращаем перебор marketDepth.Asks
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
                    Bid0 = marketDepth.Bids[0].Price.ToStringWithNoEndZero();

                    if (_tab.PositionOpenLong.Count == 0 )
                    {
                        /// Выставление лимитной заявки на открытие позиции для BigVolume
                        if (marketDepth.Bids[i].Bid >= BigVolume)
                        {
                            BigBid = marketDepth.Bids[i].Price.ToStringWithNoEndZero();

                            decimal price = marketDepth.Bids[i].Price + Offset * _tab.Securiti.PriceStep;

                            var pos  = _tab.BuyAtLimit(Lot, price, BigBid);
                            Log("order Long1           = ", pos);

                            break;
                        }
                        continue;
                    }

                    positionsL = _tab.PositionOpenLong;

                    foreach (Position pos in positionsL)
                    {
                        BigBid = pos.SignalTypeOpen;

                        // Проверяем функциональность Take
                        if (pos.ProfitOrderRedLine < marketDepth.Asks[0].Price
                            && pos.State == PositionStateType.Open)
                        {
                            MessageBox.Show("!!! Не работает Take(Long) = " + pos.ProfitOrderRedLine.ToStringWithNoEndZero()
                                + "< Trade= " + marketDepth.Asks[0].Price.ToStringWithNoEndZero());

                            // Переносим Take
                            decimal takePrice = marketDepth.Bids[0].Price;
                            _tab.CloseAtProfit(pos, takePrice, takePrice, "Take " + takePrice.ToStringWithNoEndZero());
                        }

                        // Проверяем убыточность позиции
                        if (Convert.ToDecimal(BigBid) >= marketDepth.Bids[0].Price
                            && pos.State == PositionStateType.Open
                            && marketDepth.Bids[0].Bid < BigVolume / 2)
                        {
                            // Закрываем позицию по рынку
                            _tab.CloseAtMarket(pos, pos.OpenVolume, "StopLoss " + marketDepth.Asks[0].Price.ToStringWithNoEndZero());
                            Log("posLong close0(" + positions.Count + ")     = ", pos);

                            continue;
                        }

                        // Проверяем появление более близкого к трейду объема Big Volume
                        if (marketDepth.Bids[i].Bid > BigVolume
                            && marketDepth.Bids[i].Price > Convert.ToDecimal(pos.SignalTypeOpen))
                        {
                            BigBid = marketDepth.Bids[i].Price.ToStringWithNoEndZero();

                            if (pos.State == PositionStateType.Opening)
                            {
                                // снимаем заявку - появился новый BigVolume для Buy
                                _tab.CloseAllOrderToPosition(pos, "order to New " + BigBid);
                                Log("orderLong close2(" + positions.Count + ")   = ", pos);

                                BigBid = "";
                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Переставляем Big Volume для Buy на новый уровень

                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "close to New " + marketDepth.Bids[i].Price.ToStringWithNoEndZero());
                                BigBid = marketDepth.Bids[i].Price.ToStringWithNoEndZero();
                                pos.SignalTypeOpen = BigBid;

                                // Переносим Take Profit
                                decimal takePrice = marketDepth.Bids[i].Price + Take * _tab.Securiti.PriceStep;
                                _tab.CloseAtProfit(pos, takePrice, takePrice, "Take " + takePrice.ToStringWithNoEndZero());
                                
                                Log("posLong newBig2(" + positions.Count + ")      = ", pos);
                            }

                            // прекращаем перебор marketDepth.Bids
                            i = marketDepth.Bids.Count;

                            break;
                        }

                        // Проверяем наличие BigVolume -инициируем снятие ордера на открытие позиции / закрытие позиции
                        if (marketDepth.Bids[i].Price == Convert.ToDecimal(BigBid)
                             && marketDepth.Bids[i].Bid < BigVolume / 2)
                        {
                            if (pos.State == PositionStateType.Opening)
                            {
                                //  Снимаем ордера на покупку - исчез BigVolume для Buy
                                _tab.CloseAllOrderToPosition(pos, "order small " + marketDepth.Bids[i].Bid.ToStringWithNoEndZero());
                                Log("orderLong close3(" + positions.Count + ")   = ", pos);
                            }
                            else if (pos.State == PositionStateType.Open)
                            {
                                // Инициируем состояние закрытие позиции по рынку - исчез BigVolume для Buy
                                //_tab.CloseAtMarket(pos, pos.OpenVolume, "close small " + marketDepth.Bids[i].Bid.ToStringWithNoEndZero());
                                //Log("posLong close3(" + positions.Count + ")     = ", pos);
                            }
                            BigBid = "";

                            // прекращаем перебор marketDepth.Bids
                            i = marketDepth.Bids.Count;

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
