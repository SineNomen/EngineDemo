// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sojourn.PicnicIOC {
	//mapped to a specific type
	public class TypeData {
		public Type type;
		public List<FieldInfo> fields = new List<FieldInfo>();
		public List<PropertyInfo> properties = new List<PropertyInfo>();
		public MethodInfo callback;

		public override bool Equals(object obj) {
			TypeData other = obj as TypeData;
			return type == other.type;
		}

		public override int GetHashCode() {
			return type.GetHashCode();
		}
	}

	/// <summary>
	/// Inversion of control container handles dependency injection for registered types
	/// </summary>
	public class Container : Container.IScope {
		#region Static Members
		private Dictionary<Type, TypeData> _injectTable = new Dictionary<Type, TypeData>();
		private Dictionary<Type, TypeData> _autoInjectTable = new Dictionary<Type, TypeData>();
		//needs to map to a type
		private Dictionary<Type, HashSet<Object>> _autoInjectMap = new Dictionary<Type, HashSet<Object>>();
		public static Container Instance { get; set; } = new Container();
		#endregion

		#region Public interfaces
		/// <summary>
		/// Represents a scope in which per-scope objects are instantiated a single time
		/// </summary>
		public interface IScope : IDisposable, IServiceProvider {
		}

		/// <summary>
		/// IRegisteredType is return by Container.Register and allows further configuration for the registration
		/// </summary>
		public interface IRegisteredType {
			/// <summary>
			/// Make registered type a singleton
			/// </summary>
			void AsSingleton();

			/// <summary>
			/// Make registered type a per-scope type (single instance within a Scope)
			/// </summary>
			void PerScope();
		}
		#endregion

		// Map of registered types
		private readonly Dictionary<Type, Func<ILifetime, object>> _registeredTypes = new Dictionary<Type, Func<ILifetime, object>>();

		// Lifetime management
		private readonly ContainerLifetime _lifetime;

		/// <summary>
		/// Creates a new instance of IoC Container
		/// </summary>
		public Container() {
			_lifetime = new ContainerLifetime(t => _registeredTypes[t]);
		}

		/// <summary>
		/// Registers a factory function which will be called to resolve the specified interface
		/// </summary>
		/// <param name="interface">Interface to register</param>
		/// <param name="factory">Factory function</param>
		/// <returns></returns>
		public IRegisteredType Register(Type @interface, Func<object> factory)
			=> RegisterType(@interface, _ => factory());

		/// <summary>
		/// Registers an implementation type for the specified interface
		/// </summary>
		/// <param name="interface">Interface to register</param>
		/// <param name="implementation">Implementing type</param>
		/// <returns></returns>
		public IRegisteredType Register(Type @interface, Type implementation)
			=> RegisterType(@interface, FactoryFromType(implementation));

		private IRegisteredType RegisterType(Type itemType, Func<ILifetime, object> factory) {
			IRegisteredType rt = new RegisteredType(itemType, f => _registeredTypes[itemType] = f, factory);
			OnRegister(itemType);
			return rt;
		}

		/// <summary>
		/// Returns the object registered for the given type
		/// </summary>
		/// <param name="type">Type as registered with the container</param>
		/// <returns>Instance of the registered type</returns>
		public object GetService(Type type) {
			Func<ILifetime, object> val = null;
			if (_registeredTypes.TryGetValue(type, out val)) {
				return _registeredTypes[type](_lifetime);
			}
			return null;
		}

		/// <summary>
		/// Creates a new scope
		/// </summary>
		/// <returns>Scope object</returns>
		public IScope CreateScope() => new ScopeLifetime(_lifetime);

		/// <summary>
		/// Disposes any <see cref="IDisposable"/> objects owned by this container.
		/// </summary>
		public void Dispose() => _lifetime.Dispose();

		#region Lifetime management
		// ILifetime management adds resolution strategies to an IScope
		interface ILifetime : IScope {
			object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory);

			object GetServicePerScope(Type type, Func<ILifetime, object> factory);
		}

		// ObjectCache provides common caching logic for lifetimes
		abstract class ObjectCache {
			// Instance cache
			private readonly ConcurrentDictionary<Type, object> _instanceCache = new ConcurrentDictionary<Type, object>();

			// Get from cache or create and cache object
			//`Mat may need to call nRegister here
			protected object GetCached(Type type, Func<ILifetime, object> factory, ILifetime lifetime)
				=> _instanceCache.GetOrAdd(type, _ => factory(lifetime));

			public void Dispose() {
				foreach (var obj in _instanceCache.Values)
					(obj as IDisposable)?.Dispose();
			}
		}

		// Container lifetime management
		class ContainerLifetime : ObjectCache, ILifetime {
			// Retrieves the factory functino from the given type, provided by owning container
			public Func<Type, Func<ILifetime, object>> GetFactory { get; private set; }

			public ContainerLifetime(Func<Type, Func<ILifetime, object>> getFactory) => GetFactory = getFactory;

			public object GetService(Type type) => GetFactory(type)(this);

			// Singletons get cached per container
			public object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory)
				=> GetCached(type, factory, this);

			// At container level, per-scope items are equivalent to singletons
			public object GetServicePerScope(Type type, Func<ILifetime, object> factory)
				=> GetServiceAsSingleton(type, factory);
		}

		// Per-scope lifetime management
		class ScopeLifetime : ObjectCache, ILifetime {
			// Singletons come from parent container's lifetime
			private readonly ContainerLifetime _parentLifetime;

			public ScopeLifetime(ContainerLifetime parentContainer) => _parentLifetime = parentContainer;

			public object GetService(Type type) => _parentLifetime.GetFactory(type)(this);

			// Singleton resolution is delegated to parent lifetime
			public object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory)
				=> _parentLifetime.GetServiceAsSingleton(type, factory);

			// Per-scope objects get cached
			public object GetServicePerScope(Type type, Func<ILifetime, object> factory)
				=> GetCached(type, factory, this);
		}
		#endregion

		#region Container items
		// Compiles a lambda that calls the given type's first constructor resolving arguments
		private static Func<ILifetime, object> FactoryFromType(Type itemType) {
			// Get first constructor for the type
			var constructors = itemType.GetConstructors();
			if (constructors.Length == 0) {
				// If no public constructor found, search for an internal constructor
				constructors = itemType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
			}
			var constructor = constructors.First();

			// Compile constructor call as a lambda expression
			var arg = Expression.Parameter(typeof(ILifetime));
			return (Func<ILifetime, object>)Expression.Lambda(
				Expression.New(constructor, constructor.GetParameters().Select(
					param => {
						var resolve = new Func<ILifetime, object>(
							lifetime => lifetime.GetService(param.ParameterType));
						return Expression.Convert(
							Expression.Call(Expression.Constant(resolve.Target), resolve.Method, arg),
							param.ParameterType);
					})),
				arg).Compile();
		}

		// RegisteredType is supposed to be a short lived object tying an item to its container
		// and allowing users to mark it as a singleton or per-scope item
		class RegisteredType : IRegisteredType {
			private readonly Type _itemType;
			private readonly Action<Func<ILifetime, object>> _registerFactory;
			private readonly Func<ILifetime, object> _factory;

			public RegisteredType(Type itemType, Action<Func<ILifetime, object>> registerFactory, Func<ILifetime, object> factory) {
				_itemType = itemType;
				_registerFactory = registerFactory;
				_factory = factory;

				registerFactory(_factory);
			}

			public void AsSingleton()
				=> _registerFactory(lifetime => lifetime.GetServiceAsSingleton(_itemType, _factory));

			public void PerScope()
				=> _registerFactory(lifetime => lifetime.GetServicePerScope(_itemType, _factory));
		}
		#endregion

		/// <summary>
		/// Registers and object to have it's properties auto-injected whenever register is called
		/// </summary>
		/// <param name="obj">Object to inject</param>
		/// <summary>
		//`Mat call this when the registration is finished
		private void OnRegister(Type type) {
			AutoInjectAll(type);
			//call the callback
			// object[] objectArray = new object[] { type };
			// foreach (AutoInjectData data in list) {
			// 	data.callback.Invoke(data.obj, objectArray);
			// }
		}

		private TypeData CreateTypeData<T>(Type type) where T : Attribute {
			TypeData data = new TypeData();
			data.type = type;

			FieldInfo[] fields = type.GetFields(System.Reflection.BindingFlags.Instance
												| System.Reflection.BindingFlags.Public
												| System.Reflection.BindingFlags.NonPublic
												| System.Reflection.BindingFlags.GetField
												| System.Reflection.BindingFlags.GetProperty
												);
			foreach (FieldInfo field in fields) {
				T inject = field.GetCustomAttribute<T>();
				if (inject != null) {
					data.fields.Add(field);
				}
			}
			PropertyInfo[] props = type.GetProperties(System.Reflection.BindingFlags.Instance
													| System.Reflection.BindingFlags.Public
													| System.Reflection.BindingFlags.NonPublic
													| System.Reflection.BindingFlags.GetField
													| System.Reflection.BindingFlags.GetProperty
													);
			foreach (PropertyInfo prop in props) {
				T inject = prop.GetCustomAttribute<T>();
				if (inject != null) {
					data.properties.Add(prop);
				}
			}

			return data;
		}

		public TypeData GetTypeData(Type type) {
			TypeData data = null;
			if (!_injectTable.TryGetValue(type, out data)) {
				data = CreateTypeData<Inject>(type);
				_injectTable[type] = data;
			}
			return data;
		}

		public TypeData GetAutoTypeData(Type type) {
			TypeData data = null;
			if (!_autoInjectTable.TryGetValue(type, out data)) {
				data = CreateTypeData<AutoInject>(type);
				_autoInjectTable[type] = data;
			}
			return data;
		}

		/// <summary>
		/// Injects all properties and fields of type on all objects in the auto-ibject list
		/// </summary>
		/// <param name="type">Type of object to autoinject</param>
		public void AutoInjectAll(Type type) {
			HashSet<Object> objects = null;
			if (_autoInjectMap.TryGetValue(type, out objects)) {
				foreach (Object obj in objects) {
					AutoInjectObject(obj);
				}
			}
		}

		/// <summary>
		/// Registers object to auto-inject list for the spullied type
		/// </summary>
		/// <param name="type">Type of the property that can be auto-injected </param>
		/// <param name="obj">Object to inject</param>

		public void AddAutoInjectObject(Type type, Object obj) {
			HashSet<Object> objects = null;
			if (!_autoInjectMap.TryGetValue(type, out objects)) {
				_autoInjectMap[type] = new HashSet<Object>();
				objects = _autoInjectMap[type];
			}
			objects.Add(obj);
		}

		/// <summary>
		/// Injects all properties and fields on the supplied object
		/// </summary>
		/// <param name="obj">Object to inject</param>
		public void InjectObject(Object obj) {
			// UnityEngine.Debug.LogFormat("Inject: {0}", obj);
			System.Type type = obj.GetType();
			while (type != null) {
				TypeData data = Instance.GetAutoTypeData(type);

				foreach (FieldInfo field in data.fields) {
					Type fieldType = field.FieldType;
					Object val = Instance.GetService(fieldType);
					field.SetValue(obj, val);
				}
				foreach (PropertyInfo prop in data.properties) {
					Type propType = prop.PropertyType;
					Object val = Instance.GetService(propType);
					prop.SetValue(obj, val);
				}
				type = type.BaseType;
			}

			//`Mat also do the [AutoInject]ed ones
			AutoInjectObject(obj);
		}

		/// <summary>
		/// Injects all properties and fields on the supplied object and adds it to auto-inject list
		/// </summary>
		/// <param name="obj">Object to inject</param>
		public void AutoInjectObject(Object obj) {
			// UnityEngine.Debug.LogFormat("AutoInject: {0}", obj);
			System.Type type = obj.GetType();
			while (type != null) {
				TypeData data = Instance.GetAutoTypeData(type);

				foreach (FieldInfo field in data.fields) {
					Type fieldType = field.FieldType;
					Object val = Instance.GetService(fieldType);
					AddAutoInjectObject(fieldType, obj);
					field.SetValue(obj, val);
				}
				foreach (PropertyInfo prop in data.properties) {
					Type propType = prop.PropertyType;
					Object val = Instance.GetService(propType);
					AddAutoInjectObject(propType, obj);
					prop.SetValue(obj, val);
				}
				type = type.BaseType;
			}
		}
		/// <summary>
		/// Static version of Inject
		/// </summary>
		public static void Inject(Object obj) {
			Instance.InjectObject(obj);
			Instance.AutoInjectObject(obj);
		}
		/// <summary>
		/// Static version of Inject
		/// </summary>
		public static void AutoInject(Object obj) {
			// Instance.InjectObject(obj);
			Instance.AutoInjectObject(obj);
		}

		/// <summary>
		/// Registers an implementation type for the specified interface
		/// </summary>
		/// <typeparam name="T">Interface to register</typeparam>
		/// <param name="type">Implementing type</param>
		/// <returns>IRegisteredType object</returns>
		public static Container.IRegisteredType Register<T>(Type type) {
			return Instance.Register(typeof(T), type);
		}

		/// <summary>
		/// Registers an implementation type for the specified interface
		/// </summary>
		/// <typeparam name="TInterface">Interface to register</typeparam>
		/// <typeparam name="TImplementation">Implementing type</typeparam>
		/// <returns>IRegisteredType object</returns>
		public static Container.IRegisteredType Register<TInterface, TImplementation>() {
			return Instance.Register(typeof(TInterface), typeof(TImplementation));
		}

		/// <summary>
		/// Registers an object to resolve the specified interface
		/// </summary>
		/// <typeparam name="T">Interface to register</typeparam>
		/// <param name="object">Object of interface type</param>
		/// <returns>IRegisteredType object</returns>
		public static Container.IRegisteredType Register<T>(T obj) {
			return Instance.Register(typeof(T), () => { return obj; });
		}

		/// <summary>
		/// Registers a factory function which will be called to resolve the specified interface
		/// </summary>
		/// <typeparam name="T">Interface to register</typeparam>
		/// <param name="factory">Factory method</param>
		/// <returns>IRegisteredType object</returns>
		public static Container.IRegisteredType Register<T>(Func<T> factory) {
			return Instance.Register(typeof(T), () => factory());
		}

		/// <summary>
		/// Registers a type
		/// </summary>
		/// <typeparam name="T">Type to register</typeparam>
		/// <returns>IRegisteredType object</returns>
		public static Container.IRegisteredType Register<T>() {
			return Instance.Register(typeof(T), typeof(T));
		}

		/// <summary>
		/// Returns an implementation of the specified interface
		/// </summary>
		/// <typeparam name="T">Interface type</typeparam>
		/// <param name="scope">This scope instance</param>
		/// <returns>Object implementing the interface</returns>
		public static T Resolve<T>() {
			return (T)Instance.GetService(typeof(T));
		}

	}
}