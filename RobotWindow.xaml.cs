using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using OsEngine.Market;
using System.Threading;
using OsEngine.ViewModels;

namespace OsEngine
{
    /// <summary>
    /// Interaction logic for RobotWindow.xaml
    /// </summary>
    public partial class RobotWindow : MetroWindow
    {
        public RobotWindow()
        {
            Process ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            Dispatcher = Dispatcher.CurrentDispatcher;

            InitializeComponent();

            MainWindow.ProccesIsWorked = true;

            ServerMaster.ActivateLogging();

            this.Closed += RobotWindow_Closed;

            windowVM = new RobotWindowVM();                     // Урок 3-32  01:10:46

            DataContext = windowVM;
        }

        RobotWindowVM windowVM;


        private void RobotWindow_Closed(object sender, EventArgs e)
        {
            Save();

            MainWindow.ProccesIsWorked = false;

            Thread.Sleep(10000);

            Process.GetCurrentProcess().Kill();
        }


        /// <summary>
        /// Урок 3-32 01:09:48
        /// </summary>
        private void Save()
        {
            foreach (MyRobotVM robotVM in windowVM.Robots)                     // Урок 3-32  01:11:07
            {
                robotVM.Save();                                 // Save() из MyRobotVM.cs
            }
        }

        public static Dispatcher Dispatcher;

        /// <summary>
        /// Разрешение на перетаскивания окна при помощи нажатия Left Button мышки
        /// </summary>
        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

    }
}
