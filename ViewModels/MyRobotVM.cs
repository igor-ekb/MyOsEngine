using Newtonsoft.Json;
using OkonkwoOandaV20.TradeLibrary.DataTypes.Pricing;
using OsEngine.Commands;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.MyEntity;
using OsEngine.Robots;
using OsEngine.Views;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
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
        /// <summary>
        /// Перегрузка конструктора добавления робота через Load() из Parameters\param.txt
        /// ....... + модификация(Init) Урок 4-37 0:28:52
        /// </summary>
        public MyRobotVM(string header, int numberTab) 
        {
            string[] str = header.Split('=');

            Header = str[0];

            Init(numberTab);

            Load(Header);

            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;
        }


        /// <summary>
        /// Перегрузка конструктора для нового робота - непонятная версия и когда вызывается
        /// Урок 4-37 0:28:52
        /// </summary>
        public MyRobotVM(int numberTab)
        {
            if (Header == null)
            {
                Header = "Tab" + numberTab;
            }

            Init(numberTab);
        }


        #region Properties =================================

        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<Level> Levels { get; set; } = new ObservableCollection<Level>();

        public PlotModel Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }
        private PlotModel _model = new PlotModel()
        {
            Background = OxyColors.Gray                         // Серый фон чарта графика свечей
                                       // !!! Однако не понятно как менять цвет букв и  фона в окне ствойств свечи по пр.кнопки
        };

        /// <summary>
        /// Урок 4-37 01:23:56
        /// </summary>
        public PlotController Controller
        {
            get => _controller;

            set
            {
                _controller = value;
                OnPropertyChanged(nameof(Controller));
            }
        }
        public PlotController _controller;

        public decimal BorderUp
        {
            get => _borderUp;

            set
            {
                _borderUp = value;
                OnPropertyChanged(nameof(BorderUp));
            }
        }
        private decimal _borderUp = 0m;


        public decimal BorderDown
        {
            get => _borderDown;

            set
            {
                _borderDown = value;
                OnPropertyChanged(nameof(BorderDown));
            }
        }
        private decimal _borderDown = 0m;


        /// <summary>
        /// Состояние подключения 
        /// </summary>
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
        /// Строка номера счета, Секция Шапка
        /// </summary>
        public string StringPortfolio
        {
            get => _stringPortfolio;
            set
            {
                _stringPortfolio = value;
                OnPropertyChanged(nameof(StringPortfolio));

                _portfolio = GetPortfolio(_stringPortfolio);
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
        /// Количество уровней, Секция Parameters
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
        /// Вид торговли (Buy/Sell/BuySell), Секция Parameters
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

        public decimal WorkLot
        {
            get => _workLot;
            set
            {
                _workLot = value;
                OnPropertyChanged(nameof(WorkLot));
            }
        }
        private decimal _workLot = 0;

        /// <summary>
        /// Тип шагов уровней, Секция Parameters
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
        private StepType _stepType = StepType.PUNKT;

        /// <summary>
        /// List типов шагов уровней
        /// </summary>
        public List<StepType> StepTypes { get; set; } = new List<StepType>()
        {
            StepType.PERCENT, StepType.PUNKT
        };

        /// <summary>
        /// Величина шага уровня Level, Секция Parameters
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
        private decimal _stepLevel = 0m;

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
        /// Кол-во активных уровней от текущей цены Price, Секция Parameters
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
        private int _maxActiveLevel = 0;

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

                BorderUp = _price + GetStepLevel() * MaxActiveLevel;
                OnPropertyChanged(nameof(BorderUp));
                BorderDown = _price - GetStepLevel() * MaxActiveLevel;
                OnPropertyChanged(nameof(BorderDown));

                if (IsCheckCurrency
                     && Price != 0)
                {
                    WorkLot = Lot / Price;
                    WorkLot = decimal.Round(WorkLot, SelectedSecurity.DecimalsVolume);

                    if (WorkLot < Lot / Price)
                    {
                        WorkLot += 1;
                    }
                }
                else
                {
                    WorkLot = decimal.Round(Lot, SelectedSecurity.DecimalsVolume);
                }
                OnPropertyChanged(nameof(WorkLot));

                if (_price != 0) 
                {
                    if (Model.Title == null)
                    {
                        SetModel();

                        if (_portfolio != null)
                        {
                            Depo = _portfolio.ValueCurrent;
                            OnPropertyChanged(nameof(Depo));
                        }
                    }
                }
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

        public bool IsCheckCurrency
        {
            get => _isCheckCurrency;
            set
            {
                _isCheckCurrency = value;
                OnPropertyChanged(nameof(IsCheckCurrency));

            }
        }
        public bool _isCheckCurrency = false;


        public List<Side> Sides { get; set; } = new List<Side>()
        {
            Side.Buy, Side.Sell
        };

        #endregion


        #region Fields  =====================================

        // Внутренняя серия свечей по Бумаге с установкой дизайна свечей
        private CandleStickSeries _candleSeries = new CandleStickSeries             // Скопировали из OxyPlot 
        {
            Color = OxyColors.Black,
            IncreasingColor = OxyColors.SkyBlue,             // Цвет растущей свечи
            DecreasingColor = OxyColors.DarkRed,             // Цвет падающей свечи
            DataFieldX = "Time",
            DataFieldHigh = "H",
            DataFieldLow = "L",
            DataFieldOpen = "O",
            DataFieldClose = "C",
            TrackerFormatString = "High: {2:0.00}\nLow: {3:0.00}\nOpen: {4:0.00}\nClose: {5:0.00}"
        };

        private Portfolio _portfolio;                                       // Внутренняя Портфель

        public int NumberTab = 0;                                     // Идентификатор Робота, Порядковый номер при создании TAB

        private DateTime _lastTimeCandle = DateTime.MinValue;               // Внутренняя Текущее время свечи


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
        /// Содается учетный номер робота и вызывается создание Чарта графика свечей
        /// Урок 4-37 0:29:00
        /// </summary>
        private void Init(int numberTab)
        {
            NumberTab = numberTab;

            // Перенесено в ChangeEmitentVM  в метод Change
            //SetModel();
        }


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
        /// Проставить заявки по уровням Levels и отправить  Ордера на биржу
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
        /// Вычисление Шага уровня Level в валюте актива
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
        /// Торговая логика на открытие позиций
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
                    decimal lot = CalcWorkLot(Lot, level.PriceLevel);

                    decimal workLot = lot - Math.Abs(level.Volume) - level.LimitVolume;   // Объем, который собираемся выставлять

                    if (workLot == 0) return;           // доп.защита -  Урок 3-34 01:11:12

                    RobotWindowVM.Log(Header, "Level = " + level.GetStringForSave());
                    RobotWindowVM.Log(Header, "workLot = " + workLot);
                    //RobotWindowVM.Log(Header, "Level = " + );
                    //RobotWindowVM.Log(Header, "isCheckCurrency = " + isCheckCurrency);

                    level.PassVolume = false;

                    Order order = SendOrder(SelectedSecurity, level.PriceLevel, workLot, level.Side);   // Заявка на бумагу

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
        /// Торговая логика на закрытие TakeProfit открытых позиций
        /// Урок 3-31 42:34 - 52:49
        /// </summary>
        private void TradeLogicClose(Level level)
        {
            decimal stepLevel = GetStepLevel();

            if (level.PassTake                                      // Проверка разрешение на выставление ордера Take
                    && level.PriceLevel != 0                            // Проверка расчитаны ли уровни
                    && level.Volume != 0                                // Проверка - есть ли купленный объем
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

                    Order order = SendOrder(SelectedSecurity, level.TakePrice, workLot, side);      // Заявка TakeProfit

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
        /// Расчет значения Лота
        /// Урок 3-34   0:47:34
        /// </summary>
        private decimal CalcWorkLot(decimal lot, decimal price)
        {
            decimal workLot = lot;

            if (IsCheckCurrency)
            {
                workLot = lot / price;
            }

            workLot = decimal.Round(workLot, SelectedSecurity.DecimalsVolume);

            return workLot;
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

            if (order.State == OrderStateType.Fail)
            {
                MessageBox.Show("Recieved : Send Order State = Fail");
            }

            return order;
        }

        
        /// <summary>
        /// Кнопка Старт/Стоп Робота
        /// .......  + Урок 3-34 0:24:48 - 0:43:02
        /// </summary>
        private void StartStop(object o)
        {
            if (Server == null
                || Server.ServerStatus == ServerConnectStatus.Disconnect) return;

            IsRun = !IsRun;

            RobotWindowVM.Log(Header, "StartStop = " + IsRun);

            if (IsRun)
            {
                foreach (Level level in Levels)         // Урок 3-34   0:57:37
                {
                    level.SetVolumeStart();         // подчистка ордеров

                    level.PassVolume = true;        // разрешение на выставление начальных ордеров
                    level.PassTake = true;          // разрешение на выставление TakeProfit
                }
            }
            else
            {                          // Если работа остановлена
                Task.Run(() =>         // Чтобы не блокировать окно на время снятия всех ордеров запускаем новый локальный Task
                {
                    while (true)
                    {
                        foreach (Level level in Levels)
                        {
                            level.CancelAllOrders(Server, Header);
                        }

                        Thread.Sleep(3000);                         // Берем паузу на 3 сек

                        bool flag = true;                           // Создаем флаг на прекращение бесконечного while (true)

                        foreach (Level level in Levels)
                        {
                            if (level.LimitVolume != 0           // проверяем, если остались ордера на открытие
                                || level.TakeVolume != 0)        // проверяем, если остались ордера на закрытие TakeProfit
                            {
                                flag = false;                   // снимаем разрешение на прекращение бесконечного while (true)
                                break;                          // возвращаемся в while
                            }
                        }
                        if (flag)
                        {
                            break;                             // выходим из бесконечного while (true)
                        }
                    }
                }); 
            }
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

        private void Server_ConnectStatusChangeEvent(string state)
        {
            StatusServer = state;

            if (state == "Connect")
            {
                GetStateOrders();                                           // Урок 3-34 0:10:14 (Для BinanceFutures
            }
            else
            {
                IsRun = false;
            }
        }


        private void Server_NewTradeEvent(List<Trade> trades)
        {
            try
            {
                if (trades != null
                && trades.Last().SecurityNameCode == SelectedSecurity.Name)
                {
                    Trade trade = trades.Last();

                    Price = trade.Price;

                    CalculateMargin();

                    if (trade.Time.Second % 5 == 0)
                    {
                        TradeLogic();          // Раз в 5 сек перепроверяем доступность уровней по Max Level - Урок 3-34 01:06:39
                    }

                    if (trade.Time.Second % 2 == 0)
                    {
                        double x = DateTimeAxis.ToDouble(trade.Time);

                        double y = (double)trade.Price;

                        double heigh = (Model.Axes[1].ActualMaximum - Model.Axes[1].ActualMinimum);    // Размер вертикальной оси

                        double offset = heigh / 100;

                        OxyColor color;


                        if (trade.Side == Side.Buy)
                        {
                            offset *= -1;
                            color = OxyColors.LightSkyBlue;
                        }
                        else
                        {
                            color = OxyColors.LightPink;
                        }


                        Model.Annotations.Clear();

                        /// Добавляем из OxyPlot Example Annotation   Урок 4-37 01:34:00
                        // Аннотация для стрелок - будем использовать для отметки сделок
                        Model.Annotations.Add(new ArrowAnnotation
                        {
                            StartPoint = new DataPoint(x, y + offset),          // Выставление начала стрелки с отступом над/под ценой
                            EndPoint = new DataPoint(x, y),                     // Выставление кончика стрелки по цене
                            Color = color,
                            Text = trade.Price.ToString(),
                            //ToolTip = "This is a tool tip for the ArrowAnnotation"
                        });

                        Model.InvalidatePlot(true);         // Обновляем график бумаги раз в 2 сек   Урок 4-37 0:47:39
                        OnPropertyChanged(nameof(Model));
                    }
                }
            }
            catch (Exception ex)
            {
                RobotWindowVM.Log(Header, "NewTradeEvent Exception = " + ex.Message);
            }
            
        }

        /// <summary>
        /// Урок 4-37 0:32:25(контекст + 0:36:06) + 41:37
        /// </summary>
        private void Server_NewCandleIncomeEvent(CandleSeries series)
        {
            try
            {
                if (series.Security.Name != SelectedSecurity.Name) return;      // отфильтровываем части свечей только по нашей бумаге

                if (_lastTimeCandle == DateTime.MinValue                    // В самом начале, когда только начали приходить свечки
                    && _candleSeries.Items.Count == 0
                    && series.CandlesAll.Count > 0)
                {
                    foreach (Candle candle in series.CandlesAll)
                    {
                        SetCandle(candle);
                    }

                    double x = _candleSeries.Items[0].X;

                    if (_candleSeries.Items.Count > 100)
                    {
                        x = _candleSeries.Items[_candleSeries.Items.Count - 100].X;     // Урок 4-37 01:16:00

                    }

                    Model.Axes[0].Minimum = x;
                    Model.Axes[0].Maximum = _candleSeries.Items.Last().X + (_candleSeries.Items.Last().X - x) / 20; // Делаем отступ справа

                    return;
                }

                Candle lastCandle = series.CandlesAll.Last();           // иначе обычнная обработка новой порции части свечи

                SetCandle(lastCandle);
            }
            catch (Exception ex)
            {
                RobotWindowVM.Log(Header, "NewCandleIncomeEvent Exception = " + ex.Message);
            }

        }

        /// <summary>
        /// В series.свечей или обновляем свечу или добавляем новую в зависимости от интервалов времени в свече
        /// Урок 4-37  01:06:27
        /// </summary>
        private void SetCandle(Candle candle)
        {
            if (candle == null) return;

            HighLowItem hlc = GetCandle(candle);

            if (_lastTimeCandle < candle.TimeStart)        // Началась новая свеча
            {
                _candleSeries.Items.Add(hlc);               // добавляем новый объект в серию свечей

                _lastTimeCandle = candle.TimeStart;         // устанавливаем текущее время на начало свежей свечи

            }
            else                                            // еще старая свеча
            {
                HighLowItem item = _candleSeries.Items.Last();  // получаем текущий объект свечи

                _candleSeries.Items.Remove(item);               // удаляем устаревшые значения свечи

                _candleSeries.Items.Add(hlc);                   // и добавляем обновленную свечу
            }
        }


        /// <summary>
        /// Урок 4-37 0:36:16
        /// </summary>
        private HighLowItem GetCandle(Candle candle)
        {
            double open = (double)candle.Open;
            double high = (double)candle.High;
            double close = (double)candle.Close;
            double low = (double)candle.Low;

            double x = DateTimeAxis.ToDouble(candle.TimeStart);

            return new HighLowItem(x, high, low, open, close);
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
                
                bool isRec = true;                                      // Флаг : Записывать или нет ?

                if (order.State == OrderStateType.Activ
                    && order.TimeCallBack.AddSeconds(10) < Server.ServerTime) isRec = false;    // Последующие Логи более не нужны

                if( isRec) RobotWindowVM.Log(Header, "NewOrderIncomeEvent = " + GetStringForSave(order));

                // Если ордеру присвоен NumberMarket
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
        /// Реакция на приход инф. о совершении сделки 
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

                    if (myTrade.Side == level.Side)                         // Если произошла сделка на открытие позиции
                    {
                        TradeLogicClose(level);
                    }
                    else                                                    // Если произошла сделка на закрытие позиции
                    {
                        TradeLogicOpen(level);
                    }

                    Save();              // Записываем рез-т сделки в Лог ( вызываем Save()
                }
            }
        }


        /// <summary>
        /// Расчет уровней Levels(затирание предыдущих данных)
        /// ......   Урок 3-31 1:08:05 - 1:12:04
        /// </summary>
        private void Calculate(object obj)
        {
            RobotWindowVM.Log(Header, "\n Calculate");

            ObservableCollection<Level> levels = new ObservableCollection<Level>();

            decimal stepTake = 0;

            if (CountLevels <= 0) return;

            decimal currBuyPrice = StartPoint;                 // Вспомогательная переменная "текущая цена" Buy

            decimal currSellPrice = StartPoint;                 // Вспомогательная переменная "текущая цена" Sell

            for ( int i = 0; i < CountLevels; i++ )
            {
                Level levelBuy = new Level() { Side = Side.Buy};
                Level levelSell = new Level() { Side = Side.Sell};

                decimal levelStep = 0;

                if ( StepType == StepType.PUNKT)
                {

                    if ( i == 0)
                    {
                        levelStep = StepLevel * SelectedSecurity.PriceStep / 2;
                    }
                    else
                    {
                        levelStep = StepLevel * SelectedSecurity.PriceStep ;
                    }

                    stepTake = TakeLevel * SelectedSecurity.PriceStep;

                }
                else if (StepType == StepType.PERCENT)
                {
                    if (i == 0)
                    {
                        levelStep = StepLevel * currBuyPrice / 100 / 2;
                    }
                    else
                    {
                        levelStep = StepLevel * currBuyPrice / 100;
                    }

                    stepTake = TakeLevel * currBuyPrice / 100;

                }

                currBuyPrice -= levelStep;
                currBuyPrice = Decimal.Round(currBuyPrice, SelectedSecurity.Decimals);

                currSellPrice += levelStep;
                currSellPrice = Decimal.Round(currSellPrice, SelectedSecurity.Decimals);

                stepTake = Decimal.Round(stepTake, SelectedSecurity.Decimals);

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

            // Сохраняем обновленные уровни и данные робота MyRobotVM.Save() в @"Parameters\Tabs\param_" + Head + ".txt"
            Save();
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

            str += "Security=" + order.SecurityNameCode + " | ";
            str += "PortfolioNumber=" + order.PortfolioNumber + " | ";
            str += order.TimeCreate + " | ";
            str += "State=" + order.State + " | ";                             // Статус соединения
            str += order.Side + " | ";
            str += "Volume=" + order.Volume + " | ";
            str += "Price=" + order.Price + " | ";
            str += "NumberUser=" + order.NumberUser + " | ";
            str += "NumberMarket=" + order.NumberMarket + " | ";

            return str;
        }

        private string GetStringForSave(MyTrade myTrade)
        {
            string str = "";

            str += "Security=" + myTrade.SecurityNameCode + " | ";
            str += myTrade.Time + " | ";
            str += myTrade.Side + " | ";
            str += "Volume=" + myTrade.Volume + " | ";
            str += "Price=" + myTrade.Price + " | ";
            str += "NumberOrderParent=" + myTrade.NumberOrderParent + " | ";
            str += "NumberTrade=" + myTrade.NumberTrade + " | ";

            return str;
        }

        /// <summary>
        /// Сохранение робота в файл Tabs\param_" + NumberTab + ".txt имен коннектора, бумаги, параметров и ордеров на открытие/закрытие
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
                //using (StreamWriter writer = new StreamWriter(@"Parameters\Tabs\param_" + NumberTab + ".txt", false))
                using (StreamWriter writer = new StreamWriter(@"Parameters\Tabs\param_" + Header + ".txt", false))
                {
                    writer.WriteLine(Header);
                    writer.WriteLine(NumberTab);
                    writer.WriteLine(ServerType);
                    writer.WriteLine(StringPortfolio);
                    writer.WriteLine(StartPoint);
                    writer.WriteLine(CountLevels);
                    writer.WriteLine(Direction);
                    writer.WriteLine(Lot);
                    writer.WriteLine(IsCheckCurrency);
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
        private void Load(string header)
        {
            if (!Directory.Exists(@"Parameters\Tabs")                             //Folder             bin\Debug\Tabs
              || !File.Exists(@"Parameters\Tabs\param_" + header + ".txt"))     //Cуществует ли файл bin\Debug\Tabs\param_.txt?
            {
                return;
            }

            string serverType = "";

            int numberTab = NumberTab;

            ObservableCollection<Level> levels = new ObservableCollection<Level>();

            try
            {
                //using (StreamReader reader = new StreamReader(@"Parameters\Tabs\param_" + NumberTab + ".txt"))
                using (StreamReader reader = new StreamReader(@"Parameters\Tabs\param_" + header + ".txt"))
                {
                    Header = reader.ReadLine();
                    numberTab = (int)GetDecimalForString(reader.ReadLine());
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

                    bool check = true;                                  // Создаем переменную типа Enum Direction

                    if (bool.TryParse(reader.ReadLine(), out check))    // Парсим, является ли считаное типом bool ?
                    {
                        IsCheckCurrency = check;
                    }

                    StepType type = StepType.PUNKT;

                    if (Enum.TryParse(reader.ReadLine(), out type))
                    {
                        StepType = type;
                    }

                    StepLevel= GetDecimalForString(reader.ReadLine());
                    TakeLevel = GetDecimalForString(reader.ReadLine());
                    MaxActiveLevel = (int)GetDecimalForString(reader.ReadLine());
                    PriceAverage = GetDecimalForString(reader.ReadLine());
                    Accum = GetDecimalForString(reader.ReadLine());

                    levels = JsonConvert.DeserializeAnonymousType(reader.ReadLine(), new ObservableCollection<Level>());

                    reader.Close();
                }
            }

            catch (Exception ex)
            {
                RobotWindowVM.Log(Header, "MyRobotVM Load param_" + NumberTab + " Error = " + ex.Message);
            }

            if (levels != null)
            {
                Levels = levels;
            }

            // Запрос сервера у ServerMaster
            StartServer(serverType);
        }


        /// <summary>
        /// Заказ запуска сервера(коннектора) по имени
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

            decimal.TryParse(str, out value);               // Безопасный (без вылета на Exception) метод Parse

            return value;
        }

        /// <summary>
        /// !!! Пока для BinanceFutures !!! Сверка состояния ордеров из нашего списка с ордерами в сервере/коннекторе
        /// Урок 3-34 0:10:14
        /// </summary>
        private void GetStateOrders()
        {
            if (Server != null)
            {
                if (Server.ServerType == ServerType.BinanceFutures)
                {
                    List<Order> orders = new List<Order>();         // формируем список ордеров для проверкм

                    foreach (Level level in Levels)
                    {
                        GetStateOrders(level.OrdersForOpen, ref orders);

                        GetStateOrders(level.OrdersForClose, ref orders);
                    }

                    AServer aServer = (AServer)Server;              // Преобразуем Iserver Server в класс AServer !!!!

                    if (orders.Count > 0)
                    {
                        // Теперь доступны ServerRealization и GetOrdersState   -  запрос состояния ордеров
                        aServer.ServerRealization.GetOrdersState(orders);
                    }
                }
            }
        }

        /// <summary>
        /// Составление (Фильтр) списка ордеров на проверка 
        /// Урок 3-34 0:19:20
        /// </summary>
        private void GetStateOrders(List<Order> orders, ref List<Order> stateOrders)
        {
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    if (order.State == OrderStateType.Activ
                        || order.State == OrderStateType.Patrial
                        || order.State == OrderStateType.Pending)
                    {
                        stateOrders.Add(order);
                    }
                }
            }
        }

        /// <summary>
        /// Создание модели (Имя и чарт для графика свечей)
        /// Урок 4-37 0:25:17
        /// </summary>
        public void SetModel()
        {
            try
            {
                Model.Axes.Clear();
                Model.Title = Header;

                DateTimeAxis dateAxis = new DateTimeAxis()                                        // горизонтальная ось - время
                {
                    Position = AxisPosition.Bottom,
                    MinorIntervalType = DateTimeIntervalType.Auto,                          // Интервал     Урок 4-37   0:52:12
                    MajorGridlineStyle = LineStyle.Dot,
                    MinorGridlineStyle = LineStyle.Dot,
                    MajorGridlineColor = OxyColors.LightGray,
                    TicklineColor = OxyColor.FromRgb(82, 82, 82)                              // Урок 4-37   0:54:31
                };

                LinearAxis linearAxis = new LinearAxis()                                        // вертикальная ось - значения
                {
                    Position = AxisPosition.Right,
                    MajorGridlineStyle = LineStyle.Dot,
                    MinorGridlineStyle = LineStyle.Dot,
                    MajorGridlineColor = OxyColors.LightGray,
                    TicklineColor = OxyColor.FromRgb(82, 82, 82)
                };

                Model.Axes.Add(dateAxis);
                Model.Axes.Add(linearAxis);

                // Урок 4-37 01:01:37 : подписываемся на изм.данных Оси  by zooming, panning or resetting
                dateAxis.AxisChanged += (sender, e) => AdjustYExtent(Model.Title, _candleSeries, dateAxis, linearAxis);

                Model.Series.Add(_candleSeries);                        //  добавляем свечу в график Урок 4-37 0:50:17

                //( контекст Урок 4-37 1:19-45 - 22:56) Урок 4-37 01:23:15 + 01:25:28
                Controller = new PlotController();           

                //Назначаем левой кн мыши функции перемещения графика
                Controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);

                //Назначаем правой кн мыши функции показ св-ств графика
                Controller.BindMouseDown(OxyMouseButton.Right, PlotCommands.HoverSnapTrack);

            }
            catch (Exception ex)
            {
                RobotWindowVM.Log(Header, "MyRobotVM SetModel Error = " + ex.Message);
            }

        }

        /// <summary>
        /// Adjusts the Y extent.
        /// Урок 4-37 01:00:48
        /// </summary>
        private static void AdjustYExtent(string header, CandleStickSeries series, DateTimeAxis xaxis, LinearAxis yaxis)
        {
            try
            {
                // Защита, чтобы не начинать расчет min/max в series до поступления свечей
                if (series.Items == null                        
                    || series.Items.Count == 0) return;

                var xmin = xaxis.ActualMinimum;
                var xmax = xaxis.ActualMaximum;

                var istart = series.FindByX(xmin);
                var iend = series.FindByX(xmax, istart);

                var ymin = double.MaxValue;
                var ymax = double.MinValue;
                for (int i = istart; i <= iend; i++)
                {
                    var bar = series.Items[i];
                    ymin = Math.Min(ymin, bar.Low);
                    ymax = Math.Max(ymax, bar.High);
                }

                var extent = ymax - ymin;
                var margin = extent * 0.10;

                yaxis.Zoom(ymin - margin, ymax + margin);
            }
            catch (Exception ex)
            {
                RobotWindowVM.Log(header, "MyRobotVM AdjustYExtent Error = " + ex.Message);
            }

        }


        #endregion

        #region Events =====================================

        public delegate void onSelectedSecurity();
        public event onSelectedSecurity OnSelectedSecurity;                 // Событие на создание новой вкладки Tab

        #endregion


    }
}
