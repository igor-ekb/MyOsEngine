using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OsEngine.Robots.CryptoCluster
{
    public class MyRobotCluster : BotPanel
    {
        public MyRobotCluster(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            _tabSimple = TabsSimple[0];

            TabCreate(BotTabType.Cluster);

            _tabCluster = TabsCluster[0];

            _tabSimple.CandleFinishedEvent += _tabSimple_CandleFinishedEvent;

            Mode = CreateParameter("Mode", "Off", new[] { "Off", "On" });

            Depo = CreateParameter("Depo", 10000m, 1000m, 10000m, 1000m);

            Risk = CreateParameter("Risk %", 1m, 0.1m, 2m, 0.1m);

            Koef = CreateParameter("Коэффициет объема", 3, 2, 10, 1);

            MinClustVolume = CreateParameter("Min Cluster Volume $", 100000, 100000, 500000, 50000);

            Take = CreateParameter("Take ATR", 4, 1, 50, 1);
            Stop = CreateParameter("Stop ATR", 1, 1, 10, 1);
            LengthAtr = CreateParameter("Период ATR", 100, 10, 1000, 10);

            _atr = IndicatorsFactory.CreateIndicatorByName("ATR", name + "ATR", false);     // из PriceChannelVolatility.cs
            
            _atr.ParametersDigit[0].Value = LengthAtr.ValueInt;                             // период для ATR

            _atr = (Aindicator)_tabSimple.CreateCandleIndicator(_atr, "Second");
            _atr.PaintOn= true;                                                             // прорисовка на Чарте
            _atr.Save();

            _tabSimple.PositionOpeningSuccesEvent += _tabSimple_PositionOpeningSuccesEvent;
        }

       
        #region Fields =======================================================

        BotTabSimple _tabSimple;                    // Вкладка для обычных свечей

        BotTabCluster _tabCluster;                  // Вкладка для кластеров

        public StrategyParameterString Mode;        // Режим работы Off/ On

        public StrategyParameterString Strategy;    // Long / Short / Both

        public StrategyParameterInt CountCandles;   // Кол-во свечей для анализа

        public StrategyParameterInt Koef;           // коэф. во сколько раз объем последней свечи превышал ср.объем CountCandles

        public StrategyParameterInt MinClustVolume;  // Отсечка по мин.объему кластера ( > 100 000 $ )

        public StrategyParameterDecimal Depo;

        public StrategyParameterDecimal Risk;

        public StrategyParameterInt Take;

        public StrategyParameterInt Stop;

        private Aindicator _atr;

        private decimal _takePrice = 0;

        private decimal _stopPrice = 0;

        /// <summary>
        /// Atr period
        /// период ATR
        /// </summary>
        public StrategyParameterInt LengthAtr;      


        #endregion

        #region Methods ======================================================

        private void _tabSimple_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count < CountCandles.ValueInt
                || _tabCluster.VolumeClusters.Count < CountCandles.ValueInt
                || _tabCluster.VolumeClusters == null)
            {
                return;
            }

            List<Position> positions = _tabSimple.PositionOpenLong;

            if (positions == null || positions.Count == 0)
            {
                decimal average = 0;

                for (int i = _tabCluster.VolumeClusters.Count - CountCandles.ValueInt;
                    i < _tabCluster.VolumeClusters.Count - 2; i++)                              // Убедится в "-2"
                {
                    average += _tabCluster.VolumeClusters[i].MaxSummVolumeLine.VolumeSumm - 1;
                }

                average /= CountCandles.ValueInt - 1;

                HorizontalVolumeLine last = _tabCluster.VolumeClusters[_tabCluster.VolumeClusters.Count - 2].MaxSummVolumeLine;

                if (last.VolumeSumm > average*Koef.ValueInt
                    && last.VolumeDelta < 0
                    && last.VolumeSumm * last.Price > MinClustVolume.ValueInt)
                {
                    decimal lastATR = _atr.DataSeries[0].Last;

                    decimal manyRisk = Depo.ValueDecimal * Risk.ValueDecimal / 100;

                    decimal volume = manyRisk / (lastATR * Stop.ValueInt);

                    _tabSimple.BuyAtMarket(volume);

                    _stopPrice = candles[candles.Count - 1].Close - Stop.ValueInt * lastATR;

                    _takePrice = candles[candles.Count - 1].Close + Take.ValueInt * lastATR;
                }
            }
        }

        private void _tabSimple_PositionOpeningSuccesEvent(Position pos)
        {
            if (pos.State == PositionStateType.Open )
            {
                _tabSimple.CloseAtStop(pos, _stopPrice, _stopPrice - 100 * _tabSimple.Securiti.PriceStep);
                //_tabSimple.CloseAtProfit(pos, _takePrice, _takePrice);
                _tabSimple.CloseAtLimit(pos, _takePrice,pos.OpenVolume);
            }
        }

        public override string GetNameStrategyType()
        {
            return nameof(MyRobotCluster);
        }

        public override void ShowIndividualSettingsDialog()
        {

        }


        #endregion
    }
}
