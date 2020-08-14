﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Coderr.Client.Converters;
using Newtonsoft.Json;

namespace Coderr.Client.Contracts
{
    /// <summary>
    ///     Model used to wrap all information from an exception.
    /// </summary>
    public class ExceptionDTO
    {
        private static readonly CultureInfo English = new CultureInfo("en-US");
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExceptionDTO" /> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <exception cref="System.ArgumentNullException">exception</exception>
        public ExceptionDTO(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            Copy(exception, this);
            Properties = new Dictionary<string, string>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExceptionDTO" /> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <exception cref="System.ArgumentNullException">exception</exception>
        public ExceptionDTO(ExceptionDTO exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            AssemblyName = exception.AssemblyName;
            BaseClasses = exception.BaseClasses;
            Everything = exception.Everything;
            FullName = exception.FullName;
            if (exception.InnerException != null)
            {
                InnerException = new ExceptionDTO(exception.InnerException);
            }

            Message = exception.Message;
            Namespace = exception.Namespace;
            Name = exception.Name;
            Properties = new Dictionary<string, string>(exception.Properties);
            StackTrace = exception.StackTrace;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExceptionDTO" /> class.
        /// </summary>
        [JsonConstructor]
        protected ExceptionDTO()
        {
        }

        /// <summary>
        ///     Assembly name (version included)
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        ///     Exception base classes. Most specific first: <c>ArgumentOutOfRangeException</c>, <c>ArgumentException</c>,
        ///     <c>Exception</c>.
        /// </summary>
        public string[] BaseClasses { get; set; }

        /// <summary>
        ///     Everything (<c>exception.ToString()</c>)
        /// </summary>
        public string Everything { get; set; }

        /// <summary>
        ///     Full type name (namespace + class name)
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        ///     Inner exception (if any; otherwise <c>null</c>).
        /// </summary>
        public ExceptionDTO InnerException { get; set; }

        /// <summary>
        ///     Exception message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     Type name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Namespace that the exception is in
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        ///     All properties (public and private)
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

        /// <summary>
        ///     Stack trace, line numbers included if your app also distributes the PDB files.
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        ///     Copy the .NET exception information into our DTO.
        /// </summary>
        /// <param name="source">Exception to copy from</param>
        /// <param name="destination">Model to put the information in.</param>
        public static void Copy(Exception source, ExceptionDTO destination)
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            var uiCulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                var evt = new ManualResetEventSlim(false);

                // in a separate thread to not get any side effects due to the culture change.
                ThreadPool.QueueUserWorkItem(x =>
                {
                    //TODO: wont work for some exceptions (where texts are preloaded). We need to use Google Translate.
                    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = English;
                    try
                    {
                        destination.FullName = source.GetType().FullName;
                        destination.Name = source.GetType().Name;
                        destination.Namespace = source.GetType().Namespace;
                        destination.AssemblyName = source.GetType().Assembly.FullName;
                        destination.Message = source.Message;

                        var baseTypes = GetBaseClasses(source);
                        destination.BaseClasses = baseTypes.ToArray();
                        destination.StackTrace = source.StackTrace;
                        destination.Properties = GetProperties(source);

                        try
                        {
                            destination.Everything = Serializer.Serialize(source);
                        }
                        catch
                        {
                            destination.Everything = source.ToString();
                        }

                        if (source.InnerException != null)
                            destination.InnerException = new ExceptionDTO(source.InnerException);
                    }
                    catch
                    {
                        Thread.CurrentThread.CurrentCulture = culture;
                        Thread.CurrentThread.CurrentUICulture = uiCulture;
                    }
                    evt.Set();
                });
                evt.Wait();

                if (destination.StackTrace == null)
                {
                    Debug.WriteLine("StackTrace can be empty (SoapException), but it's not likely");
                    destination.StackTrace = "";
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = uiCulture;
            }
        }

        /// <summary>
        ///     Get all properties from an exception (public and non public).
        /// </summary>
        /// <param name="exception">Exception to scan.</param>
        /// <returns>Properties</returns>
        public static Dictionary<string, string> GetProperties(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            var properties = new Dictionary<string, string>();
            var props =
                exception.GetType()
                    .GetProperties(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic |
                                   BindingFlags.Public);
            foreach (var propertyInfo in props)
            {
                if (propertyInfo.GetIndexParameters().Length > 0)
                    continue;

                if (propertyInfo.Name == "BaseException"
                    || propertyInfo.Name == "Data"
                    || propertyInfo.Name == "StackTrace")
                    continue;

                var value = propertyInfo.GetValue(exception, null);
                if (value == null)
                    continue;

                var name = propertyInfo.Name;

                AddProperty(properties, name, value);
            }

            var methods =
                exception.GetType()
                    .GetMethods(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic |
                                BindingFlags.Public)
                    .Where(x => x.Name.StartsWith("Get") && x.GetParameters().Length == 0);
            foreach (var methodInfo in methods)
            {
                if (methodInfo.Name == "GetType" || methodInfo.Name == "GetHashCode" ||
                    methodInfo.Name == "GetStackTrace"
                    || methodInfo.Name == "GetBaseException" || methodInfo.Name == "GetExceptionMethodFromString")
                    continue;

                try
                {
                    var value = methodInfo.Invoke(exception, null);
                    if (value == null)
                        continue;

                    AddProperty(properties, methodInfo.Name.Remove(0, 3), value.ToString());
                }
                catch (Exception ex)
                {
                    properties.Add(methodInfo.Name + "_error", ex.ToString());
                }
            }

            return properties;
        }

        private static void AddProperty(IDictionary<string, string> properties, string name, object value)
        {
            // string requires to be first as it's enumerable
            if (value is string)
            {
                properties.Add(name, value.ToString());
            }
            else
            {
                var lst = value as IEnumerable;
                if (lst != null)
                    try
                    {
                        var index = 0;
                        foreach (var item in lst)
                        {
                            properties.Add(name + "_" + index, item.ToString());
                            index++;
                        }
                    }
                    catch (Exception ex)
                    {
                        properties.Add(name + "_error", ex.ToString());
                        properties.Add(name, value.ToString());
                    }
                properties.Add(name, value.ToString());
            }
        }

        private static List<string> GetBaseClasses(Exception exception)
        {
            var baseTypes = new List<string>();
            var baseClass = exception.GetType().BaseType;
            while (baseClass != null)
            {
                if (baseClass != typeof(object))
                    baseTypes.Add(baseClass.FullName);
                baseClass = baseClass.BaseType;
            }
            return baseTypes;
        }
    }
}