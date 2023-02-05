using Newtonsoft.Json;
using OsEngine.Commands;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.MyEntity;
using OsEngine.Robots;
using OsEngine.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OsEngine.ViewModels
{
    public class MyRobotVM: BaseVM
    {
        public MyRobotVM(string header, int number) 
        {
            Header = header;
            NumberTab = number;

            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;

            Load();

        }


        #region Properties =================================

        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<Level> Levels { get; set; } = new ObservableCollection<Level>();

        public string Header
        {
            get 
            {
                if (SelectedSecurity != null)
                {
                    return SelectedSecurity.Name;
                }
                else
                {
                    return _header;
                }
            }

            set
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }
        private string _header;

        public string StatusServer
        {
            get => _statusServer;
            
            set
            {
                _statusServer = value;
                OnPropertyChanged(nameof(StatusServer));
            }
        }
        private string _statusServer;

        /// <summary>
        /// Выбранная бумага
        /// </summary>
        public Security SelectedSecurity
        {
            get => _selectedSecurity;
            set 
            {
                _selectedSecurity = value;

                OnPropertyChanged(nameof(SelectedSecurity));
                OnPropertyChanged(nameof(Header));

                if (SelectedSecurity != null)
                {
                    StartSecurity(SelectedSecurity);            // Подписка на события в инструменте
                    OnSelectedSecurity?.Invoke();               // Генерируем событие OnSelectedSecurity - бумага выбрана
                }
            }
        }
        private Security _selectedSecurity = null;


        public ServerType ServerType
        {
            get
            { 
                if (Server == null) 
                {
                    return _serverType;
                }
                return Server.ServerType;
            }
            set
            {
                if (value != _serverType)
                {
                    _serverType = value;
                }
            }
        }
        public ServerType _serverType = ServerType.None;


        /// <summary>
        /// Строка номера счета
        /// </summary>
        public string StringPortfolio
        {
            get => _stringPortfolio;
            set
            {
                _stringPortfolio = value;
                OnPropertyChanged(nameof(StringPortfolio));

                _portfolio = GetPortfolio(_stringPortfolio);

                if (_portfolio != null)
                {
                    Depo = _portfolio.ValueCurrent;
                    OnPropertyChanged(nameof(Depo));
                }
            }
        }
        private string _stringPortfolio = "";

        public decimal StartPoint
        {
            get => _startPoint;
            set
            {
                _startPoint = value;
                OnPropertyChanged(nameof(StartPoint));
            }
        }
        private decimal _startPoint;


        /// <summary>
        /// Количество уровней
        /// </summary>
        public int CountLevels
        {
            get => _countLevels;
            set
            {
                _countLevels = value;
                OnPropertyChanged(nameof(CountLevels));
            }
        }
        private int _countLevels;


        /// <summary>
        /// Вид торговли (Buy/Sell/BuySell)
        /// </summary>
        public Direction Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                OnPropertyChanged(nameof(Direction));
            }
        }
        private Direction _direction = Direction.BUY;


        /// <summary>
        /// List направлений торговли
        /// </summary>
        public List<Direction> Directions { get; set; } = new List<Direction>()
        {
            Direction.BUY, Direction.SELL, Direction.BUYSELL
        };

        public decimal Lot
        {
            get => _lot;
            set
            {
                _lot = value;
                OnPropertyChanged(nameof(Lot));
            }
        }
        private decimal _lot;

        /// <summary>
        /// Тип шагов уровней
        /// </summary>
        public StepType StepType
        {
            get => _stepType;
            set
            {
                _stepType = value;
                OnPropertyChanged(nameof(StepType));
            }
        }
        private StepType _stepType;

        /// <summary>
        /// List типов шагов уровней
        /// </summary>
        public List<StepType> StepTypes { get; set; } = new List<StepType>()
        {
            StepType.PERCENT, StepType.PUNKT
        };

        /// <summary>
        /// Величина шага уровня
        /// </summary>
        public decimal StepLevel
        {
            get => _stepLevel;
            set
            {
                _stepLevel = value;
                OnPropertyChanged(nameof(StepLevel));
            }
        }
        private decimal _stepLevel;

        public decimal TakeLevel
        {
            get => _takeLevel;
            set
            {
                _takeLevel = value;
                OnPropertyChanged(nameof(TakeLevel));
            }
        }
        private decimal _takeLevel;

        /// <summary>
        /// Кол-во активных уровней
        /// </summary>
        public int MaxActiveLevel
        {
            get => _maxActiveLevel;
            set
            {
                _maxActiveLevel = value;
                OnPropertyChanged(nameof(MaxActiveLevel));
            }
        }
        private int _maxActiveLevel;

        /// <summary>
        /// Полная позиция по бумаге
        /// </summary>
        public decimal AllPositionsCount
        {
            get => _allPositionsCount;
            set
            {
                _allPositionsCount = value;
                OnPropertyChanged(nameof(AllPositionsCount));
            }
        }
        private decimal _allPositionsCount;

        /// <summary>
        /// Средняя цена
        /// </summary>
        public decimal PriceAverage
        {
            get => _priceAverage;
            set
            {
                _priceAverage = value;
                OnPropertyChanged(nameof(PriceAverage));
            }
        }
        private decimal _priceAverage;

        /// <summary>
        /// Текущая последняя цена
        /// </summary>
        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }
        private decimal _price;

        /// <summary>
        /// Var Маржа для всей бумаги
        /// </summary>
        public decimal VarMargine
        {
            get => _varMargine;
            set
            {
                _varMargine = value;
                OnPropertyChanged(nameof(VarMargine));
            }
        }
        private decimal _varMargine;

        /// <summary>
        /// Накопленная прибыль для всей бумаги
        /// </summary>
        public decimal Accum
        {
            get => _accum;
            set
            {
                _accum = value;
                OnPropertyChanged(nameof(Accum));
            }
        }
        private decimal _accum;

        /// <summary>
        /// Сумма Accum + VarMargin
        /// </summary>
        public decimal Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
            }
        }
        private decimal _total;

        public decimal Depo
        {
            get => _depo;
            set
            {
                _depo = value;
                OnPropertyChanged(nameof(Depo));
            }
        }
        private decimal _depo = 0m;

        public bool IsRun
        {
            get => _isRun;
            set
            {
                _isRun = value;
                OnPropertyChanged(nameof(IsRun));

                if (IsRun)  TradeLogic();

            }
        }
        private bool _isRun;

        public IServer Server
        {
            get => _server;
            set
            {
                if (Server != null)
                {
                    UnSubScribeToServer();

                    _server = null;
                }

                _server = value;
                OnPropertyChanged(nameof(ServerType));
                
                SubScribeToServer();                                // Подписываем вкладку на события сервера

                StatusServer = _server.ServerStatus.ToString();

                OnPropertyChanged(nameof(StatusServer));
            }
        }
        private IServer _server = null;

        #endregion


        #region Fields  =====================================

        private Portfolio _portfolio;

        public int NumberTab = 0;                                         // Номер вкладки TAB

        #endregion


        #region Commands =================================

        /// <summary>
        /// Кнопка выбора бумаги
        /// </summary>
        public DelegateCommand CommandSelectSecurity
        {
            get
            {
                if (_commandSelectSecurity == null)
                {
                    _commandSelectSecurity = new DelegateCommand(SelectSecurity);
                }
                return _commandSelectSecurity;
            }
        }
        private DelegateCommand _commandSelectSecurity;

        /// <summary>
        /// Кнопка Посчитать уровни
        /// </summary>
        public DelegateCommand CommandCalculate
        {
            get
            {
                if (_commandCalculate == null)
                {
                    _commandCalculate = new DelegateCommand(Calculate);
                }
                return _commandCalculate;
            }
        }
        private DelegateCommand _commandCalculate;

        /// <summary>
        /// Кнопка Старт/Стоп робота
        /// </summary>
        public DelegateCommand CommandStartStop
        {
            get
            {
                if (_commandStartStop == null)
                {
                    _commandStartStop = new DelegateCommand(StartStop);
                }
                return _commandStartStop;
            }
        }
        private DelegateCommand _commandStartStop;

        #endregion

        #region Methods =================================

        /// <summary>
        /// Урок 3-32 1:07:44
        /// </summary>
        private void ServerMaster_ServerCreateEvent(IServer server)
        {
            if (server.ServerType == ServerType)
            {
                Server = server;
            }
        }

        /// <summary>
        /// Проставить заявки по уровням Levels и отправка Ордера на биржу
        /// </summary>
        private void TradeLogic()
        {
            if (IsRun is false
                || SelectedSecurity == null)
            {
                return;
            }


            foreach (Level level in Levels)
            {
                TradeLogicOpen(level);

                TradeLogicClose(level);
            }            
        }


        /// <summary>
        /// Урок 3-31 59:37
        /// </summary>
        private decimal GetStepLevel()
        {
            decimal stepLevel = 0;          // Шаг в валюте актива

            if (StepType == StepType.PUNKT)
            {
                stepLevel = StepLevel * SelectedSecurity.PriceStep;
            }
            else if (StepType == StepType.PERCENT)
            {
                stepLevel = StepLevel * Price / 100;

                stepLevel = Decimal.Round(stepLevel, SelectedSecurity.Decimals);
            }

            return stepLevel;
        }


        /// <summary>
        /// Урок 3-31 42:34
        /// </summary>
        private void TradeLogicOpen(Level level)
        {
            decimal stepLevel = GetStepLevel();

            decimal borderUp = Price + stepLevel * MaxActiveLevel;

            decimal borderDown = Price - stepLevel * MaxActiveLevel;

            if (level.PassVolume                                    // Проверка разрешение на выставление ордера по объему Volume
                    && level.PriceLevel != 0                                    // Проверка расчитаны ли уровни
                    && Math.Abs(level.Volume) + level.LimitVolume < Lot)        // Проверка ограничения по объему для уровня
            {
                if ((level.Side == Side.Buy && level.PriceLevel >= borderDown)
                    || (level.Side == Side.Sell && level.PriceLevel <= borderUp))
                {
                    decimal workLot = Lot - Math.Abs(level.Volume) - level.LimitVolume;

                    RobotWindowVM.Log(Header, "Level = " + level.GetStringForSave());
                    RobotWindowVM.Log(Header, "workLot = " + workLot);
                    //RobotWindowVM.Log(Header, "Level = " + );
                    //RobotWindowVM.Log(Header, "isCheckCurrency = " + isCheckCurrency);

                    level.PassVolume = false;

                    Order order = SendOrder(SelectedSecurity, level.PriceLevel, workLot, level.Side);

                    if (order != null)
                    {
                        level.OrdersForOpen.Add(order);

                        RobotWindowVM.Log(Header, "Send Limit order = " + GetStringForSave(order));
                    }
                    else
                    {
                        level.PassVolume = true;
                    }
                }
            }
        }


        /// <summary>
        /// Урок 3-31 42:34 - 52:49
        /// </summary>
        private void TradeLogicClose(Level level)
        {
            decimal stepLevel = GetStepLevel();

            if (level.PassTake                                      // Проверка разрешение на выставление ордера Take
                    && level.PriceLevel != 0                            // Проверка расчитаны ли уровни
                    && level.Volume != 0                                // Проверка ограничения по объему для уровня
                    && Math.Abs(level.Volume) != level.TakeVolume)
            {
                Side side = Side.None;

                if (level.Volume > 0)
                {
                    side = Side.Sell;
                }
                else if (level.Volume < 0)
                {
                    side = Side.Buy;
                }
                RobotWindowVM.Log(Header, "Level = " + level.GetStringForSave());

                decimal workLot = Math.Abs(level.Volume) - level.TakeVolume;

                RobotWindowVM.Log(Header, "workLot = " + workLot);
                //RobotWindowVM.Log(Header, "Level = " + );
                //RobotWindowVM.Log(Header, "isCheckCurrency = " + isCheckCurrency);

                if (workLot > 0)
                {
                    level.PassTake = false;

                    Order order = SendOrder(SelectedSecurity, level.TakePrice, workLot, side);

                    if (order != null)
                    {
                        level.OrdersForClose.Add(order);

                        RobotWindowVM.Log(Header, "Send Take order = " + GetStringForSave(order));
                    }
                    else
                    {
                        level.PassTake = true;
                    }
                }
            }
        }


        /// <summary>
        /// Расчет значений секции Position
        /// Урок 3-31 1:18:51 - 1:27:50
        /// </summary>
        private void CalculateMargin()
        {
            if (Levels.Count == 0
                || SelectedSecurity == null) return;

            decimal volume = 0;
            decimal accum = 0;
            decimal margin = 0;
            decimal averPrice = 0;

            foreach (Level level in Levels)
            {
                if (level.Volume + volume != 0)
                {
                    averPrice = (level.OpenPrice * level.Volume + volume * averPrice) / (level.Volume + volume);
                }

                level.Margin = (Price - level.OpenPrice) * level.Volume * SelectedSecurity.Lot;

                margin += level.Margin;

                volume += level.Volume;

                accum += level.Accum;
            }

            AllPositionsCount = Math.Round(volume, SelectedSecurity.DecimalsVolume);
            PriceAverage = Math.Round(averPrice, SelectedSecurity.Decimals);
            VarMargine = Math.Round(margin, SelectedSecurity.Decimals);
            Accum = Math.Round(accum, SelectedSecurity.Decimals);

            Total = Accum + VarMargine;

        }


        /// <summary>
        /// Выставление ордера
        /// </summary>
        private Order SendOrder(Security sec, decimal price, decimal volume, Side side)
        {
            if (string.IsNullOrEmpty(StringPortfolio))
            {
                MessageBox.Show("StringPortfolio == null !!!");
                return null;
            }
            Order order = new Order()
            {
                Price = price,
                Volume = volume,
                Side = side,
                PortfolioNumber = StringPortfolio,
                TypeOrder = OrderPriceType.Limit,
                NumberUser = NumberGen.GetNumberOrder(StartProgram.IsOsTrader),     // Генерируется уникальный номер
                SecurityNameCode = sec.Name,
                SecurityClassCode = sec.NameClass
            };

            Server.ExecuteOrder(order);                             // Отправка ордера в сервер(коннектор)

            return order;
        }

        
        /// <summary>
        /// Кнопка Старт/Стоп Робота
        /// </summary>
        private void StartStop(object o)
        {
            IsRun = !IsRun;
        }


        private void SubScribeToServer()
        {

            _server.NewMyTradeEvent += Server_NewMyTradeEvent;
            _server.NewOrderIncomeEvent += Server_NewOrderIncomeEvent;
            _server.NewCandleIncomeEvent += Server_NewCandleIncomeEvent;
            _server.NewTradeEvent += Server_NewTradeEvent;
            _server.SecuritiesChangeEvent += Server_SecuritiesChangeEvent;
            _server.PortfoliosChangeEvent += Server_PortfoliosChangeEvent;
            _server.ConnectStatusChangeEvent += Server_ConnectStatusChangeEvent;
        }


        /// <summary>
        /// Урок 3-32 01:22:37
        /// </summary>
        private void Server_PortfoliosChangeEvent(List<Portfolio> portfolios)
        {
            if (portfolios == null
                || portfolios.Count == 0) return;

            StringPortfolios = GetStringPorfolios(_server);     // Получаем список номеров счетов(портфели) от сервера(коннектора)
            OnPropertyChanged(nameof(StringPortfolios));

            if (StringPortfolios != null
                    && StringPortfolios.Count > 0)
            {
                if (StringPortfolio == "")
                {
                    StringPortfolio = StringPortfolios[0];
                }

                for (int i = 0; i < StringPortfolios.Count; i++)
                {
                    if (portfolios[i].Number == StringPortfolio)
                    {
                        _portfolio = portfolios[i];
                    }
                }
            }
        }

        private void UnSubScribeToServer()
        {
            _server.NewMyTradeEvent -= Server_NewMyTradeEvent;
            _server.NewOrderIncomeEvent -= Server_NewOrderIncomeEvent;
            _server.NewCandleIncomeEvent -= Server_NewCandleIncomeEvent;
            _server.NewTradeEvent -= Server_NewTradeEvent;
            _server.SecuritiesChangeEvent -= Server_SecuritiesChangeEvent;
            _server.PortfoliosChangeEvent -= Server_PortfoliosChangeEvent;
            _server.ConnectStatusChangeEvent -= Server_ConnectStatusChangeEvent;
        }


        /// <summary>
        /// Урок 3-32 1:03:27
        /// </summary>
        private void Server_SecuritiesChangeEvent(List<Security> securities)
        {
            for (int i = 0; i < securities.Count; i++)
            {
                if (securities[i].Name == Header)
                {
                    SelectedSecurity = securities[i];
                }
            }
        }

        private void Server_ConnectStatusChangeEvent(string obj)
        {
            StatusServer = obj;

        }


        private void Server_NewTradeEvent(List<Trade> trades)
        {
            if ( trades != null
                && trades.Last().SecurityNameCode == SelectedSecurity.Name)
            {
                Price = trades.Last().Price;

                CalculateMargin();
            }
        }


        private void Server_NewCandleIncomeEvent(CandleSeries series)
        {
            
        }


        /// <summary>
        /// Проверяем коллекцию Orders и добавляем потерянные
        /// Урок 3-33 0:26:36 
        /// </summary>
        public void CheckMissedOrders()
        {
            if (SelectedSecurity == null) return;

            if (RobotWindowVM.Orders == null
                || RobotWindowVM.Orders.Count == 0) return;

            foreach (var val in RobotWindowVM.Orders)
            {
                if (val.Key == SelectedSecurity.Name)
                {
                    foreach (var value in val.Value)
                    {
                        Server_NewOrderIncomeEvent(value.Value);
                    }
                }
            }
        }


        /// <summary>
        /// Проверяем коллекцию MyTrades и добавляем потерянные
        /// Урок 3-33 0:33:31
        /// </summary>
        public void CheckMissedMyTrades()
        {
            if (SelectedSecurity == null) return;

            if (RobotWindowVM.MyTrades == null
                || RobotWindowVM.MyTrades.Count == 0) return;

            foreach (var val in RobotWindowVM.MyTrades)
            {
                if (val.Key == SelectedSecurity.Name)
                {
                    foreach (var value in val.Value)
                    {
                        Server_NewMyTradeEvent(value.Value);
                    }
                }
            }
        }


        /// <summary>
        /// Проверка на новую сделку: false - если сделка уже существовала, а если сделка новая - true и производим расчеты
        /// Урок 3-31 40:48 - 41:39
        /// </summary>
        /// 
        private void Server_NewOrderIncomeEvent(Order order)
        {
            if (order == null) return;

            if (SelectedSecurity != null
                && order.SecurityNameCode == SelectedSecurity.Name
                && order.ServerType == Server.ServerType
                && order.PortfolioNumber == StringPortfolio )
            {                
                
                bool isRec = true;                                      // Записать или нет ?

                if (order.State == OrderStateType.Activ
                    && order.TimeCallBack.AddSeconds(10) < Server.ServerTime) isRec = false;    // Последующие Лог'b более не нужны

                if( isRec) RobotWindowVM.Log(Header, "NewOrderIncomeEvent = " + GetStringForSave(order));

                if (order.NumberMarket != "")
                {
                    foreach (Level level in Levels)
                    {
                        bool res = level.NewOrder(order);

                        if (res)
                        {
                            RobotWindowVM.Log(Header, "Update level  = " + level.GetStringForSave());
                        }
                    }
                }

                Save();                 // Если order записываем, то вызываем Save()
            }
        }


        /// <summary>
        /// Урок 3-31 0:00 - 
        /// </summary>
        private void Server_NewMyTradeEvent(MyTrade myTrade)
        {
            if (myTrade == null
                || SelectedSecurity == null
                || myTrade.SecurityNameCode != SelectedSecurity.Name) return;

            foreach (Level level in Levels)
            {
                bool res = level.AddMyTrade(myTrade, SelectedSecurity);

                if (true)
                {
                    RobotWindowVM.Log(Header, GetStringForSave(myTrade));

                    if (myTrade.Side == level.Side)                         // Если сделка на открытие
                    {
                        TradeLogicClose(level);
                    }
                    else
                    {
                        TradeLogicOpen(level);
                    }

                    Save();              // Как только Лог записали, вызываем Save()
                }
            }
        }


        /// <summary>
        /// Расчет уровней
        /// ......   Урок 3-31 1:08:05 - 1:12:04
        /// </summary>
        private void Calculate(object obj)
        {
            RobotWindowVM.Log(Header, "\n\n Calculate");

            ObservableCollection<Level> levels = new ObservableCollection<Level>();

            decimal stepTake = 0;

            if (CountLevels <= 0) return;

            decimal currBuyPrice = StartPoint;                 // Вспомогательная переменная "текущая цена" Buy

            decimal currSellPrice = StartPoint;                 // Вспомогательная переменная "текущая цена" Sell

            for ( int i = 0; i < CountLevels; i++ )
            {
                Level levelBuy = new Level() { Side = Side.Buy};
                Level levelSell = new Level() { Side = Side.Sell};

                if ( StepType == StepType.PUNKT)
                {
                    currBuyPrice -= StepLevel * SelectedSecurity.PriceStep;
                    currSellPrice += StepLevel * SelectedSecurity.PriceStep;

                    stepTake = TakeLevel * SelectedSecurity.PriceStep;
                    stepTake = Decimal.Round(stepTake, SelectedSecurity.Decimals);
                }
                else if (StepType == StepType.PERCENT)
                {
                    currBuyPrice -= StepLevel * currBuyPrice / 100;
                    currBuyPrice = Decimal.Round(currBuyPrice, SelectedSecurity.Decimals);

                    currSellPrice += StepLevel * currSellPrice / 100;
                    currSellPrice = Decimal.Round(currSellPrice, SelectedSecurity.Decimals);

                    stepTake = TakeLevel * currBuyPrice / 100;
                    stepTake = Decimal.Round(stepTake, SelectedSecurity.Decimals);
                }

                levelBuy.PriceLevel = currBuyPrice;
                levelSell.PriceLevel = currSellPrice;

                if (Direction == Direction.BUY
                    || Direction == Direction.BUYSELL)
                {
                    levelBuy.TakePrice = levelBuy.PriceLevel + stepTake;

                    levels.Add(levelBuy);
                }

                if (Direction == Direction.SELL
                    || Direction == Direction.BUYSELL)
                {
                    levelSell.TakePrice = levelSell.PriceLevel - stepTake;

                    levels.Insert(0, levelSell);
                }
            }

            Levels = levels;
            OnPropertyChanged(nameof(Levels));

            Save();             // После окончания расчетов вызываем Save()
        }


        /// <summary>
        /// Получение списка счетов(Porfolio)
        /// </summary>
        public ObservableCollection<string> GetStringPorfolios(IServer server)
        {
            ObservableCollection<string> stringPortfolious = new ObservableCollection<string>();

            if (server == null)
            {
                return stringPortfolious;
            }

            foreach (Portfolio portf in server.Portfolios)
            {
                stringPortfolious.Add(portf.Number);
            }

            return stringPortfolious;
        }


        /// <summary>
        /// Получение PortFolio по номеру счета
        /// </summary>
        private Portfolio GetPortfolio(String number)
        {
            if (Server != null)
            {
                foreach (Portfolio portf in Server.Portfolios)
                {
                    if (portf.Number == number)
                    {
                        return portf;
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Вызов окна для выбора бумаги
        /// </summary>
        void SelectSecurity(object o)
        {
            if (RobotWindowVM.ChangeEmitentWindow != null)
            {
                return;
            }

            RobotWindowVM.ChangeEmitentWindow = new ChangeEmitentWindow(this);

            RobotWindowVM.ChangeEmitentWindow.ShowDialog();

            RobotWindowVM.ChangeEmitentWindow = null;
        }


        /// <summary>
        /// Заказ событий по выбранному инструменту в отдельном потоке
        /// </summary>
        /// <param name="security"></param>
        private void StartSecurity(Security security)
        {
            if (security == null)
            {
                Debug.WriteLine("StartSecurity security = null");
                return;
            }

            Task.Run(() =>
            {
                while (true)
                {
                    // TimeFrameBuilder() - значения по умолчанию TimeFrame 1 Min, TradeCount = 100;
                    TimeFrameBuilder timeFrameBuilder = new TimeFrameBuilder();

                    var series = Server.StartThisSecurity(security.Name, timeFrameBuilder, security.NameClass);

                    if (series != null)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }
            });
        }

        private string GetStringForSave(Order order)
        {
            string str = "";

            str += order.SecurityNameCode + " | ";
            str += order.PortfolioNumber + " | ";
            str += order.TimeCreate + " | ";
            str += order.State + " | ";                             // Статус соединения
            str += order.Side + " | ";
            str += "Volume" + order.Volume + " | ";
            str += "Price" + order.Price + " | ";
            str += "NumberUser" + order.NumberUser + " | ";
            str += "NumberMarket" + order.NumberMarket + " | ";

            return str;
        }

        private string GetStringForSave(MyTrade myTrade)
        {
            string str = "";

            str += myTrade.SecurityNameCode + " | ";
            str += myTrade.Time + " | ";
            str += myTrade.Side + " | ";
            str += "Volume" + myTrade.Volume + " | ";
            str += "Price" + myTrade.Price + " | ";
            str += "NumberOrderParent" + myTrade.NumberOrderParent + " | ";
            str += "NumberTrade" + myTrade.NumberTrade + " | ";

            return str;
        }

        /// <summary>
        /// Сохранение имен коннектора, бумаги, параметров и уровней
        /// Урок 3-32 0:27:50
        /// </summary>
        public void Save()
        {
            if (!Directory.Exists(@"Parameters\Tabs"))                           // Folder   OsEngine\bin\Debug
            {
                Directory.CreateDirectory(@"Parameters\Tabs");
            }
  
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Parameters\Tabs\param_" + NumberTab + ".txt", false))
                {
                    writer.WriteLine(Header);
                    writer.WriteLine(ServerType);
                    writer.WriteLine(StringPortfolio);
                    writer.WriteLine(StartPoint);
                    writer.WriteLine(CountLevels);
                    writer.WriteLine(Direction);
                    writer.WriteLine(Lot);
                    writer.WriteLine(StepType);
                    writer.WriteLine(StepLevel);
                    writer.WriteLine(TakeLevel);
                    writer.WriteLine(MaxActiveLevel);
                    writer.WriteLine(PriceAverage);
                    writer.WriteLine(Accum);
                    writer.WriteLine(JsonConvert.SerializeObject(Levels));                  


                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                RobotWindowVM.Log(Header + NumberTab, "Save error = " + ex.Message);
            }
        }


        /// <summary>
        /// Считывание сохраненных имен коннектора , бумаги , параметров и уровней
        /// Урок 3-32 0:36:36
        /// </summary>
        private void Load()
        {
            if (!Directory.Exists(@"Parameters\Tabs"))                         // Folder   OsEngine\bin\Debug\Tabs
            {
                return;
            }

            string serverType = "";

            ObservableCollection<Level> levels = new ObservableCollection<Level>();

            try
            {
                using (StreamReader reader = new StreamReader(@"Parameters\Tabs\param_" + NumberTab + ".txt"))
                {
                    Header = reader.ReadLine();
                    serverType = reader.ReadLine();
                    StringPortfolio = reader.ReadLine();
                    StartPoint = GetDecimalForString(reader.ReadLine());
                    CountLevels = (int)GetDecimalForString(reader.ReadLine());

                    Direction direction = Direction.BUYSELL;                // Создаем переменную типа Enum Direction

                    if (Enum.TryParse(reader.ReadLine(), out direction))    // Парсим, является ли считаное типом Enum ?
                    {
                        Direction = direction;
                    }

                    Lot = GetDecimalForString(reader.ReadLine());

                    StepType type = StepType.PUNKT;

                    if (Enum.TryParse(reader.ReadLine(), out type))
                    {
                        StepType = type;
                    }

                    StepLevel= GetDecimalForString(reader.ReadLine());
                    TakeLevel = GetDecimalForString(reader.ReadLine());
                    MaxActiveLevel = (int)GetDecimalForString(reader.ReadLine());
                    PriceAverage = GetDecimalForString(reader.ReadLine());

                    levels = JsonConvert.DeserializeAnonymousType(reader.ReadLine(), new ObservableCollection<Level>());

                    reader.Close();
                }
            }

            catch (Exception ex)
            {

                RobotWindowVM.Log(Header + NumberTab, "Load error = " + ex.Message);
            }

            if (levels != null)
            {
                Levels = levels;
            }

            StartServer(serverType);
        }


        /// <summary>
        /// Заказ сервера(коннектора) по имени
        /// Урок 3-32 0:53:44
        /// </summary>
        private void StartServer(string serverType)
        {
            ServerType type = ServerType.None;

            if (Enum.TryParse(serverType, out type))
            {
                ServerType = type;

                ServerMaster.SetNeedServer(ServerType);
            }
        }


        /// <summary>
        /// Преобразование string в decimal
        /// Урок 3-32 0:40:13
        /// </summary>
        private decimal GetDecimalForString(string str)
        {
            decimal value = 0;

            decimal.TryParse(str, out value);               // Безопасный (без вылета на Exception) меnтод Parse

            return value;
        }


        #endregion

        #region Events =====================================

        public delegate void onSelectedSecurity();
        public event onSelectedSecurity OnSelectedSecurity;                 // Событие на создание новой вкладки Tab

        #endregion
    }
}
