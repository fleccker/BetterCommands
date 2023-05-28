using PluginAPI.Core;
using PluginAPI.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BetterCommands.Support.Compendium
{
    public static class CompendiumSupport
    {
        private static Assembly _assembly;
        private static bool _assemblyRetrieved;

        private static Type _cacheManagerType;
        private static bool _cacheManagerTypeRetrieved;

        private static MethodInfo _uniqueIdMethod;
        private static bool _uniqueIdMethodRetrieved;

        private static MethodInfo _uniqueIdToIpMethod;
        private static bool _uniqueIdToIpMethodRetrieved;

        private static Func<ReferenceHub, string> _uniqueIdDelegate;
        private static bool _uniqueIdDelegateRetrieved;

        private static Func<string, string> _uniqueIdToIpDelegate;
        private static bool _uniqueIdToIpDelegateRetrieved;

        public const string CacheManagerFullName = "";

        public static bool IsAvailable
        {
            get
            {
                if (!_assemblyRetrieved)
                {
                    return Assembly != null;
                }

                return true;
            }
        }

        public static Assembly Assembly
        {
            get
            {
                if (_assembly is null)
                {
                    if (!_assemblyRetrieved)
                    {
                        _assemblyRetrieved = true;
                        TryGetAssembly(out _assembly);
                    }
                }

                return _assembly;
            }
        }

        public static Type CacheManagerType
        {
            get
            {
                if (_cacheManagerType is null)
                {
                    if (!_cacheManagerTypeRetrieved)
                    {
                        _cacheManagerTypeRetrieved = true;
                        if (Assembly != null) _cacheManagerType = _assembly.GetType(CacheManagerFullName);
                    }
                }

                return _cacheManagerType;
            }
        }

        public static MethodInfo UniqueIdMethod
        {
            get
            {
                if (_uniqueIdMethod is null)
                {
                    if (!_uniqueIdMethodRetrieved)
                    {
                        _uniqueIdMethod = CacheManagerType?.GetMethod("GetPlayerId");
                        _uniqueIdMethodRetrieved = true;
                    }
                }

                return _uniqueIdMethod;
            }
        }

        public static MethodInfo UniqueIdToIpMethod
        {
            get
            {
                if (_uniqueIdToIpMethod is null)
                {
                    if (!_uniqueIdToIpMethodRetrieved)
                    {
                        _uniqueIdToIpMethod = CacheManagerType?.GetMethod("GetIpById");
                        _uniqueIdToIpMethodRetrieved = true;
                    }
                }

                return _uniqueIdToIpMethod;
            }
        }

        public static Func<ReferenceHub, string> UniqueIdDelegate
        {
            get
            {
                if (_uniqueIdDelegate is null)
                {
                    if (!_uniqueIdDelegateRetrieved)
                    {
                        _uniqueIdDelegateRetrieved = true;
                        if (UniqueIdMethod != null) _uniqueIdDelegate = Delegate.CreateDelegate(typeof(Func<ReferenceHub, string>), UniqueIdMethod) as Func<ReferenceHub, string>;
                    }
                }

                return _uniqueIdDelegate;
            }
        }

        public static Func<string, string> UniqueIdToIpDelegate
        {
            get
            {
                if (_uniqueIdToIpDelegate is null)
                {
                    if (!_uniqueIdToIpDelegateRetrieved)
                    {
                        _uniqueIdToIpDelegateRetrieved = true;
                        if (UniqueIdToIpMethod != null) _uniqueIdToIpDelegate = Delegate.CreateDelegate(typeof(Func<string, string>), UniqueIdToIpMethod) as Func<string, string>;
                    }
                }

                return _uniqueIdToIpDelegate;
            }
        }

        public static bool TryGetUniqueId(ReferenceHub hub, out string uniqueId)
        {
            uniqueId = null;

            if (UniqueIdDelegate is null) return false;

            uniqueId = UniqueIdDelegate(hub);
            return true;
        }

        public static bool TryGetIpById(string id, out string ip)
        {
            ip = null;

            if (UniqueIdToIpDelegate is null) return false;

            ip = UniqueIdToIpDelegate(id);
            return true;
        }

        public static bool TryGetAssembly(out Assembly assembly)
        {
            foreach (var plugin in AssemblyLoader.InstalledPlugins)
            {
                if (plugin._entryPoint != null)
                {
                    if (plugin.PluginName == "Compendium")
                    {
                        assembly = plugin._entryPoint.DeclaringType.Assembly;
                        return true;
                    }
                }
            }

            assembly = null;
            return false;
        }
    }
}
