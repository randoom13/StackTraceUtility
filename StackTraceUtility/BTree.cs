using System;
using System.Collections.Generic;

namespace StackTraceUtility
{
    public class BTree<TKey, TValue>
    {
        private readonly IComparer<TKey> _comparer;
        private readonly int _t;  // Minimum degree (defines the range for the number of keys)
        private BTreeNode<TKey, TValue> _root;

        // RemoveAll method that removes all key-value pairs matching the predicate
        public int RemoveAll(Predicate<KeyValuePair<TKey, TValue>> match)
        {
            int removedCount = 0;
            removedCount += RemoveAllFromNode(_root, match);
            return removedCount;
        }

        // Helper method to recursively remove matching key-value pairs from nodes
        private int RemoveAllFromNode(BTreeNode<TKey, TValue> node, Predicate<KeyValuePair<TKey, TValue>> match)
        {
            int removedCount = 0;

            // Remove from the current node
            for (int i = node.Keys.Count - 1; i >= 0; i--)
            {
                var pair = new KeyValuePair<TKey, TValue>(node.Keys[i], node.Values[i]);
                if (match(pair))
                {
                    node.Keys.RemoveAt(i);
                    node.Values.RemoveAt(i);
                    removedCount++;
                }
            }

            // Recursively remove from child nodes if not a leaf
            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    removedCount += RemoveAllFromNode(child, match);
                }
            }

            return removedCount;
        }

        // Constructor that accepts an IComparer<TKey> to define the comparison strategy
        public BTree(int t, IComparer<TKey> comparer = null)
        {
            _t = t;
            _comparer = comparer ?? Comparer<TKey>.Default; // Default comparer if none provided
            _root = new BTreeNode<TKey, TValue>(_t, true);
        }

        // Insert a key-value pair into the B-tree
        public void Insert(TKey key, TValue value)
        {
            // If the root node is full, split it
            if (_root.Keys.Count == 2 * _t - 1)
            {
                var newRoot = new BTreeNode<TKey, TValue>(_t, false);
                newRoot.Children.Add(_root);  // Move the old root to a new child
                SplitChild(newRoot, 0);  // Split the old root

                _root = newRoot;  // Set the new root
            }

            InsertNonFull(_root, key, value);
        }

        // Split a child node
        private void SplitChild(BTreeNode<TKey, TValue> parent, int index)
        {
            var fullNode = parent.Children[index];
            var newNode = new BTreeNode<TKey, TValue>(_t, fullNode.IsLeaf);
            parent.Keys.Insert(index, fullNode.Keys[_t - 1]);
            parent.Values.Insert(index, fullNode.Values[_t - 1]);
            parent.Children.Insert(index + 1, newNode);

            // Move the last t-1 keys and children from the full node to the new node
            newNode.Keys.AddRange(fullNode.Keys.GetRange(_t, _t - 1));
            newNode.Values.AddRange(fullNode.Values.GetRange(_t, _t - 1));
            fullNode.Keys.RemoveRange(_t - 1, _t);
            fullNode.Values.RemoveRange(_t - 1, _t);

            if (!fullNode.IsLeaf)
            {
                newNode.Children.AddRange(fullNode.Children.GetRange(_t, _t));
                fullNode.Children.RemoveRange(_t, _t);
            }
        }

        // Insert into a node that is not full
        private void InsertNonFull(BTreeNode<TKey, TValue> node, TKey key, TValue value)
        {
            int i = node.Keys.Count - 1;

            // If the node is a leaf, insert the key-value pair
            if (node.IsLeaf)
            {
                while (i >= 0 && _comparer.Compare(key, node.Keys[i]) < 0)
                {
                    i--;
                }
                node.Keys.Insert(i + 1, key);
                node.Values.Insert(i + 1, value);
            }
            else
            {
                // If the node is not a leaf, find the correct child to insert into
                while (i >= 0 && _comparer.Compare(key, node.Keys[i]) < 0)
                {
                    i--;
                }
                i++;

                // If the child node is full, split it
                if (node.Children[i].Keys.Count == 2 * _t - 1)
                {
                    SplitChild(node, i);

                    if (_comparer.Compare(key, node.Keys[i]) > 0)
                    {
                        i++;
                    }
                }

                // Recur on the child node
                InsertNonFull(node.Children[i], key, value);
            }
        }

        // Find a value for a given key
        public TValue Find(TKey key)
        {
            return Find(_root, key);
        }

        // Search for a key in a given node
        private TValue Find(BTreeNode<TKey, TValue> node, TKey key)
        {
            int i = 0;
            while (i < node.Keys.Count && _comparer.Compare(key, node.Keys[i]) > 0)
            {
                i++;
            }

            if (i < node.Keys.Count && _comparer.Compare(key, node.Keys[i]) == 0)
            {
                return node.Values[i];  // Key found
            }

            if (node.IsLeaf)
            {
                return default(TValue);  // Key not found, return default
            }

            return Find(node.Children[i], key);  // Recur on the child node
        }

        // Remove a key from the tree
        public bool Remove(TKey key)
        {
            return Remove(_root, key);
        }

        private bool Remove(BTreeNode<TKey, TValue> node, TKey key)
        {
            int idx = FindKeyIndex(node, key);
            if (idx < node.Keys.Count && _comparer.Compare(key, node.Keys[idx]) == 0)
            {
                // Key is in the current node
                if (node.IsLeaf)
                {
                    // Remove from leaf node directly
                    node.Keys.RemoveAt(idx);
                    node.Values.RemoveAt(idx);
                }
                else
                {
                    // If the key is in an internal node, we need to find a suitable replacement
                    TKey predKey = GetPredecessor(node, idx);
                    node.Keys[idx] = predKey;
                    node.Values[idx] = Find(predKey);
                    Remove(node.Children[idx], predKey);
                }
            }
            else if (!node.IsLeaf)
            {
                bool result = Remove(node.Children[idx], key);
                if (node.Children[idx].Keys.Count < _t - 1)
                {
                    HandleUnderflow(node, idx);
                }
                return result;
            }
            return false;
        }

        // Find the index of the key in the node
        private int FindKeyIndex(BTreeNode<TKey, TValue> node, TKey key)
        {
            int idx = 0;
            while (idx < node.Keys.Count && _comparer.Compare(key, node.Keys[idx]) > 0)
            {
                idx++;
            }
            return idx;
        }

        // Get the predecessor of a key in an internal node
        private TKey GetPredecessor(BTreeNode<TKey, TValue> node, int idx)
        {
            BTreeNode<TKey, TValue> current = node.Children[idx];
            while (!current.IsLeaf)
            {
                current = current.Children[current.Children.Count - 1];
            }
            return current.Keys[current.Keys.Count - 1];
        }

        // Handle node underflow by merging or redistributing keys
        private void HandleUnderflow(BTreeNode<TKey, TValue> parent, int idx)
        {
            var child = parent.Children[idx];
            if (idx > 0 && parent.Children[idx - 1].Keys.Count >= _t)
            {
                // Borrow a key from the left sibling
                BorrowFromLeftSibling(parent, idx);
            }
            else if (idx < parent.Children.Count - 1 && parent.Children[idx + 1].Keys.Count >= _t)
            {
                // Borrow a key from the right sibling
                BorrowFromRightSibling(parent, idx);
            }
            else
            {
                // Merge with the sibling
                if (idx < parent.Children.Count - 1)
                {
                    MergeWithRightSibling(parent, idx);
                }
                else
                {
                    MergeWithLeftSibling(parent, idx);
                }
            }
        }

        // Borrow a key from the left sibling
        private void BorrowFromLeftSibling(BTreeNode<TKey, TValue> parent, int idx)
        {
            var child = parent.Children[idx];
            var leftSibling = parent.Children[idx - 1];

            child.Keys.Insert(0, parent.Keys[idx - 1]);
            child.Values.Insert(0, parent.Values[idx - 1]);

            parent.Keys[idx - 1] = leftSibling.Keys[leftSibling.Keys.Count - 1];
            parent.Values[idx - 1] = leftSibling.Values[leftSibling.Values.Count - 1];

            leftSibling.Keys.RemoveAt(leftSibling.Keys.Count - 1);
            leftSibling.Values.RemoveAt(leftSibling.Values.Count - 1);

            if (!leftSibling.IsLeaf)
            {
                child.Children.Insert(0, leftSibling.Children[leftSibling.Children.Count - 1]);
                leftSibling.Children.RemoveAt(leftSibling.Children.Count - 1);
            }
        }

        // Borrow a key from the right sibling
        private void BorrowFromRightSibling(BTreeNode<TKey, TValue> parent, int idx)
        {
            var child = parent.Children[idx];
            var rightSibling = parent.Children[idx + 1];

            child.Keys.Add(parent.Keys[idx]);
            child.Values.Add(parent.Values[idx]);

            parent.Keys[idx] = rightSibling.Keys[0];
            parent.Values[idx] = rightSibling.Values[0];

            rightSibling.Keys.RemoveAt(0);
            rightSibling.Values.RemoveAt(0);

            if (!rightSibling.IsLeaf)
            {
                child.Children.Add(rightSibling.Children[0]);
                rightSibling.Children.RemoveAt(0);
            }
        }

        // Merge two siblings
        private void MergeWithLeftSibling(BTreeNode<TKey, TValue> parent, int idx)
        {
            var leftSibling = parent.Children[idx - 1];
            var child = parent.Children[idx];

            leftSibling.Keys.Add(parent.Keys[idx - 1]);
            leftSibling.Values.Add(parent.Values[idx - 1]);

            leftSibling.Keys.AddRange(child.Keys);
            leftSibling.Values.AddRange(child.Values);

            if (!child.IsLeaf)
            {
                leftSibling.Children.AddRange(child.Children);
            }

            parent.Children.RemoveAt(idx);
            parent.Keys.RemoveAt(idx - 1);
            parent.Values.RemoveAt(idx - 1);
        }

        // Merge two siblings
        private void MergeWithRightSibling(BTreeNode<TKey, TValue> parent, int idx)
        {
            var child = parent.Children[idx];
            var rightSibling = parent.Children[idx + 1];

            child.Keys.Add(parent.Keys[idx]);
            child.Values.Add(parent.Values[idx]);

            child.Keys.AddRange(rightSibling.Keys);
            child.Values.AddRange(rightSibling.Values);

            if (!rightSibling.IsLeaf)
            {
                child.Children.AddRange(rightSibling.Children);
            }

            parent.Children.RemoveAt(idx + 1);
            parent.Keys.RemoveAt(idx);
            parent.Values.RemoveAt(idx);
        }

        // For debugging purposes, display the B-tree structure
        public void Display()
        {
            Display(_root, 0);
        }

        private void Display(BTreeNode<TKey, TValue> node, int level)
        {
            Console.WriteLine(new string(' ', level * 2) + string.Join(", ", node.Keys));
            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    Display(child, level + 1);
                }
            }
        }
    }

    public class BTreeNode<TKey, TValue>
    {
        public List<TKey> Keys { get; set; }
        public List<TValue> Values { get; set; }
        public List<BTreeNode<TKey, TValue>> Children { get; set; }
        public bool IsLeaf { get; set; }

        public BTreeNode(int t, bool isLeaf)
        {
            Keys = new List<TKey>();
            Values = new List<TValue>();
            Children = new List<BTreeNode<TKey, TValue>>();
            IsLeaf = isLeaf;
        }
    }

}
