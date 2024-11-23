In modern projects, there are often many moving parts.
Smart logging in long-established projects can significantly
simplify a developer's work. If you have many instances of a  
single type and it's important for you to identify all of  
them and log information about any one of them, this  
library can be useful. 
This library helps you add markers to the stack trace by  
generating methods above a block or method.  
The generated method names will be in the following format:  
<Marker_<tag>_[Number]>  
<Marker_<tag>_[Parent__Number]_[Number]>  

To associate a class instance with a generated  
method name in the format <Marker_<tag>_[Number]>, use:  
string Register(object target) / Task<string> RegisterAsync(object target)
(The method returns the future method name)

To generate a method name in the format <Marker_<tag>_[Parent__Number]_[Number]>, use:  
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