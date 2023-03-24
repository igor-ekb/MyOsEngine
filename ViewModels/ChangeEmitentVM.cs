using OsEngine.Commands;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.Huobi.Entities;
using OsEngine.MyEntity;
using OsEngine.Robots;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.ViewModels
{
    public class ChangeEmitentVM :BaseVM
    {
        public ChangeEmitentVM(MyRobotVM robot)
        {
            _robot = robot;

            Init();
        }

        #region Fields ===========================================

        // Словарь _classes : список бумапо классам
        Dictionary<string, List<Security>> _classes = new Dictionary<string, List<Security>>();

        // список классов
        List<EmitClass> emitClasses= new List<EmitClass>();
        
        private MyRobotVM _robot;

        private IServer _server = null;

        #endregion


        #region Properties =======================================

        // Создаем списки ObservableCollection ( не List, т.к. их состав может динамически меняться)
        public ObservableCollection<ExChange> ExChanges { get; set; } = new ObservableCollection<ExChange>();

        public ObservableCollection<EmitClass> EmitClasses { get; set; } = new ObservableCollection<EmitClass>();

        public ObservableCollection<Emitent> Securities { get; set; } = new ObservableCollection<Emitent>();

        public Emitent SelectedEmitent
        {
            get => _selectedEmitent;
            set
            {
                _selectedEmitent =value;

                OnPropertyChanged(nameof(SelectedEmitent));
            }

        }
        private Emitent _selectedEmitent;

        #endregion

        #region Commands =========================================

        public DelegateCommand CommandSetExchange
        {
            get 
            {
                if (commandSetExchange == null)
                {
                    commandSetExchange = new DelegateCommand(SetExchange);      // Нажатие кнопки выбора биржы
                }
                return commandSetExchange;
            }
        }
        private DelegateCommand commandSetExchange;

        public DelegateCommand CommandSetEmitClass
        {
            get
            {
                if (commandSetEmitClass == null)
                {
                    commandSetEmitClass = new DelegateCommand(SetEmitClass);
                }
                return commandSetEmitClass;
            }
        }
        private DelegateCommand commandSetEmitClass;

        public DelegateCommand CommandChange
        {
            get
            {
                if (commandChange == null)
                {
                    commandChange = new DelegateCommand(Change);
                }
                return commandChange;
            }
        }
        private DelegateCommand commandChange;

        #endregion

        #region Methods =========================================

        private void Change(object obj)
        {
            if (SelectedEmitent != null
                && SelectedEmitent.Security != null)
            {
                _robot.Server = _server;                                            // присваиваем Server

                _robot.StringPortfolios = _robot.GetStringPorfolios(_robot.Server); // считываем и присваиваем PortFolio

                _robot.SelectedSecurity = SelectedEmitent.Security;                 // присваиваем выбранную бумагу

                _robot.SetModel();

            }
        }


        void SetEmitClass(object obj)
        {
            string classEmit = (string)obj;

            List<Security> secList = _classes[classEmit];

            ObservableCollection<Emitent> emits = new ObservableCollection<Emitent>();

            foreach (Security sec in secList)
            {
                emits.Add(new Emitent(sec));
            }

            Securities = emits;
            OnPropertyChanged(nameof(Securities));
        }


        void SetExchange(object obj)
        {
            ServerType type = (ServerType)obj;                      // Определяем type сервера

            List<IServer> servers = ServerMaster.GetServers();

            List<Security> securities = null;

            foreach (IServer server in servers)
            {
                if (server.ServerType == type)
                {
                    securities = server.Securities;                 // Загружаем список Security(инструментов)

                    _server = server;

                    break;
                }
            }
            
            if (securities == null)
            {
                return;
            }

            _classes.Clear();

            EmitClasses.Clear();

            foreach (Security sec in securities)
            {
                if (_classes.ContainsKey(sec.NameClass))
                {
                    _classes[sec.NameClass].Add(sec);
                }
                else
                {
                    List<Security> secs = new List<Security>();

                    secs.Add(sec);

                    _classes.Add(sec.NameClass, secs);

                    EmitClasses.Add(new EmitClass(sec.NameClass));

                }
            }
        }

        void Init()
        {
            List<IServer> servers = ServerMaster.GetServers();      // Считываем список сервера из ServerMaster

            ExChanges.Clear();                                      // Очищаем ObservableCollection ExChanges

            if (servers == null || servers.Count == 0) return;

            foreach (IServer server in servers)
            {
                // Добавляем в ExChanges экземпляр класса ExChange, в который передаем ServerType 
                ExChanges.Add(new ExChange(server.ServerType));
            }
 
            OnPropertyChanged(nameof(ExChanges));
        }

        #endregion
    }
}
