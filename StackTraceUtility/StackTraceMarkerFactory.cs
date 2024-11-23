using System;

namespace StackTraceUtility
{
    public class StackTraceMarkerFactory
    {
        private static StackTraceMarkerFactory _instance;
        private static IMarkerObjectsHolder _defaultHolder = new SimpleObjectsHolder();
        public static IMarkerObjectsHolder DefaultHolder 
        {
            get => _defaultHolder;
            set
            {
                if (value == null)
                    throw new ArgumentException(nameof(DefaultHolder));

                _defaultHolder = value;
            }
        }
        private static readonly object _lock = new object();
        private StackTraceMarkerFactory() { }
        public static StackTraceMarkerFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new StackTraceMarkerFactory();
                            _instance.StackTraceMarker = new StackTraceMarker(_defaultHolder);
                        }
                    }
                }
                return _instance;
            }
        }
        public IStackTraceMarker StackTraceMarker { get; private set; }
    }
}
