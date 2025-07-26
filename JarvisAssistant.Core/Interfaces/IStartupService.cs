namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for startup service management.
    /// </summary>
    public interface IStartupService
    {
        /// <summary>
        /// Gets a value indicating whether this is the first run of the application.
        /// </summary>
        bool IsFirstRun { get; }

        /// <summary>
        /// Initializes the application asynchronously.
        /// </summary>
        /// <param name="progress">Progress reporter for startup operations.</param>
        /// <returns>A task representing the startup result.</returns>
        Task<StartupResult> InitializeAsync(IProgress<StartupProgress>? progress = null);

        /// <summary>
        /// Registers a startup task to be executed during initialization.
        /// </summary>
        /// <param name="task">The startup task to register.</param>
        void RegisterStartupTask(IStartupTask task);

        /// <summary>
        /// Completes the first run setup.
        /// </summary>
        /// <returns>A task representing the completion operation.</returns>
        Task CompleteFirstRunAsync();
    }

    /// <summary>
    /// Interface for tasks that should run during application startup.
    /// </summary>
    public interface IStartupTask
    {
        /// <summary>
        /// Executes the startup task.
        /// </summary>
        /// <returns>A task representing the execution.</returns>
        Task ExecuteAsync();
    }

    /// <summary>
    /// Represents the progress of startup operations.
    /// </summary>
    public class StartupProgress
    {
        public string Message { get; }
        public int Current { get; }
        public int Total { get; }
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;

        public StartupProgress(string message, int current, int total)
        {
            Message = message;
            Current = current;
            Total = total;
        }
    }

    /// <summary>
    /// Represents the result of startup operations.
    /// </summary>
    public class StartupResult
    {
        public bool IsSuccess { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsFirstRun { get; set; }
        public Exception? Error { get; set; }
    }
}
