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

        /// <summary>
        /// Выбранный робот 
        /// Урок 4-37  0:02:06
        /// </summary>
        public MyRobotVM SelectedRobot
        {
            get => _selectedRobot;

            set
            {
                _selectedRobot = value;
                OnPropertyChanged(nameof(SelectedRobot));
            }
        }
        public MyRobotVM _selectedRobot;

        #endregion

        #region Fields =====================================

        /// <summary>
        /// Статическая очередь(чтобы исключить множественность одноременно запускаемых) окно для смены эмитента 
        /// </summary>
        private static ConcurrentQueue<MessageForLog> _logMessages = new ConcurrentQueue<MessageForLog>();

        public static ChangeEmitentWindow ChangeEmitentWindow = null;

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, Order>> Orders =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, Order>>();                          // Урок 3-33 00:05:05

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
        /// <param name="Security.Name_NumberTab"><</param>
        /// </summary>
        void AddTab(string name)
        {
            int tabCount = Robots.Count + 1;

            if (name != "")
            {
                // Создание MyRobotVM c header = name и загрузкой параметров MyRootVM.Load() из файла TABS\Param_name.txt
                Robots.Add(new MyRobotVM(name, Robots.Count + 1));
            }
            else
            {
                // Проверка : создание нового  MyRobotVM c начальным header = "TAB"+(Robots.Count + 1)
                // Robots.Add(new MyRobotVM("Tab" + tabCount, Robots.Count + 1));
                Robots.Add(new MyRobotVM(Robots.Count + 1));
            }

            // Подписка на создание новой вкладки (запуск Save()  в param.txt обновленного списка роботов(TABs) 
            Robots.Last().OnSelectedSecurity += RobotWindowVM_OnSelectedSecurity;
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
                    if (File.Exists(@"Parameters\Tabs\param_" + header + ".txt"))
                    {
                        File.Delete(@"Parameters\Tabs\param_" + header + ".txt");
                    }

                    Robots.Remove(delRobot);
                }
            }

            Save();
        }


        /// <summary>
        /// Добавление событий в ObservableCollection _logMessages для дальнейшей записи в журнал
        /// </summary>
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
            if (!Directory.Exists(@"Log"))                      // Folder   bin\Debug\Log
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
        /// Сохранение "Parameters\param.txt" списка имен+номер вкладок Tab и (SelectedRobot.Head)
        /// Урок 3-32 0:0:22 - 0:08:33 ;  Урок 4-37 0:03:13 (SelectedRobot)
        /// </summary>
        private void Save()
        {
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

                    if (Robots == null || Robots.Count == 0 || SelectedRobot == null)
                    {
                        //writer.WriteLine(Robots[0].Header);
                    }

                    else
                    {
                        // Урок 4-37 0:03:22 (SelectedRobot)
                        writer.WriteLine(SelectedRobot.Header);
                        //writer.WriteLine(Robots[Robots.Count - 1].Header);
                    }

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Log("App ", "Save error = " + ex.Message);
            }
        }


        /// <summary>
        /// Считывание из "Parameters\param.txt" списка имен+номер вкладок Tab и (SelectedRobot.Head)
        /// Урок 3-32  0:08:33 - 0:13:41 ; Урок 4-37 0:03:55 (SelectedRobot)
        /// </summary>
        private void Load()
        {
            if (!Directory.Exists(@"Parameters"))                           // Folder   bin\Debug\Parameters
            {
                return;
            }

            string strTabs = "";

            // Урок 4-37 0:03:55 (SelectedRobot)
            string header = "";

            try
            {
                using (StreamReader reader = new StreamReader(@"Parameters\param.txt"))
                {
                    strTabs = reader.ReadLine();

                    // Урок 4-37 0:04:06 (SelectedRobot)
                    header = reader.ReadLine();

                    reader.Close();
                }
            }

            catch (Exception ex)
            {

                Log("App ", "Load error = " + ex.Message);
            }

            string[] tabs = strTabs.Split(';');

            foreach (string tab in tabs)
            {
                if (tab != "")
                {
                    AddTab(tab);

                    // Урок 4-37 0:04:45 (SelectedRobot)
                    if (Robots.Last().Header == header )
                    {
                        // Урок 4-37 0:05:10 (SelectedRobot)
                        SelectedRobot = Robots.Last();
                    }
                }
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
