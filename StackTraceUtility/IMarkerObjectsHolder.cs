using System;
using System.Runtime.CompilerServices;

namespace StackTraceUtility
{
    public interface IMarkerObjectsHolderOwner
    {
        string Tag { get; }
        void Clean();
        string Id { get; }
    }

    public interface IMarkerObjectsHolder : IDisposable
    {
        MarkerObjectInfo GetInfo(IMarkerObjectsHolderOwner owner, object target, MarkerObjectInfo parentInfo = null);
        bool TryGetInfo(object target, out MarkerObjectInfo markerInfo);
        void Clean();
    }

    public sealed class SimpleObjectsHolder : IMarkerObjectsHolder
    {
        private ConditionalWeakTable<object, MarkerObjectInfo> _cachedInfoByObj = new ConditionalWeakTable<object, MarkerObjectInfo>();

        public MarkerObjectInfo GetInfo(IMarkerObjectsHolderOwner owner, object target, MarkerObjectInfo parent = null)
        {
            if (owner == null)
                throw new ArgumentException(nameof(owner));

            if (target == null)
                throw new ArgumentException(nameof(target));

            MarkerObjectInfo result;
            if (!_cachedInfoByObj.TryGetValue(target, out result))
            {
                result = new MarkerObjectInfo(owner.Id, parent, owner.Tag);
                _cachedInfoByObj.Add(target, result);
            }
            return result;
        }

        public bool TryGetInfo(object target, out MarkerObjectInfo info)
        {
            if (target == null)
                throw new ArgumentException(nameof(target));

            return _cachedInfoByObj.TryGetValue(target, out info);
        }

        public void Clean() { }

        public void Dispose()
        {
            _cachedInfoByObj = new ConditionalWeakTable<object, MarkerObjectInfo>();
        }
    }

    public class BaseBTreeObjectsHolder : IMarkerObjectsHolder
    {
        protected WeakKeyDictionary<MarkerObjectInfo> _cachedInfoByObj;
        internal BaseBTreeObjectsHolder(int keysRange)
        {
            _cachedInfoByObj = new WeakKeyDictionary<MarkerObjectInfo>(keysRange);
        }

        public virtual MarkerObjectInfo GetInfo(IMarkerObjectsHolderOwner owner, object target, MarkerObjectInfo parentInfo = null)
        {
            if (owner == null)
                throw new ArgumentException(nameof(owner));

            if (target == null)
                throw new ArgumentException(nameof(target));

            MarkerObjectInfo result = null;
            if (!_cachedInfoByObj.TryGetValue(false, target, out result))
            {
                result = new MarkerObjectInfo(owner.Id, parentInfo, owner.Tag);
                _cachedInfoByObj.Add(target, result);

            }
            return result;
        }

        public bool ApplyCleanupOnBuild { get; set; } = false;

        public bool TryGetInfo(object target, out MarkerObjectInfo info)
        {
            if (target == null)
                throw new ArgumentException(nameof(target));

            return _cachedInfoByObj.TryGetValue(ApplyCleanupOnBuild, target, out info);
        }

        public void Clean()
        {
            _cachedInfoByObj.CleanupDeadKeys();
        }

        public virtual void Dispose()
        {
            _cachedInfoByObj = new WeakKeyDictionary<MarkerObjectInfo>(1);
        }
    }

    public sealed class BTreeObjectsHolder : BaseBTreeObjectsHolder
    {
        public BTreeObjectsHolder(int keysRange) : base(keysRange)
        {
        }

        public BTreeObjectsHolder() : base(2)
        {
        }
    }

    public sealed class AdvancedBTreeObjectsHolder : BaseBTreeObjectsHolder
    {
        public AdvancedBTreeObjectsHolder(int keysRange) : base(keysRange)
        {
        }
        public AdvancedBTreeObjectsHolder() : base(2)
        {
        }

        private class Notifier
        {
            private readonly WeakReference _cleanable;
            public Notifier(WeakReference cleanable)
            {
                if (cleanable == null)
                    throw new ArgumentException(nameof(cleanable));

                _cleanable = cleanable;
            }

            ~Notifier()
            {
                var cleanable = _cleanable.Target as IMarkerObjectsHolderOwner;
                try
                {
                    cleanable?.Clean();
                }
                catch (Exception ex)
                {
                    var t = ex;
                }
            }
        }
        private ConditionalWeakTable<object, Notifier> _notifierByObj = new ConditionalWeakTable<object, Notifier>();

        public override MarkerObjectInfo GetInfo(IMarkerObjectsHolderOwner owner, object target, MarkerObjectInfo parentInfo = null)
        {
            MarkerObjectInfo result = base.GetInfo(owner, target, parentInfo);
            Notifier notifier;
            if (!_notifierByObj.TryGetValue(target, out notifier))
            {

                if (_notifierCleaner.Target == null)
                    _notifierCleaner.Target = owner;

                notifier = new Notifier(_notifierCleaner);
                _notifierByObj.Add(target, notifier);
            }
            return result;
        }
        // Deal with using WeakReference in Notifier in release mode
        private WeakReference _notifierCleaner = new WeakReference(null);

        public override void Dispose()
        {
            _notifierCleaner.Target = null;
            _notifierByObj = new ConditionalWeakTable<object, Notifier>();
            _cachedInfoByObj = new WeakKeyDictionary<MarkerObjectInfo>(1);
        }
    }
}
