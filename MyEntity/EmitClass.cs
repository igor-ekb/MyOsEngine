using OsEngine.Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.MyEntity
{

    public class EmitClass : BaseVM
    {
        public EmitClass(string str)
        {
            ClassEmit = str;
        }

        public string ClassEmit
        {
            get => _classEmit;

            set
            {
                _classEmit = value;
                OnPropertyChanged(nameof(ClassEmit));
            }
        }
        private string _classEmit;

        public string ClassName
        {
            get => _className;

            set
            {
                _className = value;
                OnPropertyChanged(nameof(ClassName));
            }
        }
        private string _className = "";
    }
}
