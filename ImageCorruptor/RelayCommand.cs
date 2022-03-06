using System;
using System.Windows.Input;

namespace ImageCorruptor
{
    public class RelayCommand<T> : ICommand
    {
        private Action<T?> _execute;
        private Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute)
        {
            _execute = execute;
            _canExecute = null;
        }
        public RelayCommand(Action<T?> execute, Func<T?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute is null)
                return true;

            if (parameter == null && typeof(T).IsValueType)
                return _canExecute(default(T));

            if (parameter == null || parameter is T)
                return _canExecute((T?)parameter);

            return false;
        }

        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter) || _execute is null)
                return;

            if (parameter == null)
            {
                if (typeof(T).IsValueType)
                    _execute(default(T));
                else
                    _execute((T?)parameter);
            }
            else
            {
                _execute((T)parameter);
            }
        }
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action execute) : base((p) => execute()) { }
        public RelayCommand(Action execute, Func<bool> canExecute) : base((p) => execute(), (p) => canExecute()) { }
    }
}
