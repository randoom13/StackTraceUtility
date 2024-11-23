using System;

namespace StackTraceUtility
{
    public class CacheItem
    {
        private readonly WeakReference _parentRef = new WeakReference(null);
        private readonly WeakReference _targetRef = new WeakReference(null);

        public object Parent => _parentRef.Target;
        public object Target => _targetRef.Target;

        public CacheItem(object parent, object target)
        {
            _parentRef.Target = parent;
            _targetRef.Target = target;
        }

        public CacheItem PresiousItem { get; set; }
    }

}
