using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Utilities;
using System.Threading.Tasks;
using System.Threading;

namespace NGraphQL.Server.Utilities;

/// <summary>Utility class to schedule tasks and keep count of active tasks. </summary>
public class TaskRunner {
  // public instance members
  public int TaskCount => _taskCount;

  private volatile int _taskCount;

  public bool WaitForIdle(int timeoutSec = 5) {
    var end = AppTime.UtcNow.AddSeconds(timeoutSec);
    while (_taskCount > 0 && AppTime.UtcNow < end) {
      Thread.Sleep(20);
    }
    return _taskCount == 0;
  }

  /// <summary>Runs action asynchronously, maintains TaskCount while task is executing (+1/-1). </summary>
  /// <param name="action">The action to run.</param>
  public void Run(Action action) {
    IncTaskCountAndRun(action, runSync: false);
  }

  /// <summary>Runs action synchronously, maintains TaskCount while task is executing (+1/-1). </summary>
  /// <param name="action">The action to run.</param>
  public void RunSync(Action action) {
    IncTaskCountAndRun(action, runSync: true);
  }

  private void IncTaskCountAndRun(Action action, bool runSync) {
    // we increment synchonously, before scheduling task
    var oldValue = _taskCount;
    var newValue = Interlocked.Increment(ref _taskCount);
    if (runSync)
      RunAction(action);
    else
      Task.Run(() => RunAction(action));
  }

  // Private implementations
  private void RunAction(Action action) {
    try {
      action();
    } finally {
      Interlocked.Decrement(ref _taskCount);
    }
  }
}

