using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using PusherClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.MyPriceChanel
{
    public class MyPriceChanelFix : BotPanel
    {
        public MyPriceChanelFix(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            _tab = TabsSimple[0];

            LengthUp = CreateParameter("Length Channel Up", 12, 5, 80, 2);
            LengthDown = CreateParameter("Length Channel Down", 12, 5, 80, 2);

            Mode = CreateParameter("Mode", "Off", new[] { "Off", "On" });

            Lot = CreateParameter("Lot", 10, 5, 20, 1);

            Risk = CreateParameter("Risk %", 1m, 0.2m, 3m, 0.1m);

            _pc = IndicatorsFactory.CreateIndicatorByName("PriceChannel", name + "PriceChannel", false);

            _pc.ParametersDigit[0].Value = LengthUp.ValueInt;

            _pc.ParametersDigit[1].Value = LengthDown.ValueInt;

            _pc = (Aindicator)_tab.CreateCandleIndicator(_pc, "Prime");

            _pc.Save();

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
        }

        

        #region Fields =============================================

        private BotTabSimple _tab;

        private Aindicator _pc;

        private StrategyParameterInt LengthUp;

        private StrategyParameterInt LengthDown;

        private StrategyParameterString Mode;         // Запуск - останов робота

        private StrategyParameterInt Lot;           // Для первой стратегии будем задавать Лоты,
        private StrategyParameterDecimal Risk;      // а для второй - Риск

        #endregion


        #region Methods =============================================

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            // Если робот выключен
            if (Mode.ValueString == "Off")
            {
                return;
            }

            // если не хватает свечей на расчет индикаторов
            if (_pc.DataSeries[0].Values == null
                || _pc.DataSeries[1].Values == null
                || _pc.DataSeries[0].Values.Count < LengthUp.ValueInt + 1      
                || _pc.DataSeries[1].Values.Count < LengthDown.ValueInt + 1)
            {
                return;
            }

            // Логика бота
            Candle candle = candles[candles.Count- 1];

            //Значение индикатора UP на предпоследней свече
            decimal lastUp = _pc.DataSeries[0].Values[_pc.DataSeries[0].Values.Count - 2];

            //Значение индикатора Down на предпоследней свече
            decimal lastDown = _pc.DataSeries[1].Values[_pc.DataSeries[1].Values.Count - 2];

            //проверка налиция открытых позиций
            List<Position> positions = _tab.PositionOpenLong;

            if (candle.Close > lastUp
                && candle.Open < lastUp
                && positions.Count == 0)
            {
                // Оценка суммы Риска
                decimal riskValue = _tab.Portfolio.ValueBegin * Risk.ValueDecimal / 100;

                // Прикидываем цену пункта
                decimal costPriceStep = _tab.Securiti.PriceStepCost;        // В тестере будет равно 0,
                //costPriceStep = 7m;                                       // поэтому для BRENT назначаем примерно 7 ))
                costPriceStep = 1m;                                         // для Si равно 1

                // определяем стоп в шагах цены (пунктах)
                decimal stopSteps = (lastUp - lastDown)/_tab.Securiti.PriceStep;

                decimal lot = riskValue / (stopSteps * costPriceStep);

                // Take для второй стратегии фиксированного риска
                //_tab.BuyAtMarket((int)lot);                                 // int - спользуем для московской биржи

                // Take для первой стратегии
                _tab.BuyAtMarket(Lot.ValueInt);
            }

            //Логика для Стоп
            if (positions.Count > 0)
            {
                Trailing(positions);
            }

        }

        private void Trailing(List<Position> positions)
        {
            decimal lastDown = _pc.DataSeries[1].Last;

            foreach (Position pos in positions)
            {
                if (pos.State == PositionStateType.Open)
                {
                    if (pos.Direction == Side.Buy)
                    {
                        _tab.CloseAtTrailingStop(pos, lastDown, lastDown - 100 * _tab.Securiti.PriceStep);
                    }
                }
            }
        }

        public override string GetNameStrategyType()
        {
            return nameof(MyPriceChanelFix);
        }

        public override void ShowIndividualSettingsDialog()
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
