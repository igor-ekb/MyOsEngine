using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsTrader.Panels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OsEngine.Robots.HFT
{
    public class HFTRobot : BotPanel

    {
        public HFTRobot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;
        }

        #region Fields ================================================

        private List<IServer> _servers = new List<IServer>();

        private List<Portfolio> _portfolios = new List<Portfolio>();

        private List<Security> _securities = new List<Security>();

        private string _nameSecurity = "BTCUSDT";

        private ServerType _serverType = ServerType.Binance;

        private Security _security = null;   // выбранная бумага/актив

        private IServer _server;  // выбранная биржа

        private CandleSeries _series = null ;   // Серия свечей

        #endregion


        #region Methods ===============================================

        private void ServerMaster_ServerCreateEvent(IServer newServer)
        {
            foreach(IServer server in _servers) 
            {
                if (server == newServer)
                {
                    return;
                }
            }

            if (newServer.ServerType == _serverType)
            {
                _server = newServer;
            }

            _servers.Add(newServer);

            newServer.PortfoliosChangeEvent += NewServer_PortfoliosChangeEvent;     // подписка на изменение портфеля
            newServer.SecuritiesChangeEvent += NewServer_SecuritiesChangeEvent;     // подписка на изменение в бумаге
            newServer.NeadToReconnectEvent += NewServer_NeadToReconnectEvent;       // требуется перезаказ бумаги у сервера
            newServer.NewMarketDepthEvent += NewServer_NewMarketDepthEvent;         // подписка  на стакан
            newServer.NewTradeEvent += NewServer_NewTradeEvent;                     // подписка на пул обезличенных сделок
            newServer.NewOrderIncomeEvent += NewServer_NewOrderIncomeEvent;         // подписка на события по ордерам
            newServer.NewMyTradeEvent += NewServer_NewMyTradeEvent;                 // произошла моя сделка
        }

        private void NewServer_NewMyTradeEvent(MyTrade myTrade)
        {
            
        }

        private void NewServer_NewOrderIncomeEvent(Order order)
        {
            
        }

        private void NewServer_NewTradeEvent(List<Trade> trades)
        {
            
        }

        private void NewServer_NewMarketDepthEvent(MarketDepth marketDepth)
        {
            
        }

        private void NewServer_NeadToReconnectEvent()
        {
            StartSecurity(_security);
        }

        private void NewServer_SecuritiesChangeEvent(List<Security> securities)
        {
            if (_security != null)
            {
                return;
            }
            for (int i = 0; i < securities.Count; i++)
            {
                if (_security.Name == _securities[i].Name)
                {
                    _security = securities[i];

                    StartSecurity(_security);

                    break;
                }
            }
        }

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
                    _series = _server.StartThisSecurity(security.Name, new TimeFrameBuilder(), security.NameClass);

                    if (_series != null)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }
            });

        }

        private void NewServer_PortfoliosChangeEvent(List<Portfolio> newPortfolios)
        {
            for (int x = 0; x < newPortfolios.Count; x++)
            {
                bool flag = true;

                for (int i = 0; i < _portfolios.Count; i++)
                {
                    if (newPortfolios[x].Number == _portfolios[i].Number)
                    {
                        flag = false;

                        break;
                    }
                }
                if (flag)
                {
                    _portfolios.Add(newPortfolios[x]);
                }
            }
        }

        public override string GetNameStrategyType()
        {
            return nameof(HFTRobot);
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }

        #endregion
    }
}
