using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CommunityToolkit.Mvvm.Input;

public sealed class AsyncRelayCommand : ICommand, INotifyPropertyChanged
{
    internal static readonly PropertyChangedEventArgs ExecutionTaskChangedEventArgs = new(nameof(ExecutionTask));

    internal static readonly PropertyChangedEventArgs CanBeCanceledChangedEventArgs = new(nameof(CanBeCanceled));

    internal static readonly PropertyChangedEventArgs IsCancellationRequestedChangedEventArgs = new(nameof(IsCancellationRequested));

    internal static readonly PropertyChangedEventArgs IsRunningChangedEventArgs = new(nameof(IsRunning));

    private readonly Func<Task>? execute;

    private readonly Func<CancellationToken, Task>? cancelableExecute;

    private readonly Func<bool>? canExecute;

    private readonly bool allowConcurrentExecutions;

    private CancellationTokenSource? cancellationTokenSource;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CanExecuteChanged;

    public AsyncRelayCommand(Func<Task> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
        this.allowConcurrentExecutions = true;
    }

    public AsyncRelayCommand(Func<Task> execute, bool allowConcurrentExecutions)
    {
        ArgumentNullException.ThrowIfNull(execute);

        this.execute = execute;
        this.allowConcurrentExecutions = allowConcurrentExecutions;
    }

    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);

        this.cancelableExecute = cancelableExecute;
        this.allowConcurrentExecutions = true;
    }

    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, bool allowConcurrentExecutions)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);

        this.cancelableExecute = cancelableExecute;
        this.allowConcurrentExecutions = allowConcurrentExecutions;
    }

    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
        this.allowConcurrentExecutions = true;
    }

    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute, bool allowConcurrentExecutions)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
        this.allowConcurrentExecutions = allowConcurrentExecutions;
    }

    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, Func<bool> canExecute)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.cancelableExecute = cancelableExecute;
        this.canExecute = canExecute;
        this.allowConcurrentExecutions = true;
    }

    public AsyncRelayCommand(Func<CancellationToken, Task> cancelableExecute, Func<bool> canExecute, bool allowConcurrentExecutions)
    {
        ArgumentNullException.ThrowIfNull(cancelableExecute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.cancelableExecute = cancelableExecute;
        this.canExecute = canExecute;
        this.allowConcurrentExecutions = allowConcurrentExecutions;
    }

    private Task? executionTask;

    public Task? ExecutionTask
    {
        get => this.executionTask;
        private set
        {
            if (ReferenceEquals(this.executionTask, value))
            {
                return;
            }

            this.executionTask = value;

            PropertyChanged?.Invoke(this, ExecutionTaskChangedEventArgs);
            PropertyChanged?.Invoke(this, IsRunningChangedEventArgs);
            PropertyChanged?.Invoke(this, CanBeCanceledChangedEventArgs);

            if (value?.IsCompleted ?? true)
            {
                return;
            }

            static async void MonitorTask(AsyncRelayCommand @this, Task task)
            {
                try
                {
                    await task;
                }
                catch
                {
                }

                if (ReferenceEquals(@this.executionTask, task))
                {
                    @this.PropertyChanged?.Invoke(@this, ExecutionTaskChangedEventArgs);
                    @this.PropertyChanged?.Invoke(@this, IsRunningChangedEventArgs);
                    @this.PropertyChanged?.Invoke(@this, CanBeCanceledChangedEventArgs);
                }
            }

            MonitorTask(this, value!);
        }
    }

    public bool CanBeCanceled => this.cancelableExecute is not null && IsRunning;

    public bool IsCancellationRequested => this.cancellationTokenSource?.IsCancellationRequested == true;

    public bool IsRunning => ExecutionTask?.IsCompleted == false;

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute(object? parameter)
    {
        bool canExecute = this.canExecute?.Invoke() != false;

        return canExecute && (this.allowConcurrentExecutions || ExecutionTask?.IsCompleted != false);
    }

    public void Execute(object? parameter)
    {
        _ = ExecuteAsync(parameter);
    }

    public Task ExecuteAsync(object? parameter)
    {
        if (CanExecute(parameter))
        {
            // Non cancelable command delegate
            if (this.execute is not null)
            {
                return ExecutionTask = this.execute();
            }

            // Cancel the previous operation, if one is pending
            this.cancellationTokenSource?.Cancel();

            CancellationTokenSource cancellationTokenSource = this.cancellationTokenSource = new();

            PropertyChanged?.Invoke(this, IsCancellationRequestedChangedEventArgs);

            // Invoke the cancelable command delegate with a new linked token
            return ExecutionTask = this.cancelableExecute!(cancellationTokenSource.Token);
        }

        return Task.CompletedTask;
    }

    public void Cancel()
    {
        this.cancellationTokenSource?.Cancel();

        PropertyChanged?.Invoke(this, IsCancellationRequestedChangedEventArgs);
        PropertyChanged?.Invoke(this, CanBeCanceledChangedEventArgs);
    }
}