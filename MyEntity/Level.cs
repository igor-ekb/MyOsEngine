using OsEngine.Entity;
using OsEngine.Robots;
using OsEngine.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.MyEntity
{
    public class Level:BaseVM
    {

        #region Properties =============================================

        /// <summary>
        /// Расчетная цена уровня
        /// </summary>
        public decimal PriceLevel
        {
            get => _priceLevel;
            set
            {
                _priceLevel = value;
                OnPropertyChanged(nameof(PriceLevel));
            }
        }
        private decimal _priceLevel = 0;

        /// <summary>
        /// Направление сделки
        /// </summary>
        public Side Side
        {
            get => _side;
            set
            {
                _side = value;
                OnPropertyChanged(nameof(Side));
            }
        }
        private Side _side = 0;

        /// <summary>
        /// Реальная цена открытой позиции
        /// </summary>
        public decimal OpenPrice
        {
            get => _openPrice;
            set
            {
                _openPrice = value;
                OnPropertyChanged(nameof(OpenPrice));
            }
        }
        private decimal _openPrice = 0;

        /// <summary>
        /// Расчетная цена для TakeProfit
        /// </summary>
        public decimal TakePrice
        {
            get => _takePrice;
            set
            {
                _takePrice = value;
                OnPropertyChanged(nameof(TakePrice));
            }
        }
        private decimal _takePrice = 0;

        /// <summary>
        /// Объем открытой позиции
        /// </summary>
        public decimal Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                Change();
            }
        }
        private decimal _volume = 0;

        public decimal Margin
        {
            get => _margin;
            set
            {
                _margin = value;
                OnPropertyChanged(nameof(Margin));
            }
        }
        private decimal _margin = 0;

        public decimal Accum
        {
            get => _accum;
            set
            {
                _accum = value;
                OnPropertyChanged(nameof(Accum));
            }
        }
        private decimal _accum = 0;

        /// <summary>
        /// Объем лимитки на покупку
        /// </summary>
        public decimal LimitVolume
        {
            get => _limitVolume;
            set
            {
                _limitVolume = value;
                Change();
            }
        }
        private decimal _limitVolume;

        /// <summary>
        /// Объем Take лимитки
        /// </summary>
        public decimal TakeVolume
        {
            get => _takeVolume;
            set
            {
                _takeVolume = value;
                Change();
            }
        }
        private decimal _takeVolume;

        /// <summary>
        /// Флаг Разрешения на выставление Объема ордера
        /// </summary>
        public bool PassVolume
        {
            get => _passVolume;
            set
            {
                _passVolume = value;
                Change();
            }
        }
        private bool _passVolume = true;

        /// <summary>
        /// Флаг Разрешения на выставление Take ордера
        ///  </summary>
        public bool PassTake
        {
            get => _passTake;
            set
            {
                _passTake = value;
                Change();
            }
        }
        private bool _passTake = true;


        #endregion

        #region Fields =========================================

        private CultureInfo CultureInfo = new CultureInfo("ru-RU");

        /// <summary>
        /// Лимитка на Take
        /// </summary>
        public List<Order> OrdersForClose = new List<Order>();

        /// <summary>
        /// Список ордеров для Открытия
        /// </summary>
        public List<Order> OrdersForOpen = new List<Order>();

        private List<MyTrade> _myTrades = new List<MyTrade>();

        private decimal _calcVolume = 0;

        #endregion

        #region Methods ========================================

        /// <summary>
        /// Поиск какой из Order обновился в списках OrdersForOpen/OrdersForClose и обновлением его параметров
        /// </summary>
        public bool NewOrder(Order newOrder)
        {
            for (int i = 0; i < OrdersForOpen.Count; i++)
            {
                if (OrdersForOpen[i].NumberMarket == newOrder.NumberMarket)
                {
                    CopyOrder(newOrder, OrdersForOpen[i]);

                    CalaculateOrders();

                    return true;
                }
            }

            for (int i = 0; i < OrdersForClose.Count; i++)
            {
                if (OrdersForClose[i].NumberMarket == newOrder.NumberMarket)
                {
                    CopyOrder(newOrder, OrdersForClose[i]);

                    CalaculateOrders();

                    return true;
                }
            }

            return false;                                                       // Order не соответствует ни одному из уровней
        }

        /// <summary>
        /// Проверка на новую сделку: false - если сделка уже существовала, а если сделка новая - true и производим расчеты
        /// Урок 3-31 04:22 - 32:24
        /// </summary>
        public bool AddMyTrade(MyTrade newTrade, Security security)
        {
            foreach (MyTrade trade in _myTrades)
            {
                if (trade.NumberTrade == newTrade.NumberTrade) return false;                
            }

            if (IsMyTrade(newTrade))
            {
                _myTrades.Add(newTrade);

                CalculatePosition(newTrade, security);

                CalaculateOrders();

                return true;
            }

            return false;
        }


        /// <summary>
        /// Проверка на новую сделку: false - если сделка уже существовала, а если сделка новая - true и производим расчеты
        /// Урок 3-31 12:17 - 30:37
        /// </summary>
        private void CalculatePosition(MyTrade myTrade, Security security)
        {
            string str = "myTrade = " + myTrade.Price + "\n";
            str += "Side = " + myTrade.Side + "\n";
            RobotWindowVM.Log(security.Name, str);

            decimal accum = 0;

            if (_calcVolume == 0)
            {
                OpenPrice = myTrade.Price;
            }
            else if (_calcVolume > 0)
            {
                if (myTrade.Side == Side.Buy)
                {
                    OpenPrice = (_calcVolume * OpenPrice + myTrade.Volume * myTrade.Price) /
                        (_calcVolume + myTrade.Volume);
                }
                else if (myTrade.Side == Side.Sell)
                {
                    if (myTrade.Volume <= _calcVolume)
                    {
                        accum = (myTrade.Price - OpenPrice) * myTrade.Volume;
                    }
                    else
                    {
                        accum = (myTrade.Price - OpenPrice) * _calcVolume;
                        OpenPrice = myTrade.Price;
                    }
                }
            }
            else if (_calcVolume < 0)
            {
                if (myTrade.Side == Side.Buy)
                {
                    if (myTrade.Volume <= Math.Abs(_calcVolume))
                    {
                        accum = (OpenPrice - myTrade.Price) * myTrade.Volume;
                    }
                    else if (myTrade.Volume > Math.Abs(_calcVolume))
                    {
                        accum = (OpenPrice - myTrade.Price) * Math.Abs(_calcVolume);
                        OpenPrice = myTrade.Price;
                    }
                }
                else if (myTrade.Side == Side.Sell)
                {
                    OpenPrice = (Math.Abs(_calcVolume) * OpenPrice + myTrade.Volume * myTrade.Price) /
                        (Math.Abs(_calcVolume) + myTrade.Volume);
                }
            }
            
            if (myTrade.Side == Side.Buy)
            {
                _calcVolume += myTrade.Volume;
            }
            else
            {
                _calcVolume -= myTrade.Volume;
            }

            if (_calcVolume == 0) OpenPrice = 0;

            Accum += accum * security.Lot;

            OpenPrice = Math.Round(OpenPrice, security.Decimals);
        }

        /// <summary>
        /// Проверка, принадлежит ли сделка моим ордерам
        /// Урок 3-31 09:20
        /// </summary>
        private bool IsMyTrade(MyTrade newTrade)
        {
            foreach (Order order in OrdersForOpen)
            {
                if (order.NumberMarket == newTrade.NumberOrderParent) return true;
            }

            foreach (Order order in OrdersForClose)
            {
                if (order.NumberMarket == newTrade.NumberOrderParent) return true;
            }

            return false;
        }

        /// <summary>
        /// Пересчет значений orders в списках OrdersForOpen/OrdersForClose после события об изменения order
        /// </summary>
        private void CalaculateOrders()
        {
            decimal activeVolume = 0;           // текущий активный объем ордеров на открытие
            decimal volumeExecute = 0;          // Исполненный объем ордеров
            decimal activeTake = 0;             // активный объем ордеров для закрытия по Take

            bool passLimit = true;
            bool passTake = true;

            foreach (Order order in OrdersForOpen)
            {
                volumeExecute += order.VolumeExecute;

                if (order.State == OrderStateType.Activ
                    || order.State == OrderStateType.Patrial)
                {
                    activeVolume += order.Volume - order.VolumeExecute;
                }
                else if (order.State == OrderStateType.Pending
                    || order.State == OrderStateType.None)
                {
                    passLimit = false;
                }
            }

            foreach (Order order in OrdersForClose)
            {
                volumeExecute -= order.VolumeExecute;

                if (order.State == OrderStateType.Activ
                    || order.State == OrderStateType.Patrial)
                {
                    activeTake += order.Volume - order.VolumeExecute;
                }
                else if (order.State == OrderStateType.Pending
                    || order.State == OrderStateType.None)
                {
                    passTake = false;
                }
            }

            Volume = volumeExecute;

            if (Side == Side.Sell)
            {
                Volume *= -1;
            }

            LimitVolume = activeVolume;
            TakeVolume = activeTake;

            PassVolume = passLimit;
            PassTake = passTake;
        }

        private Order CopyOrder(Order newOrder, Order order)
        {
            order.State = newOrder.State;
            order.TimeCancel = newOrder.TimeCancel;
            order.Volume = newOrder.Volume;
            order.VolumeExecute = newOrder.VolumeExecute;
            order.TimeDone = newOrder.TimeDone;
            order.TimeCallBack = newOrder.TimeCallBack;
            order.NumberUser = newOrder.NumberUser;

            return order;
        }

        private void Change()
        {
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(OpenPrice));
            OnPropertyChanged(nameof(TakePrice));
            OnPropertyChanged(nameof(TakeVolume));
            OnPropertyChanged(nameof(LimitVolume));
            OnPropertyChanged(nameof(PassVolume));
            OnPropertyChanged(nameof(PassTake));

        }

        public string GetStringForSave()
        {
            string str = "";

            str += "Volume = " + Volume.ToString(CultureInfo) + " | ";
            str += "PriceLevel = " + PriceLevel.ToString(CultureInfo) + " | ";
            str += "OpenPrice = " + OpenPrice.ToString(CultureInfo) + " | ";
            str += "Side = " + Side + " | ";
            str += "PassVolume = " + PassVolume.ToString(CultureInfo) + " | ";
            str += "PassTake = " + PassTake.ToString(CultureInfo) + " | ";
            str += "LimitVolume = " + LimitVolume.ToString(CultureInfo) + " | ";
            str += "TakeVolume = " + TakeVolume.ToString(CultureInfo) + " | ";
            str += "TakePrice = " + TakePrice.ToString(CultureInfo) + " | ";

            return str;
        }

        #endregion

        #region Delegates ======================================

        #endregion
    }
}
