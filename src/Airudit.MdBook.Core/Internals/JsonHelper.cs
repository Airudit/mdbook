
namespace Airudit.MdBook.Core.Internals
{
    using Airudit.Promethai.Domain.Core.Internals;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Helps manipulate JSON objects. 
    /// </summary>
    public sealed class JsonHelper
    {
        private readonly string source;

        public JsonHelper(string source)
        {
            this.source = source;
        }

        /// <summary>
        /// Gets a DateTime from a property. 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">if the property does not exist, is null, or does not represent a DateTime</exception>
        public DateTime GetDateTime(JObject element, string name)
        {
            return this.GetValue<DateTime>(element, name, this.Parse);
        }

        /// <summary>
        /// Gets a DateTime from a property, accepting a default value when not set. 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <param name="defaultValue">When null, will not tolerate empty values. When set, will be returned when the property is not set or has no value.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">if the property does not exist, is null, or does not represent a DateTime</exception>
        public DateTime? GetDateTime(JObject element, string name, DateTime? defaultValue)
        {
            return this.GetNullableValue<DateTime>(element, name, this.Parse, defaultValue);
        }

        public string SetValue(JObject element, string name, DateTime value)
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else
            {
                var newValue = value.ToString("o", CultureInfo.InvariantCulture);
                element[name] = newValue;
                return newValue;
            }
        }

        public string SetValue(JObject element, string name, string value)
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else
            {
                var newValue = value;
                element[name] = newValue;
                return newValue;
            }
        }

        /// <summary>
        /// Gets a Boolean from a property. 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">if the property does not exist, is null, or does not represent a Boolean</exception>
        public bool GetBoolean(JObject element, string name)
        {
            return this.GetValue<Boolean>(element, name, this.Parse);
        }

        /// <summary>
        /// Gets a Boolean from a property. 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">if the property does not exist, is null, or does not represent a Boolean</exception>
        public bool? GetNullableBoolean(JObject element, string name)
        {
            return this.GetNullableValue<Boolean>(element, name, this.Parse, null);
        }

        /// <summary>
        /// Gets a Enum value from a property, accepting a default value when not set. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue">When null, will not tolerate empty values. When set, will be returned when the property is not set or has no value.</param>
        /// <param name="element"></param>
        /// <exception cref="InvalidOperationException">if the property does not exist, is null, or does not represent the specified type</exception>
        public T GetEnumValue<T>(JObject element, string name, T? defaultValue)
            where T : struct
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else if (element.TryGetValue(name, out JToken? child))
            {
                var valueElement = child as JValue;
                if (valueElement != null && valueElement.Type == JTokenType.Null)
                {
                    return this.SetValueOrFail(element, name, defaultValue);
                }
                else if (valueElement != null && valueElement.Type == JTokenType.String)
                {
                    var stringValue = (string?)valueElement.Value;
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        return defaultValue ?? throw this.Fail("Element[" + name + "] has no value. ");
                    }
                    else if (Enum.TryParse<T>(stringValue, out T value))
                    {
                        return value;
                    }
                    else
                    {
                        throw this.Fail("Element[" + name + "] should be a " + typeof(T).Name + " as a String, failed to parse value " + stringValue + ". ");
                    }
                }
                else
                {
                    throw this.Fail("Element[" + name + "] should be a " + typeof(T).Name + " as a String, got a " + child.Type + ". ");
                }
            }
            else
            {
                return this.SetValueOrFail(element, name, defaultValue);
            }
        }

        public Guid GetGuid(JObject element, string name)
        {
            return this.GetValue<Guid>(element, name, this.Parse);
        }

        public Guid? GetNullableGuid(JObject element, string name)
        {
            return this.GetNullableValue<Guid>(element, name, this.Parse, default(Guid?));
        }

        public int? GetNullableInt32(JObject element, string name)
        {
            return this.GetNullableValue<Int32>(element, name, this.Parse, default(Int32?));
        }

        public int? GetInt32(JObject element, string name)
        {
            return this.GetValue<Int32>(element, name, this.Parse);
        }

        /// <summary>
        /// Returns a string property. May return NULL.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">when property is not of string type</exception>
        public string? GetString(JObject element, string name)
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else if (element.TryGetValue(name, out JToken? child))
            {
                var valueElement = child as JValue;
                if (valueElement != null && valueElement.Type == JTokenType.Null)
                {
                    return null;
                }
                else if (valueElement != null && valueElement.Type == JTokenType.String)
                {
                    return (string)valueElement.Value;
                }
                else
                {
                    throw this.Fail("Element[" + name + "] should be a String, got a " + child.Type + ". ");
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a string[] property. May return NULL.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">if the property does not represent a string[]</exception>
        public string[] GetStringArray(JObject element, string name)
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else if (element.TryGetValue(name, out JToken child))
            {
                if (child.Type == JTokenType.Null)
                {
                    return null;
                }
                else if (child is JArray arrayElement)
                {
                    var values = new List<string>(arrayElement.Count);
                    int i = -1;
                    foreach (JToken item in arrayElement.Children())
                    {
                        i++;
                        if (item.Type == JTokenType.Null)
                        {
                            values.Add(null);
                        }
                        else if (item is JValue itemValue && itemValue.Type == JTokenType.String)
                        {
                            values.Add((string)itemValue.Value);
                        }
                        else
                        {
                            throw this.Fail("Element[" + name + "][" + i + "] should be a String, got a " + item.Type + ". ");
                        }
                    }

                    return values.ToArray();
                }
                else if (child is JValue valueElement && valueElement.Type == JTokenType.String)
                {
                    return new string[] { (string)valueElement.Value, };
                }
                else
                {
                    throw this.Fail("Element[" + name + "] should be a Array of String, got a " + child.Type + ". ");
                }
            }
            else
            {
                return null;
            }
        }

        public JArray SetStringArray(JObject element, string name, IList<string> values)
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else if (values == null)
            {
                element[name] = null;
                return null;
            }
            else
            {
                var array = new JArray(values);
                element[name] = array;
                return array;
            }
        }

        public T SetObject<T>(JObject element, string name, T value)
            where T : class
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else
            {
                if (value != null)
                {
                    if (value is IJsonObject thing)
                    {
                        element[name] = thing.Node;
                    }
                    else
                    {
                        this.Fail("Object " + value + " does not implement " + nameof(IJsonObject) + ". ");
                    }
                }
                else
                {
                    element[name] = null;
                }
            }

            return value;
        }

        public T GetObject<T>(JObject element, string name, Func<JObject, T> factory)
            where T : class
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else if (element.TryGetValue(name, out JToken child))
            {
                var objectElement = child as JObject;
                if (objectElement != null && objectElement.Type == JTokenType.Null)
                {
                    return this.SetObjectOrFail<T>(element, name, factory);
                }
                else if (objectElement != null && objectElement.Type == JTokenType.Object)
                {
                    var value = factory(objectElement);
                    return value;
                }
                else
                {
                    throw this.Fail("Element[" + name + "] should be a Object, got a " + child.Type + ". ");
                }
            }
            else
            {
                return this.SetObjectOrFail(element, name, factory);
            }
        }

        /// <summary>
        /// Gets an object from a property. It is created if it does not exist.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">when the property is not of Object type</exception>
        public JObject ObjectProperty(JObject element, string name)
        {
            JObject item;
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else if (element.TryGetValue(name, out JToken child))
            {
                var objectElement = child as JObject;
                if (objectElement != null && objectElement.Type == JTokenType.Null)
                {
                    item = new JObject();
                    element[name] = item;
                    return item;
                }
                else if (objectElement != null && objectElement.Type == JTokenType.Object)
                {
                    item = (JObject)objectElement;
                    return item;
                }
                else
                {
                    throw this.Fail("Element[" + name + "] should be a Object, got a " + child.Type + ". ");
                }
            }
            else
            {
                item = new JObject();
                element[name] = item;
                return item;
            }
        }

        /// <summary>
        /// Returns a magic list that bridges a JSON array and a typed dotnet list of T. 
        /// </summary>
        /// <typeparam name="T">The mapped type of dotnet object</typeparam>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <param name="factory">a method to create a T object linked with the given JObject</param>
        /// <returns></returns>
        public IList<T> GetList<T>(JObject element, string name, Func<JObject, T> factory)
            where T : IJsonObject
        {
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }

            JObjectList<T> list;
            JArray listElement;
            if (element.TryGetValue(name, out JToken child))
            {
                listElement = child as JArray;
                if (listElement != null)
                {
                    list = new JObjectList<T>(this, listElement, factory);
                }
                else
                {
                    throw this.Fail("Element[" + name + "] should be a List, got a " + child.Type + ". ");
                }
            }
            else
            {
                listElement = new JArray();
                list = new JObjectList<T>(this, listElement, factory);
                element.Add(name, listElement);
            }

            return list;
        }

        public JToken Serialize<T>(JObject element, string name, T @object)
            where T : class
        {
            if (@object == null)
            {
                return JValue.CreateNull();
            }

            var serialized = JObject.FromObject(@object);
            element[name] = serialized;
            return serialized;
        }

        public JToken SerializeList<T>(JObject element, string name, IList<T> list)
            where T : class
        {
            if (list == null)
            {
                return JValue.CreateNull();
            }

            var serialized = JArray.FromObject(list);
            element[name] = serialized;
            return serialized;
        }

        public T Deserialize<T>(JObject element, string name)
            where T : class
        {
            return this.DeserializeImpl<T>(element, name, null);
        }

        public T Deserialize<T>(JObject element, string name, Func<T> defaultValue)
            where T : class
        {
            return this.DeserializeImpl<T>(element, name, defaultValue);
        }

        private T DeserializeImpl<T>(JObject element, string name, Func<T> defaultValue)
            where T : class
        {
            T value;
            if (element == null)
            {
                throw this.Fail("Null element. ");
            }
            else if (element.TryGetValue(name, out JToken child))
            {
                var valueElement = child as JValue;
                if (valueElement != null && valueElement.Type == JTokenType.Null)
                {
                    // continue
                }
                else if (child.Type == JTokenType.Object)
                {
                    return ((JObject)child).ToObject<T>();
                }
                else if (child.Type == JTokenType.Array)
                {
                    return ((JArray)child).ToObject<T>();
                }
                else
                {
                    throw this.Fail("Element[" + name + "] should be a " + typeof(T).Name + " as a Object, got a " + child.Type + ". ");
                }
            }
            else
            {
            }

            if (defaultValue != null)
            {
                value = defaultValue();
                element[name] = JObject.FromObject(value);
                return value;
            }
            else
            {
                return default(T);
            }
        }

        internal StringStringDictionaryMapper GetStringMapper(JObject element, string name)
        {
            return new StringStringDictionaryMapper(element, name);
        }

        internal StringDictionaryMapper<T> GetStringMapper<T>(JObject element, string name, Func<JObject, T> factory)
            where T : class, IJsonObject
        {
            return new StringDictionaryMapper<T>(this, element, name, factory);
        }

        private T? GetNullableValue<T>(JObject element, string name, TryParseDelegate<T> parse, T? defaultValue)
            where T : struct
        {
            if (element == null)
            {
                throw this.Fail(name, TypeName<T>(), JTokenType.Undefined, ParseResult.NullSubject);
            }
            else if (element.TryGetValue(name, out JToken child))
            {
                var valueElement = child as JValue;
                if (valueElement != null && valueElement.Type == JTokenType.Null)
                {
                    return defaultValue;
                }
                else if (valueElement != null)
                {
                    var status = parse(name, valueElement, out T value);
                    if (status == ParseResult.Ok)
                    {
                        return value;
                    }
                    else if (status == ParseResult.Empty)
                    {
                        return defaultValue;
                    }
                    else
                    {
                        throw this.Fail(name, TypeName<T>(), child.Type, status);
                    }
                }
                else
                {
                    throw this.Fail(name, TypeName<T>(), child.Type, ParseResult.InvalidNodeType);
                }
            }
            else
            {
                return defaultValue;
            }
        }

        private T GetValue<T>(JObject element, string name, TryParseDelegate<T> parse)
            where T : struct
        {
            if (element == null)
            {
                throw this.Fail(name, TypeName<T>(), JTokenType.Undefined, ParseResult.NullSubject);
            }
            else if (element.TryGetValue(name, out JToken child))
            {
                var valueElement = child as JValue;
                if (valueElement != null && valueElement.Type == JTokenType.Null)
                {
                    throw this.Fail(name, TypeName<T>(), child.Type, ParseResult.ValueIsRequired);
                }
                else if (valueElement != null)
                {
                    var status = parse(name, valueElement, out T value);
                    if (status == ParseResult.Ok)
                    {
                        return value;
                    }
                    else
                    {
                        throw this.Fail(name, TypeName<T>(), child.Type, status);
                    }
                }
                else
                {
                    throw this.Fail(name, TypeName<T>(), child.Type, ParseResult.InvalidNodeType);
                }
            }
            else
            {
                throw this.Fail(name, TypeName<T>(), JTokenType.Undefined, ParseResult.ValueIsRequired);
            }
        }

        private Exception Fail(string name, string typeName, JTokenType tokenType, ParseResult status)
        {
            throw new InvalidOperationException($"{this.source}[{name}] should be a {typeName}, error code:{status}, token type: {tokenType}. ");
        }

        private Exception Fail(string message)
        {
            throw new InvalidOperationException(this.source + " " + message);
        }

        /// <summary>
        /// Sets a property value. Throws on null value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private T SetValueOrFail<T>(JObject element, string name, T? value)
            where T : struct
        {
            if (value != null)
            {
                element[name] = new JValue(value.Value);
                return value.Value;
            }
            else
            {
                throw this.Fail("Element[" + name + "] should be a DateTime as a String, got null value. ");
            }
        }

        private T SetObjectOrFail<T>(JObject element, string name, Func<JObject, T> factory)
            where T : class
        {
            if (factory != null)
            {
                var child = new JObject();
                var value = factory(child);
                element[name] = child;
                return value;
            }
            else
            {
                throw this.Fail("Element[" + name + "] should be a DateTime as a String, got null value. ");
            }
        }

        private static string TypeName<T>()
        {
            return typeof(T).Name;
        }

        private ParseResult Parse(string name, JValue node, out Guid result)
        {
            result = default(Guid);
            if (node.Type == JTokenType.Guid)
            {
                result = (Guid)node.Value;
                return ParseResult.Ok;
            }
            else if (node.Type == JTokenType.String)
            {
                var value = (string)node.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (Guid.TryParse(value, out result))
                    {
                        return ParseResult.Ok;
                    }
                    
                    return ParseResult.Invalid;
                }
                else
                {
                    return ParseResult.Empty;
                }
            }
            else
            {
                return ParseResult.InvalidNodeType;
            }
        }

        private ParseResult Parse(string name, JValue node, out Int32 result)
        {
            result = default(Int32);
            if (node.Type == JTokenType.Integer)
            {
                result = checked((Int32)(Int64)node.Value);
                return ParseResult.Ok;
            }
            else if (node.Type == JTokenType.String)
            {
                var value = (string)node.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                    {
                        return ParseResult.Ok;
                    }
                    
                    return ParseResult.Invalid;
                }
                else
                {
                    return ParseResult.Empty;
                }
            }
            else
            {
                return ParseResult.InvalidNodeType;
            }
        }

        private ParseResult Parse(string name, JValue node, out DateTime result)
        {
            result = default(DateTime);
            if (node.Type == JTokenType.String)
            {
                var value = (string)node.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
                    {
                        return ParseResult.Ok;
                    }
                    
                    return ParseResult.Invalid;
                }
                else
                {
                    return ParseResult.Empty;
                }
            }
            else
            {
                return ParseResult.InvalidNodeType;
            }
        }

        private ParseResult Parse(string name, JValue node, out bool result)
        {
            result = default(bool);
            if (node.Type == JTokenType.Boolean)
            {
                result = (bool)node.Value;
                return ParseResult.Ok;
            }
            else if (node.Type == JTokenType.String)
            {
                var value = (string)node.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (MyExtensions.TryParseBoolean(value, out result))
                    {
                        return ParseResult.Ok;
                    }
                    
                    return ParseResult.Invalid;
                }
                else
                {
                    return ParseResult.Empty;
                }
            }
            else
            {
                return ParseResult.InvalidNodeType;
            }
        }

        private delegate ParseResult TryParseDelegate<T>(string name, JValue node, out T result);

        private enum ParseResult
        {
            Invalid,
            Ok,
            Empty,
            InvalidNodeType,
            NullSubject,
            ValueIsRequired,
        }
        
        private sealed class JObjectList<T> : IList<T>
            where T : IJsonObject
        {
            private readonly JsonHelper helper;
            private readonly JArray elements;
            private readonly Func<JObject, T> factory;
            private readonly List<T> objects;

            public JObjectList(JsonHelper helper, JArray elements, Func<JObject, T> factory)
            {
                this.helper = helper;
                this.elements = elements;
                this.factory = factory;
                this.objects = new List<T>();

                foreach (var element in elements)
                {
                    if (element.Type == JTokenType.Comment)
                    {
                    }
                    else
                    {
                        this.objects.Add(factory((JObject)element));
                    }
                }
            }

            public T this[int index]
            {
                get { return this.objects[index]; }
                set { throw new NotImplementedException(); }
            }

            public int Count
            {
                get { return this.elements.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public void Add(T item)
            {
                this.elements.Add(item.Node);
                this.objects.Add(item);
            }

            public void Clear()
            {
                this.elements.Clear();
                this.objects.Clear();
            }

            public bool Contains(T item)
            {
                return this.objects.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    array[arrayIndex + i] = this[i];
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this.objects.GetEnumerator();
            }

            public int IndexOf(T item)
            {
                return this.objects.IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                this.elements.Insert(index, item.Node);
                this.objects.Insert(index, item);
            }

            public bool Remove(T item)
            {
                int removed = 0;
                for (int i = 0; i < this.objects.Count; i++)
                {
                    if (Object.ReferenceEquals(item, this.objects[i]))
                    {
                        this.objects.RemoveAt(i);
                        this.elements.RemoveAt(i);
                        i--;
                        removed++;
                    }
                }

                return removed > 0;
            }

            public void RemoveAt(int index)
            {
                this.objects.RemoveAt(index);
                this.elements.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.objects.GetEnumerator();
            }
        }

        public sealed class StringStringDictionaryMapper : IDictionary<string, string>
        {
            private static readonly JsonHelper json = new JsonHelper(nameof(StringStringDictionaryMapper));
            private JObject element;
            private string name;
            private JObject source;

            public StringStringDictionaryMapper(JObject element, string name)
            {
                this.element = element;
                this.name = name;
            }

            public string this[string key]
            {
                get { return json.GetString(this.Source, key); }
                set { json.SetValue(this.Source, key, value); }
            }

            public ICollection<string> Keys => throw new NotImplementedException();

            public ICollection<string> Values => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            private JObject Source
            {
                get { return this.source ?? (this.source = (JObject)this.element[this.name] ?? ((JObject)(this.element[this.name] = new JObject()))); }
            }

            public void Add(string key, string value)
            {
                throw new NotImplementedException();
            }

            public void Add(KeyValuePair<string, string> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                this.Source.RemoveAll();
            }

            public bool Contains(KeyValuePair<string, string> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(string key)
            {
                return this.Source.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                return this.Source.Remove(key);
            }

            public bool Remove(KeyValuePair<string, string> item)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out string value)
            {
                if (this.Source.ContainsKey(key))
                {
                    value = json.GetString(this.Source, key);
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public sealed class StringDictionaryMapper<T> : IDictionary<string, T>
            where T : class, IJsonObject
        {
            private static readonly JsonHelper json = new JsonHelper(nameof(StringDictionaryMapper<T>));

            // TODO: problem here is that we are re-instantiating elements each time the collection is accessed :-( we should keep track of T instances
            private readonly Dictionary<string, T> objects;
            private readonly JsonHelper helper;
            private readonly JObject element;
            private string name;
            private readonly Func<JObject, T> factory;
            private JObject source;

            public StringDictionaryMapper(JsonHelper helper, JObject element, string name, Func<JObject, T> factory)
            {
                this.element = element;
                this.name = name;
                this.factory = factory;
                this.helper = helper;
                this.factory = factory;
                this.objects = new Dictionary<string, T>();

                foreach (var entry in this.Source)
                {
                    if (element.Type == JTokenType.Comment)
                    {
                    }
                    else
                    {
                        this.objects.Add(entry.Key, factory((JObject)element));
                    }
                }
            }

            public T this[string key]
            {
                get { return this.objects[key]; }
                set
                {
                    this.Source[key] = value.Node;
                    this.objects[key] = value;
                }
            }

            public ICollection<string> Keys => this.objects.Keys;

            public ICollection<T> Values => this.objects.Values;

            public int Count => this.Source.Count;

            public bool IsReadOnly => false;

            private JObject Source
            {
                get { return this.source ?? (this.source = (JObject)this.element[this.name] ?? ((JObject)(this.element[this.name] = new JObject()))); }
            }

            public void Add(string key, T value)
            {
                if (this.objects.ContainsKey(key) || this.Source.ContainsKey(key))
                {
                    throw new ArgumentException("An entry with the same key already exists", nameof(key));
                }

                this.Source[key] = value.Node;
                this.objects[key] = value;
            }

            public void Add(KeyValuePair<string, T> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                this.Source.RemoveAll();
            }

            public bool Contains(KeyValuePair<string, T> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(string key)
            {
                return this.Source.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
            {
                int i = 0;
                foreach (var entry in this.Source)
                {
                    array[i++] = new KeyValuePair<string, T>(entry.Key, json.GetObject(this.Source, entry.Key, this.factory));
                }
            }

            public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                return this.Source.Remove(key);
            }

            public bool Remove(KeyValuePair<string, T> item)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out T value)
            {
                if (this.Source.ContainsKey(key))
                {
                    value = json.GetObject(this.Source, key, this.factory);
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
