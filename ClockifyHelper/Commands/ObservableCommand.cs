using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClockifyHelper.Commands
{
    public class ObservableCommand<T> : ICommand where T : INotifyPropertyChanged
    {
        Predicate<object> predicate;
        Action<object> execute;

        public ObservableCommand(T model, Action<object> execute, Predicate<object> canExecute)
        {
            model.PropertyChanged += ModelChanged;
            this.execute = execute;
            predicate = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return predicate(parameter);
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
