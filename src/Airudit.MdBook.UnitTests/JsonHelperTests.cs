
namespace Airudit.MdBook.UnitTests
{
    using Airudit.MdBook.Core.Internals;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class JsonHelperTests
    {
        private static readonly JsonHelper json = new JsonHelper("X");

        [Fact]
        public void GetDateTime_NoProperty()
        {
            var prop = "DateCreatedUtc";
            var root = new JObject();
            var result = json.GetDateTime(root, prop, DateTime.MaxValue);
            Assert.Equal(DateTime.MaxValue, result);
        }

        [Fact]
        public void GetDateTime_NullProperty()
        {
            var prop = "DateCreatedUtc";
            var root = new JObject();
            root[prop] = new JValue(default(object));

            var result = json.GetDateTime(root, prop, DateTime.MaxValue);
            Assert.Equal(DateTime.MaxValue, result);
        }

        [Fact]
        public void GetDateTime_ValidProperty()
        {
            var prop = "DateCreatedUtc";
            var value = new DateTime(2020, 1, 1, 13, 52, 56, 123, DateTimeKind.Utc);
            var root = new JObject();
            root[prop] = new JValue(value.ToString("o"));

            var result = json.GetDateTime(root, prop, DateTime.MaxValue);
            Assert.Equal(value, result);
        }

        [Fact]
        public void GetDateTime_InvalidProperty_Format()
        {
            var prop = "DateCreatedUtc";
            var root = new JObject();
            root[prop] = new JValue("azerty");

            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var result = json.GetDateTime(root, prop, DateTime.MaxValue);
                });
        }

        [Fact]
        public void GetDateTime_InvalidProperty_Object()
        {
            var prop = "DateCreatedUtc";
            var root = new JObject();
            root[prop] = new JObject();

            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var result = json.GetDateTime(root, prop, DateTime.MaxValue);
                });
        }

        #region String

        [Fact]
        public void GetString_NoProperty()
        {
            var prop = "Machine";
            var root = new JObject();
            var result = json.GetString(root, prop);
            Assert.Equal(default(string), result);
        }

        [Fact]
        public void GetString_NullProperty()
        {
            var prop = "Machine";
            var root = new JObject();
            root[prop] = new JValue(default(object));

            var result = json.GetString(root, prop);
            Assert.Equal(default(string), result);
        }

        [Fact]
        public void GetString_ValidProperty()
        {
            var prop = "Machine";
            var value = "azerty";
            var root = new JObject();
            root[prop] = new JValue(value);

            var result = json.GetString(root, prop);
            Assert.Equal(value, result);
        }

        [Fact]
        public void GetString_InvalidProperty_Integer()
        {
            var prop = "Machine";
            var value = 123;
            var root = new JObject();
            root[prop] = new JValue(value);

            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var result = json.GetString(root, prop);
                });
        }

        [Fact]
        public void GetString_InvalidProperty_Object()
        {
            var prop = "Machine";
            var root = new JObject();
            root[prop] = new JObject();

            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var result = json.GetString(root, prop);
                });
        }

        #endregion

        [Fact]
        public void JList_Initialize()
        {
            var rootElement = new JObject();
            {
                var array = new JArray();
                rootElement.Add("Tasks", array);
                array.Add(new JObject());
                array.Add(new JObject());
                foreach (JObject item in array)
                {
                    item.Add(new JProperty("Name", "a"));
                }
            }

            var root = new SampleObject1(rootElement);
            Assert.NotNull(root.Tasks);
            Assert.Same(root.Tasks, root.Tasks);
            Assert.Equal(2, root.Tasks.Count);
            Assert.NotNull(root.Tasks[0]);
            Assert.NotNull(root.Tasks[1]);
            Assert.Same(root.Tasks[0], root.Tasks[0]);
            Assert.Same(root.Tasks[1], root.Tasks[1]);
        }

        #region Guid

        [Fact]
        public void GetGuid_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetGuid(root, prop));
        }

        [Fact]
        public void GetGuid_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            Assert.Throws<InvalidOperationException>(() => json.GetGuid(root, prop));
        }

        [Fact]
        public void GetGuid_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            var id = new Guid("{A84CA43B-489D-43E3-A038-5D06D0AFE0EE}");
            root[prop] = new JValue(id.ToString());
            var result = json.GetGuid(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetGuid_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetGuid(root, prop));
        }

        [Fact]
        public void GetGuid_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetGuid(root, prop));
        }

        [Fact]
        public void GetNullableGuid_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            var result = json.GetNullableGuid(root, prop);
            Assert.Equal(default(Guid?), result);
        }

        [Fact]
        public void GetNullableGuid_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            var result = json.GetNullableGuid(root, prop);
            Assert.Equal(default(Guid?), result);
        }

        [Fact]
        public void GetNullableGuid_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            var id = new Guid("{A84CA43B-489D-43E3-A038-5D06D0AFE0EE}");
            root[prop] = new JValue(id.ToString());
            var result = json.GetNullableGuid(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetNullableGuid_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetGuid(root, prop));
        }

        [Fact]
        public void GetNullableGuid_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetGuid(root, prop));
        }

        #endregion

        #region Int32

        [Fact]
        public void GetInt32_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetInt32(root, prop));
        }

        [Fact]
        public void GetInt32_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            Assert.Throws<InvalidOperationException>(() => json.GetInt32(root, prop));
        }

        [Fact]
        public void GetInt32_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Int32 id = 42;
            root[prop] = new JValue(id.ToString());
            var result = json.GetInt32(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetInt32_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetInt32(root, prop));
        }

        [Fact]
        public void GetInt32_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetInt32(root, prop));
        }

        [Fact]
        public void GetNullableInt32_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            var result = json.GetNullableInt32(root, prop);
            Assert.Equal(default(Int32?), result);
        }

        [Fact]
        public void GetNullableInt32_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            var result = json.GetNullableInt32(root, prop);
            Assert.Equal(default(Int32?), result);
        }

        [Fact]
        public void GetNullableInt32_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Int32 id = 42;
            root[prop] = new JValue(id.ToString());
            var result = json.GetNullableInt32(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetNullableInt32_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetInt32(root, prop));
        }

        [Fact]
        public void GetNullableInt32_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetInt32(root, prop));
        }

        #endregion

        #region Double

        [Fact]
        public void GetDouble_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetDouble(root, prop));
        }

        [Fact]
        public void GetDouble_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            Assert.Throws<InvalidOperationException>(() => json.GetDouble(root, prop));
        }

        [Fact]
        public void GetDouble_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Double id = 42.00001;
            root[prop] = new JValue(id);
            var result = json.GetDouble(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetDouble_StringValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Double id = 42.00001;
            root[prop] = new JValue(id.ToInvariantString());
            var result = json.GetDouble(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetDouble_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetDouble(root, prop));
        }

        [Fact]
        public void GetDouble_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetDouble(root, prop));
        }

        [Fact]
        public void GetNullableDouble_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            var result = json.GetNullableDouble(root, prop);
            Assert.Equal(default(Double?), result);
        }

        [Fact]
        public void GetNullableDouble_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            var result = json.GetNullableDouble(root, prop);
            Assert.Equal(default(Double?), result);
        }

        [Fact]
        public void GetNullableDouble_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Double id = 42.00001;
            root[prop] = new JValue(id);
            var result = json.GetNullableDouble(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetNullableDouble_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetDouble(root, prop));
        }

        [Fact]
        public void GetNullableDouble_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetDouble(root, prop));
        }

        #endregion

        #region Boolean

        [Fact]
        public void GetBoolean_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetBoolean(root, prop));
        }

        [Fact]
        public void GetBoolean_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            Assert.Throws<InvalidOperationException>(() => json.GetBoolean(root, prop));
        }

        [Fact]
        public void GetBoolean_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Boolean id = true;
            root[prop] = new JValue(id.ToString());
            var result = json.GetBoolean(root, prop);
            Assert.Equal(id, result); 
        }

        [Fact]
        public void GetBoolean_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetBoolean(root, prop));
        }

        [Fact]
        public void GetBoolean_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetBoolean(root, prop));
        }

        [Fact]
        public void GetNullableBoolean_NoProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            var result = json.GetNullableBoolean(root, prop);
            Assert.Equal(default(Boolean?), result);
        }

        [Fact]
        public void GetNullableBoolean_NullProperty()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            var result = json.GetNullableBoolean(root, prop);
            Assert.Equal(default(Boolean?), result);
        }

        [Fact]
        public void GetNullableBoolean_ValidValue()
        {
            var prop = "ExternalId";
            var root = new JObject();
            Boolean id = true;
            root[prop] = new JValue(id.ToString());
            var result = json.GetNullableBoolean(root, prop);
            Assert.Equal(id, result);
        }

        [Fact]
        public void GetNullableBoolean_InvalidValue_Format()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JValue("pouet");
            Assert.Throws<InvalidOperationException>(() => json.GetBoolean(root, prop));
        }

        [Fact]
        public void GetNullableBoolean_InvalidValue_Type()
        {
            var prop = "ExternalId";
            var root = new JObject();
            root[prop] = new JObject();
            Assert.Throws<InvalidOperationException>(() => json.GetBoolean(root, prop));
        }

        #endregion

        #region StringArray

        [Fact]
        public void GetStringArray_Null()
        {
            var prop = "ExternalIds";
            var root = new JObject();
            root[prop] = JValue.CreateNull();
            var result = json.GetStringArray(root, prop);
            Assert.Null(result);
        }

        [Fact]
        public void GetStringArray_Empty()
        {
            var prop = "ExternalIds";
            var root = new JObject();
            root[prop] = new JArray();
            var result = json.GetStringArray(root, prop);
            Assert.IsType<string[]>(result);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetStringArray_Two()
        {
            var prop = "ExternalIds";
            var root = new JObject();
            root[prop] = new JArray("a", "b");
            var result = json.GetStringArray(root, prop);
            Assert.IsType<string[]>(result);
            Assert.NotNull(result);
            Assert.Collection(
                result,
                x => Assert.Equal("a", x),
                x => Assert.Equal("b", x));
        }

        [Fact]
        public void SetStringArray_Null()
        {
            var prop = "ExternalIds";
            var root = new JObject();
            json.SetStringArray(root, prop, null);
            var result = json.GetStringArray(root, prop);
            Assert.Null(result);
        }

        [Fact]
        public void SetStringArray_Empty()
        {
            var prop = "ExternalIds";
            var root = new JObject();
            json.SetStringArray(root, prop, Array.Empty<string>());
            var result = json.GetStringArray(root, prop);
            Assert.IsType<string[]>(result);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void SetStringArray_Two()
        {
            var prop = "ExternalIds";
            var root = new JObject();
            json.SetStringArray(root, prop, new string[] { "a", "b", });
            var result = json.GetStringArray(root, prop);
            Assert.IsType<string[]>(result);
            Assert.NotNull(result);
            Assert.Collection(
                result,
                x => Assert.Equal("a", x),
                x => Assert.Equal("b", x));
        }

        #endregion

        public class SampleObject1 : IJsonObject
        {
            private static readonly JsonHelper json = new JsonHelper("SampleObject1");
            private readonly JObject element;
            private IList<SampleObject2> tasks;

            public SampleObject1(JObject element)
            {
                this.element = element;
            }

            public JObject Node
            {
                get => this.element;
                set => throw new NotImplementedException();
            }

            public IList<SampleObject2> Tasks
            {
                get { return this.tasks ?? (this.tasks = json.GetList<SampleObject2>(this.element, nameof(this.Tasks), x => new SampleObject2(x))); }
            }
        }

        public class SampleObject2 : IJsonObject
        {
            private static readonly JsonHelper json = new JsonHelper("SampleObject2");
            private readonly JObject element;

            public SampleObject2(JObject element)
            {
                this.element = element;
            }

            public JObject Node
            {
                get => this.element;
                set => throw new NotImplementedException();
            }

            public string Name
            {
                get { return json.GetString(this.element, nameof(this.Name)); }
                set { this.element[nameof(this.Name)] = value; }
            }
        }
    }
}
