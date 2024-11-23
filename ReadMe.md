 ﻿In modern projects, there are often many moving parts. Smart logging in  
  long-established projects can significantly simplify a developer's work.  
  If you have many instances of a single type and it's important for you  
  to identify precisely all of them and log information about any one of  
  them, this library can be useful.  
 This library helps you add markers to the stack trace by generating  
 methods above a block or method.
  The generated method names will be in the following format:  
<Marker_<tag>_[Number]>  
<Marker_<tag>_[Parent__Number]_[Number]>  
p/s This library cannot cause memory leaks because it stores all required  
information using weak references

 To associate a class instance with a generated method name in the format  
 <Marker_<tag>_|Number|>, use 
string Register(object target) / Task<string> RegisterAsync(object target)
(The method returns the future method name)

 To associate a class instance with a generated method name in the format  
 <Marker_<tag>_|Parent__Number|_|Number|>, use:  
IEnumerable<string> Register(object parent, IEnumerable<object> children) / Task<IEnumerable<string>> RegisterAsync(object parent, IEnumerable<object> children)
(The method returns the future method names)

 You can apply this marking to a block or method by calling:  
void BuildMarker(object target, Action action) / Task BuildMarkerAsync(object target, Action action)
The body of the block or method should be inside the action.

Example
```xml
public class Program
    {
        public class Instance 
        {
            private readonly IStackTraceMarker _traceMarker;
            public Instance(IStackTraceMarker traceMarker) 
            {
                _traceMarker = traceMarker;
                _traceMarker.Register(this);
            }

            public void Invoke()
            {
               _traceMarker.BuildMarker(this, () =>
                    {
                        DoSomething();
                    });
            }

            private void DoSomething()
            {
                StackTrace stackTrace = new StackTrace(true);

                // Print each method in the stack trace
                foreach (var frame in stackTrace.GetFrames())
                {
                    Console.WriteLine(frame.GetMethod());
                }

            }
        }

        static void Main(string[] args)
        {
              var marker = new StackTraceMarker(new SimpleObjectsHolder());
              var instance = new Instance(marker);
              instance.Invoke();
              Console.ReadLine();
        }
    }
```
Result
```xml
RELEASE
         Void DoSomething()
  =>     Void Marker_0(System.Action)
         Void Main(System.String[])
         
DEBUG
        Void DoSomething()
        Void <Invoke>b__2_0()
  =>    Void Marker_0(System.Action)
        Void BuildMarker(System.Object, System.Action)
        Void Invoke()
        Void Main(System.String[])
```
By default, StackTraceMarker works online (IsOnline = true). You can turn  
it off using the TurnOff() method and turn it back on with TurnOn().

When turned off (and if ApplyOffCache = true, which is the default),  
StackTraceMarker collects registered items in a cache (a linked list). When  
StackTraceMarker is turned back on (and if ApplyOffCache remains true), the  
items from the cache are delivered to the internal holder.

You can choose from three options for the internal holder:

SimpleObjectsHolder – A simple and obvious choice with no concern for cleaning,  
as it uses a ConditionalWeakTable.

BTreeObjectsHolder – You need to manage cleaning the dictionary when a registered  
instance is garbage collected. This can be done using IStackTraceMarker.Clean() or  
by calling IStackTraceMarker.BuildMarker() with BaseBTreeObjectsHolder.ApplyCleanupOnBuild = true.

AdvancedBTreeObjectsHolder – An extension of BTreeObjectsHolder, which includes a mechanism  
that triggers IStackTraceMarker.Clean() when a registered item is garbage collected.
