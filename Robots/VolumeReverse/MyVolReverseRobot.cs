using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
//using System.Windows.Media;
using System.Drawing;

namespace OsEngine.Robots.VolumeReverse
{
    /// <summary>
    /// Робот "Объемный  разворот"
    ///        Основной алгоритм :  условие покупки :отслеживать наличие последовательности из заданного количества падающих свечей,
    ///     + последняя свеча закрывается выше своей середины и ее объем превышает средний объем у заданного кол-ва последних свечей.
    ///        После выставляются стор-лосс на уровнь минимума последней свечи; Тейк профит по уровню стоп * profitKoef.
    ///        
    ///         ДЗ :    1. Сделать перенос стопа в безубыток, когда цена пройдет в сторону тейк профита
    ///                     расстояние равное стоп лоссу.
    ///                 2. Выставить два тейк профита - первый на величину стоп лосса,
    ///                     а второй на заданный тейк (стоп * profitKoef) Без переноса в безубыток.
    ///          PS: по своей инициативе добавлена возможность прорисовки на графике индикаторов быстрая и медленная SMA.
    ///          
    ///             Условия выставления заявок выставляются в параметрах Робота.
    ///             
    /// </summary>
    public class MyVolReverseRobot : BotPanel
    {
        public MyVolReverseRobot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            this.TabCreate(BotTabType.Simple);

            _tab = TabsSimple[0];

            this.CreateParameter("Mode", "Edit", new[] { "Edit", "Trade" });

            _risk = this.CreateParameter("Risk %", 0.002m, 0.1m, 10m, 0.1m);

            _profitKoef = this.CreateParameter("Koef Profit", 2m, 0.1m, 10m, 0.1m);

            _countDownCandels = this.CreateParameter("DownCandels Amount", 2, 1, 5, 1);

            _koefVolume = this.CreateParameter("Koef Volume", 1.3m, 2m, 10m, 0.5m);

            _countCandels = this.CreateParameter("averedge Volume candles amount", 10, 5, 50, 1);

            _trailStop = this.CreateParameter("TrailingStop ?", "No", new[] { "No", "Yes" });

            _dividePos = this.CreateParameter("Divide Pos ?", "No", new[] { "No", "Yes" });

            _smaPeriod1 = this.CreateParameter("Период SMA1", 20, 5, 600, 1);

            _smaPeriod2 = this.CreateParameter("Период SMA2", 120, 5, 600, 1);

            _sma1 = new MovingAverage("SMA" + _smaPeriod1.ValueInt , true) { Lenght = _smaPeriod1.ValueInt,
                TypeCalculationAverage = MovingAverageTypeCalculation.Exponential, ColorBase = Color.LightYellow };

            _sma2 = new MovingAverage( "SMA" + _smaPeriod2.ValueInt, true) { Lenght = _smaPeriod2.ValueInt,
                TypeCalculationAverage = MovingAverageTypeCalculation.Exponential, ColorBase = Color.LightSkyBlue };

            _tab.CreateCandleIndicator( _sma1,"PRIME");

            _tab.CreateCandleIndicator( _sma2, "PRIME");

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;

            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;

            _smaPeriod1.ValueChange += _smaPeriod1_ValueChange;

            _smaPeriod2.ValueChange += _smaPeriod2_ValueChange;

        }

         #region Fields =============================================================

        private BotTabSimple _tab;

        /// <summary>
        /// Риск на сделку
        /// </summary>
        private StrategyParameterDecimal _risk;

        /// <summary>
        /// Во сколько раз тейк больше риска
        /// </summary>
        private StrategyParameterDecimal _profitKoef;

        /// <summary>
        /// Кол-во свечей перед Объемным разворотом
        /// </summary>
        private StrategyParameterInt _countDownCandels;

        /// <summary>
        /// Во сколько раз объем превышает средний
        /// </summary>
        private StrategyParameterDecimal _koefVolume;

        /// <summary>
        /// Кол-во свечей для вычисления ср. объема
        /// </summary>
        private StrategyParameterInt _countCandels;

        /// <summary>
        /// Trailing Перенос стопа в безубыток
        /// </summary>
        private StrategyParameterString _trailStop;

        /// <summary>
        /// Разделять позицию по TakeProfit ?
        /// </summary>
        private StrategyParameterString _dividePos;

        /// <summary>
        /// Период для SMA1
        /// </summary>
        private StrategyParameterInt _smaPeriod1;

        /// <summary>
        /// Период для SMA2
        /// </summary>
        private StrategyParameterInt _smaPeriod2;

        /// <summary>
        /// Средний объем
        /// </summary>
        private decimal _averageVolume;

        /// <summary>
        /// Кол-во пунктов до стоп-лосс
        /// </summary>
        private int _punkt = 0;

        /// <summary>
        /// Уровень стопа по Min свечи
        /// </summary>
        private decimal _lowCandle = 0;

        /// <summary>
        /// Индикатор1 для построения быстрой SMA1
        /// </summary>
        private MovingAverage _sma1;

        /// <summary>
        /// Индикатор2 для построения медленной SMA2
        /// </summary>
        private MovingAverage _sma2;

        #endregion

        #region Methods =============================================================

        // Изменение периода Быстрой SMA1
        private void _smaPeriod1_ValueChange()
        {
            _sma1.Lenght = _smaPeriod1.ValueInt;
            _sma1.ColorBase = Color.DarkRed;
        }

        // Изменение периода медленной SMA2
        private void _smaPeriod2_ValueChange()
        {
            _sma2.Lenght = _smaPeriod2.ValueInt;
            _sma2.ColorBase = Color.Indigo;
        }

        /// <summary>
        /// Проверка условий для выставления заявки
        /// </summary>
        /// <param name="candles"></param>
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            // Проверяем кол-во свечей для анализа
            if (candles.Count < _countDownCandels.ValueInt + 1
                || candles.Count < _countDownCandels.ValueInt + 1)
            {
                return;
            }

            _averageVolume = 0;

            // цикл анализа предыдущих свечей
            for (int i = candles.Count - 2; i > candles.Count - 2 - _countDownCandels.ValueInt; i--)
            {
                // Была ли последняя свеча падающей?
                if (candles[i].Close > candles[i].Open)
                {
                    return;
                }
                // дополнительно считаем средний объем свечей
                _averageVolume += candles[i].Volume;

            }
            // усредняем средний объем
            _averageVolume /= _countDownCandels.ValueInt;

            List<Position> positions = _tab.PositionOpenLong;

            // Вводим поле последняя свеча
            Candle candle = candles[candles.Count - 1];


            // Проверяем, есть ли открытые позиции и переносим ли Stop в безубыток ?
            if (positions.Count > 0 && _trailStop.ValueString == "Yes")
            {
                // Проверяем открытые позиций на возможность подтягивания стопа

                for (int i = 0; i < positions.Count; i++)
                {
                    // Если уровень закрытия свечи > уровня открытия позиции + stop, переносим StopLoss в безубыток
                    if (positions[i].EntryPrice * 2 - positions[i].StopOrderRedLine < candle.Close
                        && positions[i].StopOrderRedLine < positions[i].EntryPrice && positions[i].SignalTypeOpen != "1")
                    {
                        // переносим стоп-лосс в безубыток
                        _tab.CloseAtStop(positions[i], positions[i].EntryPrice, positions[i].EntryPrice - 100 * positions[i].PriceStepCost);
                    }
                }
                return;
            }

            // Был ли уровень close последней свечи > 1/2 длины свечи ?
            // Превышал ли объем свечи средний уровень _averageVolume предыдущих свечей в "_koefVolume" раз ?
            if (candle.Close < (candle.High + candle.Low) / 2
                || candle.Volume < _averageVolume * _koefVolume.ValueDecimal)
            {
                return;
            }

            // Рассчитываем размер стопа в пунктах как разницу между уровнем закрытия и low свечи
            _punkt = (int)((candle.Close - candle.Low) / _tab.Securiti.PriceStep);

            // Исключаем слишком короткие стопы
            if (_punkt < 5)
            {
                return;
            }
            // считаем размер стопа в деньгах по последней свече
            decimal amountStop = _punkt * _tab.Securiti.PriceStepCost;
            
            // размер риска в деньгах как % риска от депо
            decimal amountRisk = _tab.Portfolio.ValueBegin * _risk.ValueDecimal / 100;
            
            // число лотов для выставления заявки
            decimal volume = amountRisk / amountStop;

            decimal go = 10000;

            if (_tab.Securiti.Go > 1)
            {
                go = _tab.Securiti.Go;
            }

            // Расчитываем ограничение кол-ва лотов сходя из депозита
            decimal maxLot = _tab.Portfolio.ValueBegin / go;

            if (volume < maxLot)
            {
                // назначаем уровень стопа по min последней свечи
                _lowCandle = candle.Low;
                // выставляем заявку long по рынку

                switch (_dividePos.ValueString)
                {
                    case "No":
                        _tab.BuyAtMarket(volume);
                        break;

                    case "Yes":
                        _tab.BuyAtMarket(volume/2, "1");
                        _tab.BuyAtMarket(volume/2, "2");
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// После упешно исполненной заявки ...
        /// </summary>
        /// <param name="pos"></param>
        private void _tab_PositionOpeningSuccesEvent(Position pos)
        {
            if (_tab != null) 
            {

                _tab.CloseAtStop(pos, _lowCandle, _lowCandle - 100 * _tab.Securiti.PriceStep);

                // Размер тейк-профита
                decimal priceTake1 = pos.EntryPrice + _punkt * _tab.Securiti.PriceStepCost;
                decimal priceTake2 = pos.EntryPrice + _punkt * _profitKoef.ValueDecimal * _tab.Securiti.PriceStepCost;

                switch (pos.SignalTypeOpen)
                {
                    case "1":
                        _tab.CloseAtProfit(pos, priceTake1, priceTake1);
                        break;

                    case "2":
                        _tab.CloseAtProfit(pos, priceTake2, priceTake2);
                        break;

                    default:
                        _tab.CloseAtProfit(pos, priceTake2, priceTake2);
                        break;
                }
            }
        }

        /// <summary>
        /// После закрытия позиции ...
        /// </summary>
        /// <param name="pos"></param>
        private void _tab_PositionClosingSuccesEvent(Position pos)
        {
            SaveCSV(pos);
        }

        /// <summary>
        /// Запись результата закрытия позиции в журнал сделок : "Engine\trades.csv"
        /// </summary>
        /// <param name="pos"></param>
        private void SaveCSV(Position pos)
        {

            if (!File.Exists(@"Engine\trades.csv"))
            {
                string header = ";Позиция;Символ;Лоты;Изменение/Максимум Лотов;Исполнение входа;Сигнал входа;Бар входа;Дата входа;Время входа;" +
                            "Цена входа;Комиссия входа;Исполнение выхода;Сигнал выхода;Бар выхода;Дата выхода;Время выхода;Цена выхода;" +
                            "Комиссия выхода;Средневзвешенная цена входа;П/У;П/У сделки;П/У с одного лота;Зафиксированная П/У;Открытая П/У;" +
                            "Продолж. (баров);Доход/Бар;Общий П/У;% изменения;MAE;MAE %;MFE;MFE %";


                using (StreamWriter writer = new StreamWriter(@"Engine\trades.csv", false))
                {
                    writer.WriteLine(header);

                    writer.Close();
                }

            }

            using (StreamWriter writer = new StreamWriter(@"Engine\trades.csv", true))
            {
                string str = ";;;" + pos.Lots;                          // Кол-во лотов в поз.4

                str += ";;;;;" + pos.TimeOpen.ToShortDateString();      // Дата открытия позиции в поз.8

                str += ";" + pos.TimeOpen.TimeOfDay;                    // Время открытия позиции в поз.9

                str += ";;;;;;" + pos.TimeClose.ToShortDateString();    // Дата закрытия позиции в поз.15

                str += ";" + pos.TimeClose.TimeOfDay;                   // Время закрытия позиции в поз.16

                str += ";;;;;;;" + pos.ProfitPortfolioPunkt;            // Прибыль/убыток сделки в поз.23

                str += ";;;;;;;;;";

                writer.WriteLine(str);

                writer.Close();
            }

        }

        public override string GetNameStrategyType()
        {
            return nameof(MyVolReverseRobot);
        }

        public override void ShowIndividualSettingsDialog()
        {

        }

        #endregion
    }
}