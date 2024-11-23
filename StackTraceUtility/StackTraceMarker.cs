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
    public interface IStackTraceMarker : IDisposable, IMarkerObjectsHolderOwner
    {
        string Tag { get; set; }
        IEnumerable<string> Register(object parent, IEnumerable<object> children);
        string Register(object target);
        void BuildMarker(object target, Action action);
        void TurnOn();
        void TurnOff();
        bool ApplyOffCache { get; set; }
        void ApplyCache();
#if NETCOREAPP3_1 || NET5_0 || NETFRAMEWORK4_5
       Task<IEnumerable<string>> RegisterAsync(object parent, IEnumerable<object> children);
       Task<string> RegisterAsync(object target);
       Task ApplyCacheAsync();
       Task BuildMarkerAsync(object target, Action action);
#else      
#endif
    }

    public partial class StackTraceMarker : IStackTraceMarker
    {
        private CacheItem _cache;
        private string _tag;
        private bool _disposed = false;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        private bool _canWork = true;
        private readonly IMarkerObjectsHolder _markerObjectsHolder;
        private int _counter = 0;


        public bool ApplyOffCache { get; set; }
        public bool IsOnline { get; private set; } = true;

        public string Id => (_counter++).ToString();
        public string Tag
        {
            get => _tag;
            set
            {
                _tag = TransformToValid(value);
            }
        }

        public void TurnOn()
        {
            if (_canWork)
                IsOnline = true;
        }

        public void TurnOff()
        {
            if (_canWork)
                IsOnline = false;
        }

        private static string TransformToValid(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
                return methodName;

            StringBuilder result = new StringBuilder();
            foreach (char ch in methodName)
            {
                if (ch == '$' || ch == '_' || char.IsLetterOrDigit(ch))
                {
                    result.Append(ch);
                }
            }
            return result.ToString();
        }

        public StackTraceMarker(IMarkerObjectsHolder holder, string tag = null)
        {
            if (holder == null)
                throw new ArgumentException(nameof(holder));

            _markerObjectsHolder = holder;
            Tag = tag;
        }

        private IEnumerable<string> RegisterInternal(object parentTarget, IEnumerable<object> targets)
        {
            if (!_canWork)
            {
                return Enumerable.Empty<string>();
            }
            if (!IsOnline)
            {
                if (ApplyOffCache)
                {
                    AddCache(parentTarget, targets);
                }
                return Enumerable.Empty<string>();
            }
            ApplyCacheInternal();
            MarkerObjectInfo parentInfo = null;
            if (parentTarget != null)
                _markerObjectsHolder.TryGetInfo(parentTarget, out parentInfo);

            return targets.Select(target => _markerObjectsHolder.
                GetInfo(this, target, parentInfo).MarkerText).ToArray();
        }

        public IEnumerable<string> Register(object parentTarget, IEnumerable<object> targets)
        {
            if (!_canWork || !IsOnline)
            {
                return Enumerable.Empty<string>();
            }
            try
            {
                _semaphoreSlim.Wait();
                return RegisterInternal(parentTarget, targets);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void AddCache(object parent, params object[] targets)
        {
            foreach (object target in targets)
            {
                var newItem = new CacheItem(parent, target);
                newItem.PresiousItem = _cache;
                _cache = newItem;
            }
        }

        private string RegisterInternal(object target)
        {
                if (!_canWork)
                {
                    return string.Empty;
                }
                if (!IsOnline)
                {
                    if (ApplyOffCache)
                    {
                        AddCache(null, target);
                    }
                    return string.Empty;
                }
                ApplyCacheInternal();
                return _markerObjectsHolder.GetInfo(this, target).MarkerText;
        }

        public string Register(object target)
        {
            if (!_canWork)
            {
                return string.Empty;
            }
            try
            {
                _semaphoreSlim.Wait();
                return RegisterInternal(target);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public void ApplyCache()
        {
            if (ApplyOffCache && IsOnline && _canWork && (_cache?.Target != null
                || _cache?.PresiousItem != null))
            {
                try
                {
                    _semaphoreSlim.Wait();
                    ApplyCacheInternal();
                }
                finally 
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        private void ApplyCacheInternal()
        {
            if (!ApplyOffCache)
                return;

            var cache = _cache;
            _cache = null;
            while (cache != null) 
            {
                var parent = cache.Parent;
                var target = cache.Target;
                cache = cache.PresiousItem;
                if (target != null) 
                {
                    MarkerObjectInfo info = null;
                    if (parent != null)
                        _markerObjectsHolder.TryGetInfo(parent, out info);

                    _markerObjectsHolder.GetInfo(this, target, info);
                }
            }
        }

        public void BuildMarker(object target, Action action)
        {
            if (!IsOnline || !_canWork)
            {
                action();
                return;
            }
            ApplyCache();

            var marker = CreateUniqueMarker(target);
            if (marker == null)
                action();
            else
                marker.Invoke(action);
        }

        public void Clean()
        {
            if (!IsOnline || !_canWork)
                return;

            try
            {
                _semaphoreSlim.Wait();
                if (_canWork && IsOnline)
                    _markerObjectsHolder.Clean();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        private Action<Action> CreateUniqueMarker(object target)
        {
            MarkerObjectInfo info = null;
            Action<Action> cachedDelegate = null;
            if (!_markerObjectsHolder.TryGetInfo(target, out info))
                return cachedDelegate;

            if (info.HasDelegate)
                return info.Delegate;

            try
            {
                AssemblyName assemblyName =  new AssemblyName(DynamicMethodsInformation.AssemblyName);
#if NETCOREAPP3_1 || NET5_0
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, 
                    AssemblyBuilderAccess.Run);
#else
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
                    AssemblyBuilderAccess.Run);
#endif
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(DynamicMethodsInformation.ModuleName);

                TypeBuilder typeBuilder = moduleBuilder.DefineType(DynamicMethodsInformation.ClassName, TypeAttributes.Public);
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(info.MarkerText, MethodAttributes.Public | MethodAttributes.Static,
                    typeof(void), new Type[] { typeof(Action) });
                methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);

                ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                // Emit the IL to load the Action parameter (index 0)
                ilGenerator.Emit(OpCodes.Ldarg_0); // Load the Action parameter (index 0)
                ilGenerator.Emit(OpCodes.Callvirt, typeof(Action).GetMethod("Invoke")); // Invoke the Action.Invoke method

                ilGenerator.Emit(OpCodes.Ret); // Return.

                // Create the type (compile it).
                Type dynamicType = typeBuilder.CreateType();

                // Get the "info.MarkerText" method from the dynamically created type.
                MethodInfo customMethod = dynamicType.GetMethod(info.MarkerText);
                cachedDelegate = (Action<Action>)Delegate.CreateDelegate(typeof(Action<Action>), customMethod);
                cachedDelegate.Invoke(() => { });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to create usable delegate");
                System.Diagnostics.Debug.WriteLine(ex);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                cachedDelegate = null;
            }
            info.Delegate = cachedDelegate;
            return cachedDelegate;
        }
      

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _canWork = false;
            IsOnline = false;
            _cache = null;
            if (disposing)
            {
                try
                {
                    _markerObjectsHolder.Dispose();
                    _semaphoreSlim.Dispose();
                }
                catch (Exception)
                {
                }
            }
            _disposed = true;
        }

        ~StackTraceMarker()
        {
            // Finalizer calls Dispose with 'disposing' set to false
            Dispose(false);
        }
    }
}
