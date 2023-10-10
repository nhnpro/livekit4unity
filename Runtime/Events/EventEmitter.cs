using System;
using System.Collections.Generic;
//using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LiveKitUnity.Runtime.Debugging;

namespace LiveKitUnity.Runtime.Events
{
    public class EventEmitter
    {
        private Action<ILiveKitEvent> internalCallback;
        List<Func<ILiveKitEvent, bool>> oneTimeCallbacks = new();

        public async UniTask Dispose()
        {
            ClearCallback();
        }

        public void ClearCallback()
        {
            internalCallback = null;
        }

        public void Emit(ILiveKitEvent p)
        {
            internalCallback?.Invoke(p);
            for (var i = oneTimeCallbacks.Count - 1; i >= 0; i--)
            {
                if (oneTimeCallbacks[i].Invoke(p))
                {
                    oneTimeCallbacks.RemoveAt(i);
                }
            }
        }


        private (T1, bool) matchedType<T1>(ILiveKitEvent p) where T1 : ILiveKitEvent
        {
            if (p is T1 t1)
            {
                return (t1, true);
            }

            return (default, false);
        }

        //duration set to TimeSpan.Zero means no timeout
        public async UniTask<T1> WaitFor<T1>(Func<T1, bool> filter
            , TimeSpan duration, Func<bool> onTimeout = null) where T1 : ILiveKitEvent
        {
            try
            {
                T1 result = default;
                var success = false;
                ListenOneTime(t =>
                {
                    var matchedFilter = false;
                    var (t1, a) = matchedType<T1>(t);
                    if (a && (filter == null || filter(t1)))
                    {
                        matchedFilter = true;
                        result = t1;
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

        public void On<T1>(Action<T1> onCallback) where T1 : ILiveKitEvent
        {
            Listen(t =>
            {
                var (t1, success) = matchedType<T1>(t);
                if (success)
                {
                    onCallback(t1);
                }
            });
        }


        public void ListenOneTime(Func<ILiveKitEvent, bool> action)
        {
            oneTimeCallbacks.Add(action);
        }

        public void Listen(Action<ILiveKitEvent> action)
        {
            internalCallback += action;
        }

        public EventsListener CreateListener(bool synchronized)
        {
            var listener = new EventsListener();
            listener.RegisterEmitter(this);
            return listener;
        }
    }
}