using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StackTraceUtility
{
    public partial class StackTraceMarker
    {
#if NETCOREAPP3_1 || NET5_0 || NETFRAMEWORK4_5
        public async Task<IEnumerable<string>> RegisterAsync(object parentTarget, IEnumerable<object> targets)
        {
            if (!_canWork || !IsOnline)
            {
                return Enumerable.Empty<string>();
            }
            try
            {
                await _semaphoreSlim.WaitAsync();
                return RegisterInternal(parentTarget, targets);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<string> RegisterAsync(object target)
        {
            if (!_canWork)
            {
                return string.Empty;
            }
            try
            {
                await _semaphoreSlim.WaitAsync();
                return RegisterInternal(target);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task ApplyCacheAsync()
        {
            if (ApplyOffCache && IsOnline && _canWork && (_cache?.Target != null
                || _cache?.PresiousItem != null))
            {
                try
                {
                    await _semaphoreSlim.WaitAsync();
                    ApplyCacheInternal();
                }
                finally 
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        public async Task BuildMarkerAsync(object target, Action action)
        {
            if (!IsOnline || !_canWork)
            {
                action();
                return;
            }
            await ApplyCacheAsync();

            var marker = CreateUniqueMarker(target);
            if (marker == null)
                action();
            else
                marker.Invoke(action);
        }
#else
#endif
    }
}
