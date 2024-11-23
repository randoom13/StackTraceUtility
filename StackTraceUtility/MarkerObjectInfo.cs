using System;

namespace StackTraceUtility
{
    public class MarkerObjectInfo
    {
        public const string TagMarkerFormat = "Marker_{0}_{1}";
        public const string MarkerFormat = "Marker_{0}";
        private Action<Action> _delegate = null;

        public bool HasDelegate { get; private set; } = false;
        internal Action<Action> Delegate
        {
            get => _delegate;
            set
            {
                HasDelegate = true;
                _delegate = value;
            }
        }

        public string Key { get; private set; }
        internal string MarkerText { get; private set; }

        internal MarkerObjectInfo(string key, MarkerObjectInfo parent, string tag)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException(nameof(key));

            Key = key;
            if (parent != null)
                key = $"{parent.Key}_{key}";

            MarkerText = string.IsNullOrEmpty(tag) ?
                string.Format(MarkerFormat, key) : string.Format(TagMarkerFormat, tag, key);
        }
    }
}
