// MIT License - Copyright (C) The Mono.Xna Team This file is subject to the terms and conditions defined in file 'LICENSE.txt', which is part of this
// source code package.

using MonoGame.Framework.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework
{
    /// <summary> A container for services for a <see cref="Game"/>. </summary>
    public class GameServiceContainer : IServiceProvider
    {
        private readonly Dictionary<Type, object> services = [];
        private readonly List<IServiceProvider> childProviders = [];

        /// <summary> Create an empty <see cref="GameServiceContainer"/>. </summary>
        public GameServiceContainer() { }

        /// <summary> Add a service provider to this container. </summary>
        /// <param name="type"> The type of the service. </param>
        /// <param name="provider"> The provider of the service. </param>
        /// <exception cref="ArgumentNullException"> If <paramref name="type"/> or <paramref name="provider"/> is <see langword="null"/>. </exception>
        /// <exception cref="ArgumentException"> If <paramref name="provider"/> cannot be assigned to <paramref name="type"/>. </exception>
        public void AddService(Type type, object provider)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (!ReflectionHelpers.IsAssignableFrom(type, provider))
                throw new ArgumentException("The provider does not match the specified service type!");

            services.Add(type, provider);
        }

        /// <summary> Get a service provider for the service of the specified type. </summary>
        /// <param name="type"> The type of the service. </param>
        /// <returns>
        /// A service provider for the service of the specified type or <see langword="null"/> if no suitable service provider is registered in this container.
        /// </returns>
        /// <exception cref="ArgumentNullException"> If the specified type is <see langword="null"/>. </exception>
        public object GetService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (services.TryGetValue(type, out object service))
                return service;
            else
            {
                foreach (IServiceProvider provider in childProviders)
                {
                    if ((service = provider.GetService(type)) != null)
                        return service;
                }
            }

            return null;
        }

        /// <summary> Remove the service with the specified type. Does nothing no service of the specified type is registered. </summary>
        /// <param name="type"> The type of the service to remove. </param>
        /// <exception cref="ArgumentNullException"> If the specified type is <see langword="null"/>. </exception>
        public void RemoveService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            services.Remove(type);
        }

        /// <summary> Add a service provider to this container. </summary>
        /// <typeparam name="T"> The type of the service. </typeparam>
        /// <param name="provider"> The provider of the service. </param>
        /// <exception cref="ArgumentNullException"> If <paramref name="provider"/> is <see langword="null"/>. </exception>
        public void AddService<T>(T provider)
            => AddService(typeof(T), provider);

        /// <summary> Get a service provider of the specified type. </summary>
        /// <typeparam name="T"> The type of the service provider. </typeparam>
        /// <returns>
        /// A service provider of the specified type or <see langword="null"/> if no suitable service provider is registered in this container.
        /// </returns>
        public T GetService<T>() where T : class
        {
            object service = GetService(typeof(T));
            return (T)service;
        }

        /// <summary> Adds an <see cref="IServiceProvider"/> to the list of children this container will also check for values. </summary>
        /// <param name="provider"> The <see cref="IServiceProvider"/> to be added to the list. </param>
        /// <exception cref="ArgumentException"> If <paramref name="provider"/> is already a child. </exception>
        /// <exception cref="ArgumentNullException"> If <paramref name="provider"/> is <see langword="null"/>. </exception>
        public void AddServiceProvider(IServiceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (childProviders.Contains(provider))
                throw new ArgumentException($"Cannot add an {nameof(IServiceProvider)} that is already contained.", nameof(provider));
            childProviders.Add(provider);
        }

        /// <summary> Adds an <see cref="IServiceProvider"/> to the list of children this container will also check for values. </summary>
        /// <param name="provider"> The <see cref="IServiceProvider"/> to be added to the list. </param>
        /// <exception cref="ArgumentException"> If <paramref name="provider"/> is not already a child. </exception>
        /// <exception cref="ArgumentNullException"> If <paramref name="provider"/> is <see langword="null"/>. </exception>
        public void RemoveServiceProvider(IServiceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            int index = childProviders.IndexOf(provider);
            if (index == -1)
                throw new ArgumentException($"Cannot remove an {nameof(IServiceProvider)} that is not contained.", nameof(provider));
            childProviders.RemoveAt(index);
        }
    }
}
