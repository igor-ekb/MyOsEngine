using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using PusherClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

            Strateg = CreateParameter("Strategy", "FixLot", new[] { "FixLot", "FixRisk" });

            Lot = CreateParameter("Lot", 10, 5, 20, 1);

            Risk = CreateParameter("Risk %", 1m, 0.2m, 3m, 0.1m);

            UseLong = CreateParameter("UseLong", "Yes", new[] { "Yes", "No" });

            UseShort = CreateParameter("UseShort", "Yes", new[] { "Yes", "No" });

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

        private StrategyParameterString Mode;           // Запуск /останов робота - Of/On

        private StrategyParameterString Strateg;        // Выбор стратегии : 1 - фикс лот, 2 - фикс риск

        private StrategyParameterString UseLong;        // Работаем ли в Long

        private StrategyParameterString UseShort;        // Работаем ли в Short

        private StrategyParameterInt Lot;               // Для первой стратегии будем задавать Лоты,
        private StrategyParameterDecimal Risk;          // а для второй задаем Риск, а по нему рассчитываем размер лота

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

            // Оценка абсолютной суммы Риска, заданного в %
            decimal riskValue = _tab.Portfolio.ValueBegin * Risk.ValueDecimal / 100;

            // Прикидываем цену пункта
            decimal costPriceStep = _tab.Securiti.PriceStepCost;        // В тестере будет равно 0,
                                                                        //costPriceStep = 7m;                                       // поэтому для BRENT примерно 7 ))
            costPriceStep = 1m;                                         // для Si равно 1

            // определяем стоп в шагах цены (пунктах) для второй стратегии
            decimal stopSteps = (lastUp - lastDown) / _tab.Securiti.PriceStep;

            // Проверка колва шагов

            if (stopSteps == 0)
            {
                return;
            }

            // расчет размера лота из риска для второй стратегии
            decimal lot = riskValue / (stopSteps * costPriceStep);

            if ( UseLong.ValueString == "Yes")
            {
                // Условие для открытия Long - свеча пересекла предпоследний уровень верхнего канала lastUp
                if (candle.Close > lastUp
                    && candle.Open < lastUp
                    && _tab.PositionOpenLong.Count == 0)
                {
                    switch (Strateg.ValueString)
                    {
                        // int - спользуем для Лотов московской биржи

                        case "FixLot":
                            // Выставляем Take Long для первой стратегии ( напрямую задано кол-во лотов)
                            _tab.BuyAtMarket(Lot.ValueInt);

                            break;

                       case "FixRisk":
                        // Выставляем Take Long для второй стратегии фиксированного риска - кол-во лотов расчтывается
                        _tab.BuyAtMarket((int)lot);

                            break;

                        default:
                            break;
                    }
                }
            }

            if (UseShort.ValueString == "Yes")
            {
                 // Условие для открытия Long - свеча пересекла предпоследний уровень верхнего канала lastDown
                if (candle.Close < lastDown
                    && candle.Open > lastDown
                    && _tab.PositionOpenShort.Count == 0)
                {
                    switch (Strateg.ValueString)
                    {
                        // int - спользуем для Лотов московской биржи

                        case "FixLot":
                            // Выставляем Take Long для первой стратегии ( напрямую задано кол-во лотов)
                            _tab.SellAtMarket(Lot.ValueInt);

                            break;

                        case "FixRisk":
                            // Выставляем Take Long для второй стратегии фиксированного риска кол-во лотов lot - расчетное 
                            _tab.SellAtMarket((int)lot);

                            break;

                        default:
                            break;
                    }
                }
            }

            //Если есть открытые позиции, вызываем Трейлинг Стоп (отслеживаем )
            if (_tab.PositionsOpenAll.Count > 0)
            {
                Trailing();
            }

        }

        // Трейлинг Стопов  - подтягивание стопа к границе канала DataSeries.Last
         private void Trailing()
        {
            decimal lastDown = _pc.DataSeries[1].Last;

            foreach (Position pos in _tab.PositionOpenLong)
            {
                if (pos.State == PositionStateType.Open)
                {
                    if (pos.Direction == Side.Buy)
                    {
                        _tab.CloseAtTrailingStop(pos, lastDown, lastDown - 100 * _tab.Securiti.PriceStep);
                    }
                }
            }

            decimal lastUp = _pc.DataSeries[0].Last;

            foreach (Position pos in _tab.PositionOpenShort)
            {
                if (pos.State == PositionStateType.Open)
                {
                    if (pos.Direction == Side.Sell)
                    {
                        _tab.CloseAtTrailingStop(pos, lastUp, lastUp + 100 * _tab.Securiti.PriceStep);
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
