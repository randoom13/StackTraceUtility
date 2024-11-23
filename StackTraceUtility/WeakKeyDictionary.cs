namespace StackTraceUtility
{
    public class WeakKeyDictionary<TValue>
    {
        internal WeakKeyDictionary(int keysRange) 
        {
            _bTree = new BTree<WeakReferenceKey, TValue>(keysRange);
        }
        private readonly BTree<WeakReferenceKey, TValue> _bTree;

        // Add a key-value pair to the dictionary
        public void Add(object key, TValue value)
        {
            var weakKey = new WeakReferenceKey(key);
            _bTree.Insert(weakKey, value);
        }

        // Find a value by its key
        public object Find(object key)
        {
            var weakKey = new WeakReferenceKey(key);
            return _bTree.Find(weakKey);
        }

        // Remove a key-value pair by key
        public bool Remove(object key)
        {
            var weakKey = new WeakReferenceKey(key);
            return _bTree.Remove(weakKey);
        }

        // Display all key-value pairs
        public void Display()
        {
            _bTree.Display();
        }

        public bool TryGetValue(bool applyCleanup, object key, out TValue value)
        {
            // Clean up any dead keys before performing the lookup
            if (applyCleanup)
              CleanupDeadKeys();

            var weakKey = new WeakReferenceKey(key);
            value = _bTree.Find(weakKey);
            return value != null;
        }

        public void CleanupDeadKeys()
        {
            _bTree.RemoveAll(item => item.Key.Target == null);
        }
    }
}
