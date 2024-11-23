using System;
using System.Collections.Generic;

namespace StackTraceUtility
{
    public class WeakReferenceKey : IComparable<WeakReferenceKey>, IComparer<WeakReferenceKey>
    {
        public WeakReference Reference { get; private set; }

        public WeakReferenceKey(object target)
        {
            Reference = new WeakReference(target);
        }

        public object Target => Reference.IsAlive ? Reference.Target : null;

        // Define equality and comparison logic based on the object reference
        public override bool Equals(object obj)
        {
            if (obj is WeakReferenceKey other)
            {
                return Reference.Target == other.Reference.Target;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // The hash code can be based on the target object, if alive
            return Reference.Target?.GetHashCode() ?? 0;
        }


        // Comparison logic: Compare WeakReferenceKey objects by their Target objects.
        public int Compare(WeakReferenceKey x, WeakReferenceKey y)
        {
            // If both are null or dead (both targets are null), they're equal.
            if (x?.Target == null && y?.Target == null)
                return 0;

            // If only one is null (dead reference), the non-null one is "greater".
            if (x?.Target == null)
                return -1;
            if (y?.Target == null)
                return 1;

            // Both targets are non-null: Compare using IComparable, if possible
            if (x.Target is IComparable comparableX && y.Target is IComparable comparableY)
            {
                return comparableX.CompareTo(comparableY); // Use IComparable's CompareTo method
            }

            // Fallback: if neither target is IComparable, use custom logic or default comparison
            return x.Target?.GetHashCode().CompareTo(y.Target?.GetHashCode()) ?? 0;
        }

        // IComparable implementation for comparing the current instance with another.
        public int CompareTo(WeakReferenceKey other)
        {
            if (other == null)
                return 1; // this object is considered "greater" than null

            // Compare based on the Target property.
            return Compare(this, other);
        }
    }
}
