using System.Threading;

/// <summary>
/// Abstract base class for creating manageable background threads in Unity.
/// Provides a standardized interface for starting, stopping, and monitoring thread execution.
/// Used primarily for network communication and other background tasks that shouldn't block the main Unity thread.
/// </summary>
public abstract class RunAbleThread
{
    #region Private Fields
    /// <summary>
    /// The background thread instance
    /// </summary>
    private readonly Thread _runnerThread;
    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new thread with the Run() method as the entry point.
    /// The thread is created but not started - call Start() to begin execution.
    /// </summary>
    protected RunAbleThread()
    {
        // Create thread instead of calling Run() directly to avoid blocking Unity's main thread
        _runnerThread = new Thread(Run);
    }
    #endregion

    #region Properties
    /// <summary>
    /// Indicates whether the thread is currently running.
    /// Set to false by Stop() to signal the thread to terminate gracefully.
    /// </summary>
    protected bool Running { get; private set; }
    #endregion

    #region Abstract Methods
    /// <summary>
    /// Main execution method that will run in the background thread.
    /// Implementations must ensure this method terminates in finite time and
    /// should regularly check the Running property to determine when to exit.
    /// </summary>
    protected abstract void Run();
    #endregion

    #region Public Methods
    /// <summary>
    /// Starts the background thread execution.
    /// Sets Running to true and begins executing the Run() method.
    /// </summary>
    public void Start()
    {
        Running = true;
        _runnerThread.Start();
    }

    /// <summary>
    /// Signals the thread to stop and waits for it to complete.
    /// Sets Running to false and blocks the main thread until the background thread terminates.
    /// This ensures proper cleanup before the main thread continues.
    /// </summary>
    public void Stop()
    {
        Running = false;
        // Block main thread until background thread finishes for proper cleanup
        _runnerThread.Join();
    }
    #endregion
}