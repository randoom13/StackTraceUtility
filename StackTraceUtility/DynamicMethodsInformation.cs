using System;

namespace StackTraceUtility
{
    public static class DynamicMethodsInformation
    {
        private static string _assemblyName = "DynamicAssembly";
        public static string AssemblyName 
        {
            get => _assemblyName;
            set {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(nameof(AssemblyName));

                _assemblyName = value;
            } 
        }

        private static string _moduleName = "DynamicModule";
        public static string ModuleName
        {
            get => _moduleName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(nameof(ModuleName));

                _moduleName = value;
            }
        }

        private static string _className = "DynamicClass";
        public static string ClassName
        {
            get => _className;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(nameof(ClassName));

                _className = value;
            }
        }
    }
}
