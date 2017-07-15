using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PowerShellModule
{
    /// <summary>
    /// Simple message pump to make sure PowerShell calls are happening on the main thread
    /// </summary>
    public class MessagePump
    {
        private readonly ConcurrentQueue<Action> _parentThreadOperations = new ConcurrentQueue<Action>();

        public void Enqueue(Action action)
        {
            _parentThreadOperations.Enqueue(action);
        }

        public void LoopUntil(Task work)
        {
            Action nextAction;

            while (!work.IsCompleted)
            {
                while (_parentThreadOperations.TryDequeue(out nextAction))
                {
                    nextAction();
                }

                Thread.Sleep(100);
            }

            while (_parentThreadOperations.TryDequeue(out nextAction))
            {
                nextAction();
            }
        }
    }
}