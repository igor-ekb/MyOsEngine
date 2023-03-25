using OsEngine.Commands;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.MyEntity;
using OsEngine.Robots;
using OsEngine.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

namespace OsEngine.ViewModels
{
    public class RobotWindowVM :BaseVM
    {
        public RobotWindowVM() 
        {
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;

            // Диспатчер для отдельного Task для телеграм Бота
            _dispatcher = Dispatcher.CurrentDispatcher;

            Task.Run(() =>
            {
                RecordLog();
            });

            Load();

            ServerMaster.ActivateAutoConnection();                   // Урок 3-32 01:00:48

            CreateTgBot();                                           // Урок 4-38 00:06:18   
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

        /// <summary>
        /// Статус Телеграм Бота
        /// Урок 4-38  0:12:35
        /// </summary>
        public string StateTg
        {
            get => _stateTg;

            set
            {
                _stateTg = value;
                OnPropertyChanged(nameof(StateTg));
            }

        }
        private string _stateTg = "Disconnected...";

        /// <summary>
        /// Сообщение Телеграм Бота
        /// Урок 4-38  0:31:46
        /// </summary>
        public string MessageTg
        {
            get => _messageTg;

            set
            {
                _messageTg = value;
                OnPropertyChanged(nameof(MessageTg));
            }

        }
        private string _messageTg = "";


        #endregion

        #region Fields =====================================

        /// <summary>
        /// Статическая очередь(чтобы исключить множественность одновременно запускаемых) сообщений при смены эмитента 
        /// </summary>
        private static ConcurrentQueue<MessageForLog> _logMessages = new ConcurrentQueue<MessageForLog>();

        public static ChangeEmitentWindow ChangeEmitentWindow = null;

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, Order>> Orders =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, Order>>();                          // Урок 3-33 00:05:05

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> MyTrades =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>>();                        // Урок 3-33 00:07:43

        private TelegramBotClient _botTg;

        // Диспатчер отдельного Task для телеграм Бота
        private Dispatcher _dispatcher ;

        /// <summary>
        /// список пользователей Телеграмм, с которыми будем взаимодействовать
        /// Урок 4-38 0:24:10
        /// </summary>
        private List<long> _botUsers = new List<long>()
        {
            813088223,      // my id
        };

        private string _tokenPay = "381764678:TEST:53168";

        // ig группы предыдущего курса     Урок 4-39  00:34:55
        private long _idChannel = -1001664102103;


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

            //  Подписываемся на событие отправки сообщения в Телеграмм из MyRobotVM.Calculate() - Урок 4-38 01:07:02
            Robots.Last().OnMessageTg += RobotWindowVM_OnMessageTg;
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
                    if (System.IO.File.Exists(@"Parameters\Tabs\param_" + header + ".txt"))
                    {
                        System.IO.File.Delete(@"Parameters\Tabs\param_" + header + ".txt");
                    }

                    Robots.Remove(delRobot);
                }
            }

            Save();
        }


        /// <summary>
        /// Добавление сообщений в ObservableCollection _logMessages для дальнейшей записи в журнал
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


        /// <summary>
        /// Метод  реакция на создание  новой вкладки Tab
        /// Урок 4-38   0:06:35
        /// </summary>
        private void CreateTgBot()
        {
            /// Создаем отдельный Task для Телеграм Бота
            /// Урок 4 - 38 0:14:48 - не забыть отправить в Dispatch !!!
            Task.Run(() =>
            {
                try
                {
                    // name 
                    _botTg = new TelegramBotClient("6140918556:AAEeNL3GqG58YpKnF3MWL73aLvaonEIzsOo");

                    // [Obsolete версия 16.2]
                    _botTg.StartReceiving();

                    // Направляем StateTg в наш поток  -  Урок 4 - 38 0:15:49
                    _dispatcher.Invoke(delegate ()
                    {
                        if (_botTg.BotId != null)
                        {
                            // Получаем BotId(присваивается когда произойдет соединение)   - Урок 4-38 0:13:46)
                            StateTg = _botTg.BotId.ToString();

                            // Подписываемся на событие Message Урок 4 - 38 0:18:00  !!!  [Obsolete версия 16.2]
                            _botTg.OnMessage += _botTg_OnMessage;

                            // Подписываемся на событие _botTg.OnUpdate Урок 4 - 38 0:18:46    !!!  [Obsolete версия 16.2]
                            _botTg.OnUpdate += _botTg_OnUpdate;
                        }
                        else
                        {
                            StateTg = "Disconnected...";
                            Log("Telegram", "State = Disconnected");
                        }
                        
                    });

                }
                catch (Exception ex)
                {
                    Log("Telegram", "CreateTgBot Error = " + ex.Message);
                }
            });
                
        }

        /// <summary>
        /// async
        /// [Obsolete Telegram 16.2 version]
        /// </summary>
        private async void _botTg_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            try
            {
                //Если в Телеге нажали "Оплатить", нужно послать подтверждение PreCheckoutQuery  - Урок 4 - 39 0:19:20
                if (e.Update.Type == UpdateType.PreCheckoutQuery)
                {
                    await _botTg.AnswerPreCheckoutQueryAsync(e.Update.PreCheckoutQuery.Id);
                }
                // Проверка того, что оплата произошла  - Урок 4 - 39 0:24:24
                else if (e.Update.Type == UpdateType.Message
                    && e.Update.Message.SuccessfulPayment != null
                    && e.Update.Message.SuccessfulPayment.TelegramPaymentChargeId != null)
                {
                    // Размер полученного платежа в копейках
                    int amount = e.Update.Message.SuccessfulPayment.TotalAmount / 100;

                    // Id оплаты
                    string paymentId = e.Update.Message.SuccessfulPayment.TelegramPaymentChargeId;

                    MessageBox.Show("Прошла оплата на сумму " + amount + "руб.");

                    // отправляем link юзеру
                    SendLinkToUser(e.Update.Message.From.Id);
                }


                if (e.Update.CallbackQuery == null) return;

                string callBack = e.Update.CallbackQuery.Data;

                string[] split = callBack.Split('|');

                if (string.IsNullOrEmpty(split[1])) return;

                int ind = Convert.ToInt32(split[1]);

                if (Robots.Count > ind)
                {
                    Robots[ind].StartStop(null);
                }

                await _botTg.SendTextMessageAsync(e.Update.CallbackQuery.From.Id, "Change state my bot");

                SendMyBots(e.Update.CallbackQuery.From.Id);

            }
            catch (Exception ex)
            {
                Log("Telegram", "_botTg_OnUpdate error = " + ex.Message);
            }            
        }


        /// <summary>
        /// Отсылка link на чат(_botTg права админа)  юзеру     Урок 4-39  00:34:55
        /// </summary>
        private async void SendLinkToUser(long idUser)
        {
            //Урок 4-39  00:47:50
            ChatInviteLink link = null;
            try
            {
                // Создаем срок действия = Now + 1 day
                DateTime dt = DateTime.Now.AddDays(1);

                // Создаем link на приглашение в канал для 1 user
                link = await _botTg.CreateChatInviteLinkAsync(_idChannel, dt, 1);

                // Вносим user в список UnbanChatMemberAsync чата _idChannel
                await _botTg.UnbanChatMemberAsync(_idChannel, idUser);
            }
            catch (Exception ex)
            {
                Log("Telegram", "SendLinkToUser UnbanChatMember Error = " + ex.Message);
            }

            try
            {
                if (link != null)
                {
                    // Отправляем юзеру ссылку link на вступление в группу Урок 4-39  00:40:00
                    await _botTg.SendTextMessageAsync(idUser, link.InviteLink);
                }
            }
            catch (Exception ex)
            {
                Log("Telegram", "SendLinkToUser InviteLink Error = " + ex.Message);
            }
        }


        /// <summary>
        /// Удаление user из группы Урок 4-39  00:49:50
        /// </summary>
        private async void RemoveUserFromChanel(long idUser)
        {
            try
            {
                await _botTg.KickChatMemberAsync(_idChannel, idUser);
            }
            catch (Exception ex)
            {
                Log("Telegram", "RemoveUserFromChanel  user = " + idUser + " = " + ex.Message);
            }
        }


        /// <summary>
        /// Взаимодействие с пользователем Телеграмм
        /// Урок 4-38 0:28:35
        /// </summary>
        private async void _botTg_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            long id = e.Message.From.Id;

            // проверка, кто обратился, состоит ли в моем списке 
            if (!IsMyUser(id)) return;

            MessageTg = e.Message.Text;


            // Пишем реакцию на команды из Телеграм (Пояснения  : Урок 4 - 38 0:50:35 - 0:51:04)
            switch (e.Message.Text)
            {
                //Если нажали "MyBots", выводим список всех роботов - Урок 4 - 38 0:53:09
                case "MyBots":
                    SendMyBots(id);
                    break;

                //Если нажали "Pays", выводим список всех роботов - Урок 4 - 39 0:07:26
                case "Pay":
                    Payments(id);
                    return;

                //default:
                //    break;
            }

            try
            {
                await _botTg.SendTextMessageAsync(id, "Select on action", replyMarkup: GetButtons());
            }
            catch (Exception ex)
            {
                Log("Telegram", "_botTg_OnMessage = " + ex.Message);
            }
        }


        /// <summary>
        /// Payments. Метод асинхронный, поэтому используем метод async
        /// Урок 4-39 0:07:46
        /// <param id ="адресат"/param>
        /// </summary>
        private async void Payments(long id)
        {
            // Прайс лист Урок 4-39 0:12:56
            List<LabeledPrice> prices = new List<LabeledPrice>();

            prices.Add(new LabeledPrice("Цена", 49900));

            try
            {
                Message mess = await _botTg.SendInvoiceAsync(id, "Ваш заказ", "Нажмите Оплатить и вы на все согласны", "Наш Товар",
                    _tokenPay, "rub", prices);
            }

            catch (Exception ex)
            {
                Log("Telegram", "Payments = " + ex.Message);
            }
        }


        /// <summary>
        /// Составление списка всех роботов. Метод асинхронный, поэтому используем метод async
        /// Урок 4-38 0:53:35
        /// <param id ="адресат"/param>
        /// </summary>
        private async void SendMyBots(long id)
        {
            // Создаем список строк/столбцов состоящий из кнопок Урок 4-38 0:54:10
            List<List<InlineKeyboardButton>> list = new List<List<InlineKeyboardButton>>();

            for ( int i = 0; i< Robots.Count; i++)
            {
                // В Button дописываем Header + i(индекс робота)
                InlineKeyboardButton button = 
                    InlineKeyboardButton.WithCallbackData(Robots[i].Header + " = " + Robots[i].IsRun.ToString(), Robots[i].Header + "|"+ i);

                // Кнопки состояния робота
                //InlineKeyboardButton state = InlineKeyboardButton.WithCallbackData(Robots[i].IsRun.ToString(), Robots[i].IsRun.ToString());

                list.Add(new List<InlineKeyboardButton>()
                { 
                    button,
                    //state
                });
            }

            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(list);

            try
            {
                await _botTg.SendTextMessageAsync(id, "My Robots:",  replyMarkup: inlineKeyboard);
            }
            catch (Exception ex)
            {
                Log("Telegram", "SendMyBots error = " + ex.Message);
            }
        }


        /// <summary>
        /// Урок 4-38 01:07:44
        /// </summary>
        private void RobotWindowVM_OnMessageTg(string message)
        {
            SendMessageTg(message);
        }


        /// <summary>
        /// Ответы  пользователю Телеграмм, public  - т.е можно вызвать из любого места
        /// Урок 4-38 0:33:57
        /// </summary>
        public async void SendMessageTg(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            foreach (long id in _botUsers)
            {
                await _botTg.SendTextMessageAsync(id, message, replyMarkup: GetButtons());
            }

        }

        /// <summary>
        /// Создание кнопок для Телеграм Бота
        /// (контекст Урок 4-38 0:40:12 - 0:41:22)    0:41:22 -
        /// </summary>
        private IReplyMarkup GetButtons()
        {
            ReplyKeyboardMarkup keys = new ReplyKeyboardMarkup();

            keys.Keyboard = new List<List<KeyboardButton>>()
            {
                // первая строка кнопок --  Закомментировали ))
                //new List<KeyboardButton>() { new KeyboardButton("Start"), new KeyboardButton("Stop") },

                // Составим список кнопок все ботов Урок 4 - 38 0:52:22
                new List<KeyboardButton>() 
                {
                    // Кнопка список кнопок все ботов Урок 4 - 38 0:52:22
                    new KeyboardButton("MyBots"),

                    // Добавляем кнопку тестовой оплаты   Урок 4-39 0:05:43
                    new KeyboardButton("Pay")
                }

            };

            // Уменьшение размеров кнопок Урок 4-38 0:50:08
            keys.ResizeKeyboard = true;

            return keys;
        }


        /// <summary>
        /// Проверка на соответствие списоку пользователей Телеграмм, с которыми взаимодействуем
        /// Урок 4-38 0:27:22
        /// </summary>
        private bool IsMyUser(long id)
        {
            foreach (long idUser in _botUsers)
            {
                if (idUser == id) return true;
            }
            return false;
        }



        #endregion
    }
}
