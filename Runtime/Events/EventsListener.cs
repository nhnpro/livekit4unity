using System;
using System.Collections.Generic;
// //using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LiveKitUnity.Runtime.Debugging;

namespace LiveKitUnity.Runtime.Events
{
    public class EventsListener
    {
        public async UniTask Dispose()
        {
            internalCallback = null;
        }

        public async UniTask<T1> WaitFor<T1>(Func<T1, bool> filter
            , TimeSpan duration, Func<bool> onTimeout = null) where T1 : ILiveKitEvent
        {
            try
            {
                var success = false;
                T1 result = default;
                ListenOneTime(t =>
                {
                    var matchedFilter = false;
                    var (t1, a) = matchedType<T1>(t);
                    if (a && (filter == null || filter(t1)))
                    {
                        result = t1;
                        matchedFilter = true;
                    }

                    success = matchedFilter;
                    return matchedFilter;
                });

                if (duration != TimeSpan.Zero)
                {
                    await UniTask.WaitUntil(() => success)
                        .Timeout(duration);
                }
                else
                {
                    await UniTask.WaitUntil(() => success);
                }

                return result;
            }
            catch (Exception ex)
            {
                this.LogException(ex);
                onTimeout?.Invoke();
            }

            return default;
        }

        public async UniTask CancelAll()
        {
            internalCallback = null;
        }

        private (T1, bool) matchedType<T1>(ILiveKitEvent p) where T1 : ILiveKitEvent
        {
            if (p is T1 t1)
            {
                return (t1, true);
            }

            return (default, false);
        }

        public void On<T1>(Action<T1> onEventCallback) where T1 : ILiveKitEvent
        {
            internalCallback += t =>
            {
                var (t1, success) = matchedType<T1>(t);
                if (success)
                {
                    onEventCallback(t1);
                }
            };
        }

        List<Func<ILiveKitEvent, bool>> oneTimeCallbacks = new();

        public void ListenOneTime(Func<ILiveKitEvent, bool> action)
        {
            oneTimeCallbacks.Add(action);
        }


        public void RegisterEmitter(EventEmitter eventEmitter)
        {
            eventEmitter.Listen(internalListener);
        }

        private Action<ILiveKitEvent> internalCallback;

        private void internalListener(ILiveKitEvent t)
        {
            internalCallback?.Invoke(t);
            for (var i = oneTimeCallbacks.Count - 1; i >= 0; i--)
            {
                if (oneTimeCallbacks[i].Invoke(t))
                {
                    oneTimeCallbacks.RemoveAt(i);
                }
            }
        }
    }
}