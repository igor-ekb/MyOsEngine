using OsEngine.Robots.FrontRunner.ViewModels;
using OsEngine.Robots.MyRobots.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OsEngine.Robots.FrontRunner.Views
{
    /// <summary>
    /// Interaction logic for FrontRunnerUi.xaml
    /// </summary>
    public partial class FrontRunnerUi : Window
    {
        public FrontRunnerUi(Models.FrontRunner bot)
        {
            InitializeComponent();
            
            vm = new VMFrontRunner(bot);

            DataContext = vm;

        }
        public VMFrontRunner vm;
    }
    
}
