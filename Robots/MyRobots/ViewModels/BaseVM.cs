﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.MyRobots.ViewModels
{
    public class BaseVM : INotifyPropertyChanged
    {
        public void OnPropertyChanged(string prop)                      // using System.Runtime.ComponentModel;
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        #region Events =====================================================

        public event PropertyChangedEventHandler PropertyChanged;       // using System.ComponentModel;

        #endregion
    }
}
