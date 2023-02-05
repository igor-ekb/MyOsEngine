using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OsEngine.Views
{
    /// <summary>
    /// Контроль на ввод только цифр и наличие "." в введенном числе
    /// </summary>
    public class IntTextBox : TextBox
    {
        public IntTextBox()
        {
            this.PreviewTextInput += IntTextBlock_PreviewTextInput;

            this.TextChanged += IntTextBox_TextChanged;
        }

        /// <summary>
        /// перевод курсора в конец вводимого слова
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IntTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;

            tb.Select(tb.Text.Length, 0);   // Перевод каретки в конец слова
        }

        /// <summary>
        /// Фильтр на ввод только цифр
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IntTextBlock_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0))       // Проверяем, является ли вводимый символ цифрой

            {
                e.Handled = true;       // 
            }
        }
    }
}
