using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OsEngine.Views
{
    public class DecimalTextBox : TextBox
    {
        public DecimalTextBox()
        {
            this.PreviewTextInput += DecimalTextBlock_PreviewTextInput;

            this.TextChanged += DecimalTextBox_TextChanged;
        }
        /// <summary>
        /// перевод курсора в конец вводимого слова
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DecimalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;

            tb.Select(tb.Text.Length, 0);   // Перевод каретки в конец слова
        }

        /// <summary>
        /// Фильтр на ввод только цифр и наличие .
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DecimalTextBlock_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0)        // Проверяем, является ли вводимый символ цифрой
                && !e.Text.Contains("."))       // и присутствует ли "." в введенной последовательности
            {
                e.Handled = true;       // 
            }
        }

    }
}
