using OsEngine.Commands;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.MyEntity;
using OsEngine.Robots;
using OsEngine.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OsEngine.ViewModels
{
    public class RobotWindowVM :BaseVM
    {
        public RobotWindowVM() 
        {
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;

            Task.Run(() =>
            {
                RecordLog();
            });

            Load();

            ServerMaster.ActivateAutoConnection();                                  // Урок 3-32 01:00:48
        }


        #region Properties =====================================

        public ObservableCollection<MyRobotVM> Robots { get; set; } = new ObservableCollection<MyRobotVM>();

        public MyRobotVM SelectedRobot
        {
            get => _selectedRobot;

            set
            {
                value = _selectedRobot;

                OnPropertyChanged(nameof(SelectedRobot));
            }
        }
        public MyRobotVM _selectedRobot;

        #endregion

        #region Fields =====================================

        /// <summary>
        /// Статический(чтобы исключить множественность одноременно запускаемых) окно для смены эмитента 
        /// </summary>
        /// 
        private static ConcurrentQueue<MessageForLog> _logMessages = new ConcurrentQueue<MessageForLog>();

        public static ChangeEmitentWindow ChangeEmitentWindow = null;

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, Order>> Orders =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, Order>>();                        // Урок 3-33 00:05:05

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> MyTrades =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>>();                        // Урок 3-33 00:07:43


        #endregion

        #region Command =====================================

        public DelegateCommand CommandServersToConnect
        {
            get
            {
                if (commandServersToConnect == null)
                {
                    commandServersToConnect = new DelegateCommand(ServersToConnect);
                }
                return commandServersToConnect;
            }
        }
        private DelegateCommand commandServersToConnect;

        public DelegateCommand CommandAddEmitent
        {
            get
            {
                if (commandAddEmitent == null)
                {
                    commandAddEmitent = new DelegateCommand(AddTabEmitent);
                }
               return commandAddEmitent;
            }
        }
        private DelegateCommand commandAddEmitent;

        public DelegateCommand CommandDeleteTab
        {
            get
            {
                if (commandDeleteTab == null)
                {
                    commandDeleteTab = new DelegateCommand(DeleteTabEmitent);
                }
                return commandDeleteTab;
            }
        }
        private DelegateCommand commandDeleteTab;

        #endregion

        #region Methods =====================================

        /// <summary>
        /// Урок 3-33 0:08:11 - 
        /// </summary>
        private void ServerMaster_ServerCreateEvent(Market.Servers.IServer server)
        {
            server.NewOrderIncomeEvent += Server_NewOrderIncomeEvent;
            server.NewMyTradeEvent += Server_NewMyTradeEvent;
            server.ConnectStatusChangeEvent += Server_ConnectStatusChangeEvent;
        }


        /// <summary>
        /// Урок 3-33 0:08:11,   0:22:08 - 0:26:36,  00:34:10 - 
        /// </summary>
        private void Server_ConnectStatusChangeEvent(string state)
        {
            if (state == "Connect")
            {
                Task.Run(async() =>                          // Урок 3-33 1:08:09
                {
                    DateTime dt = DateTime.Now;

                    while (dt.AddMinutes(1) > DateTime.Now)
                    {
                        await Task.Delay(5000);

                        foreach (MyRobotVM robot in Robots)
                        {
                            robot.CheckMissedOrders();

                            robot.CheckMissedMyTrades();
                        }
                    }
                });
            }
        }


        /// <summary>
        /// Урок 3-33 0:12:42 - 
        /// </summary>
        private void Server_NewMyTradeEvent(MyTrade myTrade)
        {
            ConcurrentDictionary<string, MyTrade> myTrades = null;    // создаем локальный словарь

            if (MyTrades.TryGetValue(myTrade.SecurityNameCode, out myTrades))
            {
                // Добавляем в myTrades полученные key(NumberTrade), и значение value (myTrade) с привязкой к NumberTrade
                myTrades.AddOrUpdate(myTrade.NumberTrade, myTrade, (key, value) => value = myTrade);
            }
            else
            {
                // Если коллекции MyTrades(myTrade.SecurityNameCode, myTrades) не существует, создаем ее
                myTrades = new ConcurrentDictionary<string, MyTrade>();

                myTrades.AddOrUpdate(myTrade.NumberTrade, myTrade, (key, value) => value = myTrade);

                MyTrades.AddOrUpdate(myTrade.SecurityNameCode, myTrades, (key, value) => value = myTrades);  //и Добавляем в глобальн.словарь
            }
        }


        /// <summary>
        /// Урок 3-33 0:19:29
        /// </summary>
        private void Server_NewOrderIncomeEvent(Order order)
        {
            ConcurrentDictionary<string, Order> orders = null;    // создаем локальный словарь

            if (Orders.TryGetValue(order.SecurityNameCode, out orders))
            {
                // Добавляем в orders полученные key(NumberMarket), и значение value (order) с привязкой к NumberMarket
                orders.AddOrUpdate(order.NumberMarket, order, (key, value) => value = order);
            }
            else
            {
                // Если коллекции orders(Orders.SecurityNameCode, orders) не существует, создаем ее
                orders = new ConcurrentDictionary<string, Order>();

                orders.AddOrUpdate(order.NumberMarket, order, (key, value) => value = order);

                Orders.AddOrUpdate(order.SecurityNameCode, orders, (key, value) => value = orders);  //и Добавляем в глобальн.словарь
            }
        }

        void ServersToConnect(object o)
        {
            ServerMaster.ShowDialog(false);
        }

        void AddTabEmitent(object obj)
        {
            AddTab("");
        }


        /// <summary>
        /// Добавление вкладки Tab
        /// Урок 3-32 0:13:41 - 
        /// </summary>
        void AddTab(string name)
        {
            if (name != "")
            {
                Robots.Add(new MyRobotVM(name, Robots.Count + 1));
            }
            else
            {
                Robots.Add(new MyRobotVM("Tab" + Robots.Count + 1, Robots.Count + 1));
            }

            Robots.Last().OnSelectedSecurity += RobotWindowVM_OnSelectedSecurity;   // Подписка на создание новой вкладки
        }


        void DeleteTabEmitent(object obj)
        {
            string header = (string)obj;

            MyRobotVM delRobot = null;

            foreach (var robot in Robots)
            {
                if (robot.Header == header)
                {
                    delRobot = robot;
                    break;
                }
            }

            if (delRobot != null)
            {
                MessageBoxResult res = MessageBox.Show("Remove tab " + header + "?", header, MessageBoxButton.YesNo);

                if (res == MessageBoxResult.Yes )
                {
                    Robots.Remove(delRobot);
                }
            }
        }

        /// <summary>
        /// Логирование событий в журнал
        /// </summary>
        /// <param name="str"></param>
        public static void Log(string name, string mess)
        {
            MessageForLog messageForLog = new MessageForLog()
            {
                Name = name,

                Message = mess
            };

            _logMessages.Enqueue(messageForLog);
        }

        private static void RecordLog()
        {
            if (!Directory.Exists(@"Log"))                      // Folder   OsEngine\bin\Debug
            {
                Directory.CreateDirectory(@"Log");
            }

            while (MainWindow.ProccesIsWorked)
            {
                MessageForLog mess;

                if (_logMessages.TryDequeue(out mess))
                {
                    string name = mess.Name + "_" + DateTime.Now.ToShortDateString() + ".log";
                    using (StreamWriter writer = new StreamWriter(@"Log\" + name, true))
                    {
                        writer.WriteLine(mess.Message);

                        writer.Close();
                    }
                }

                Thread.Sleep(5);
            }
        }


        /// <summary>
        /// Сохранение имени и номера вкладки Tab
        /// Урок 3-32 0:0:22 - 0:08:33 ; 
        /// </summary>
        private void Save()
        {
            if (Robots == null || Robots.Count == 0) return;
            {

            }

            if (!Directory.Exists(@"Parameters"))                           // Folder   bin\Debug\Parameters
            {
                Directory.CreateDirectory(@"Parameters");
            }
            string str = "";

            for (int i = 0; i < Robots.Count; i++)
            {
                str += Robots[i].Header + "=" + i + ";";
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(@"Parameters\param.txt", false))
                {
                    writer.WriteLine(str);

                    //writer.WriteLine(SelectedRobot.NumberTab);
                    writer.WriteLine(Robots[Robots.Count - 1].NumberTab);

                    writer.Close();
                }
            }
            catch (Exception ex)
            {

                Log("App RobotWindowVM", "Save error = " + ex.Message);
            }
        }


        /// <summary>
        /// Считывание имени и номера вкладки Tab
        /// Урок 3-32  0:08:33 - 0:13:41 ; 
        /// </summary>
        private void Load()
        {
            if (!Directory.Exists(@"Parameters"))                           // Folder   bin\Debug\Parameters
            {
                return;
            }

            string strTabs = "";

            int SelectedNumber = 0;

            try
            {
                using (StreamReader reader = new StreamReader(@"Parameters\param.txt"))
                {
                    strTabs = reader.ReadLine();

                    SelectedNumber = Convert.ToInt32(reader.ReadLine());

                    reader.Close();
                }
            }

            catch (Exception ex)
            {

                Log("App RobotWindowVM", "Load error = " + ex.Message);
            }

            string[] tabs = strTabs.Split(';');

            foreach (string tab in tabs)
            {
                if (tab != "")
                {
                    AddTab(tab);
                }
            }

            if (Robots.Count > SelectedNumber)
            {
                SelectedRobot = Robots[SelectedNumber - 1];
            }
        }

        /// <summary>
        /// Метод  реакция на создание  новой вкладки Tab
        /// Урок 3-32  (до 0:27:00
        /// </summary>
        private void RobotWindowVM_OnSelectedSecurity()
        {
            Save();                 // Сохранение в файл Parameters\param.txt имени/n и номера вкладки Tab
        }

        #endregion
    }
}
