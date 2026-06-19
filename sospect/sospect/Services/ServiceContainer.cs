// Codigo de William Gerardo Escobar Torres
// Desarrollador: William Gerardo Escobar Torres
// LinkedIn: https://www.linkedin.com/in/william-gerardo-escobar-torres-29458b66/
// Correo: wescot2000@gmail.com
// Registro DNDA: 13-91-449, 19-sept.-2022

using System;
using System.Collections.Generic;

namespace sospect.Services
{
    public static class ServiceContainer
    {
        static readonly Dictionary<Type, Lazy<object>> services
            = new Dictionary<Type, Lazy<object>>();

        public static void Register<T>(Func<T> function)
            => services[typeof(T)] = new Lazy<object>(() => function());

        public static T Resolve<T>()
            => (T)Resolve(typeof(T));

        public static object Resolve(Type type)
        {
            {
                if (services.TryGetValue(type, out var service))
                    return service.Value;

                throw new KeyNotFoundException($"Service not found for type '{type}'");
            }
        }
    }
}



