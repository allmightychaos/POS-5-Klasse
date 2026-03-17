using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SchiffeGUI
{
    public class RelayCommand : ICommand
    {
        private Action<object> _action;

        public RelayCommand(Action<object> aktion)
        {
            _action = aktion;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _action(parameter);

        public event EventHandler CanExecuteChanged;
    }

}
