using System.Windows.Input;

namespace SklepMotoryzacyjny.Helpers
{
    /// <summary>
    /// Generyczna implementacja ICommand pozwalająca na typowany parametr komendy.
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            if (parameter is T t) return _canExecute(t);
            if (parameter == null) return _canExecute(default);
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T t)
                _execute(t);
            else if (parameter == null)
                _execute(default);
        }
    }
}
