using StackTraceUtility;
using System;
using System.Diagnostics;

namespace StackTraceUtilityConsole
{
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
        /*
         * Release
         Void DoSomething()
  =>     Void Marker_0(System.Action)
         Void Main(System.String[])
         
        Debug
        Void DoSomething()
        Void <Invoke>b__2_0()
  =>    Void Marker_0(System.Action)
        Void BuildMarker(System.Object, System.Action)
        Void Invoke()
        Void Main(System.String[])
         */

        static void Main(string[] args)
        {
              var marker = new StackTraceMarker(new SimpleObjectsHolder());
              var instance = new Instance(marker);
              instance.Invoke();
              Console.ReadLine();
        }
    }
}
