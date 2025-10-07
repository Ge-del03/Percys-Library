using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ComicReader.Commands
{
    public class RelayCommand : ICommand
    {
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action execute) : this(_ => execute()) { }
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        // Sobrecarga explícita para Action sin parámetro y canExecute opcional
        public RelayCommand(Action execute, Func<bool>? canExecute)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _execute = _ => execute();
            _canExecute = canExecute == null ? null : new Func<object?, bool>(_ => canExecute());
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { if (value != null) CommandManager.RequerySuggested += value; }
            remove { if (value != null) CommandManager.RequerySuggested -= value; }
        }
    }

    public class AsyncCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncCommand(Func<Task> executeAsync) : this(_ => executeAsync()) { }
        public AsyncCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }
        public AsyncCommand(Func<Task> executeAsync, Func<bool>? canExecute)
        {
            if (executeAsync == null) throw new ArgumentNullException(nameof(executeAsync));
            _executeAsync = _ => executeAsync();
            _canExecute = canExecute == null ? null : new Func<object?, bool>(_ => canExecute());
        }

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute == null || _canExecute(parameter));

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try { await _executeAsync(parameter); }
            finally { _isExecuting = false; RaiseCanExecuteChanged(); }
        }

        public event EventHandler? CanExecuteChanged
        {
            add { if (value != null) CommandManager.RequerySuggested += value; }
            remove { if (value != null) CommandManager.RequerySuggested -= value; }
        }
        private void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
