using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace UniJSON
{
    /// <summary>
    /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5
    /// </summary>
    public class JsonObjectValidator : IJsonSchemaValidator
    {
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.1
        /// </summary>
        public int MaxProperties
        {
            get; set;
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.2
        /// </summary>
        public int MinProperties
        {
            get; set;
        }

        HashSet<string> m_required = new HashSet<string>();
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.3
        /// </summary>
        public HashSet<string> Required
        {
            get { return m_required; }
        }

        Dictionary<string, JsonSchema> m_props;
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.4
        /// </summary>
        public Dictionary<string, JsonSchema> Properties
        {
            get
            {
                if (m_props == null)
                {
                    m_props = new Dictionary<string, JsonSchema>();
                }
                return m_props;
            }
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.5
        /// </summary>
        public string PatternProperties
        {
            get; private set;
        }

        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.6
        /// </summary>
        public JsonSchema AdditionalProperties
        {
            get; set;
        }

        Dictionary<string, string[]> m_depndencies;
        /// <summary>
        /// http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.5.7
        /// </summary>
        public Dictionary<string, string[]> Dependencies
        {
            get
            {
                if (m_depndencies == null)
                {
                    m_depndencies = new Dictionary<string, string[]>();
                }
                return m_depndencies;
            }
        }

        public void AddProperty(IFileSystemAccessor fs, string key, ListTreeNode<JsonValue> value)
        {
            var sub = new JsonSchema();
            sub.Parse(fs, value, key);

            if (Properties.ContainsKey(key))
            {
                if (sub.Validator != null)
                {
                    Properties[key].Validator.Merge(sub.Validator);
                }
            }
            else
            {
                Properties.Add(key, sub);
            }
        }

        public override int GetHashCode()
        {
            return 6;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as JsonObjectValidator;
            if (rhs == null)
            {
                return false;
            }

            if (Properties.Count != rhs.Properties.Count)
            {
                return false;
            }
            foreach (var pair in Properties)
            {
                JsonSchema value;
                if (rhs.Properties.TryGetValue(pair.Key, out value))
                {
#if true
                    if (!value.Equals(pair.Value))
                    {
                        Console.WriteLine(string.Format("{0} is not equals", pair.Key));
                        var l = pair.Value.Validator;
                        var r = value.Validator;
                        return false;
                    }
#else
                    // key name match
                    return true;
#endif
                }
                else
                {
                    return false;
                }
            }

            if (Required.Count != rhs.Required.Count)
            {
                return false;
            }
            if (!Required.OrderBy(x => x).SequenceEqual(rhs.Required.OrderBy(x => x)))
            {
                return false;
            }

            if (Dependencies.Count != rhs.Dependencies.Count)
            {
                return false;
            }
            foreach (var kv in Dependencies)
            {
                if (!kv.Value.OrderBy(x => x).SequenceEqual(rhs.Dependencies[kv.Key].OrderBy(x => x)))
                {
                    return false;
                }
            }

            if (AdditionalProperties == null
                && rhs.AdditionalProperties == null)
            {
                // ok
            }
            else if (AdditionalProperties == null)
            {
                return false;
            }
            else if (rhs.AdditionalProperties == null)
            {
                return false;
            }
            else
            {
                if (!AdditionalProperties.Equals(rhs.AdditionalProperties))
                {
                    return false;
                }
            }

            return true;
        }

        public void Merge(IJsonSchemaValidator obj)
        {
            var rhs = obj as JsonObjectValidator;
            if (rhs == null)
            {
                throw new ArgumentException();
            }

            foreach (var x in rhs.Properties)
            {
                if (this.Properties.ContainsKey(x.Key))
                {
                    this.Properties[x.Key] = x.Value;
                }
                else
                {
                    this.Properties.Add(x.Key, x.Value);
                }
            }

            foreach (var x in rhs.Required)
            {
                this.Required.Add(x);
            }

            if (rhs.AdditionalProperties != null)
            {
                if (AdditionalProperties != null)
                {
                    throw new NotImplementedException();
                }
                AdditionalProperties = rhs.AdditionalProperties;
            }
        }

        public bool FromJsonSchema(IFileSystemAccessor fs, string key, ListTreeNode<JsonValue> value)
        {
            switch (key)
            {
                case "maxProperties":
                    MaxProperties = value.GetInt32();
                    return true;

                case "minProperties":
                    MinProperties = value.GetInt32();
                    return true;

                case "required":
                    {
                        foreach (var req in value.ArrayItems())
                        {
                            m_required.Add(req.GetString());
                        }
                    }
                    return true;

                case "properties":
                    {
                        foreach (var prop in value.ObjectItems())
                        {
                            AddProperty(fs, prop.Key.GetString(), prop.Value);
                        }
                    }
                    return true;

                case "patternProperties":
                    PatternProperties = value.GetString();
                    return true;

                case "additionalProperties":
                    {
                        var sub = new JsonSchema();
                        sub.Parse(fs, value, "additionalProperties");
                        AdditionalProperties = sub;
                    }
                    return true;

                case "dependencies":
                    {
                        foreach (var kv in value.ObjectItems())
                        {
                            Dependencies.Add(kv.Key.GetString(), kv.Value.ArrayItems().Select(x => x.GetString()).ToArray());
                        }
                    }
                    return true;

                case "propertyNames":
                    return true;
            }

            return false;
        }

        public void ToJsonScheama(IFormatter f)
        {
            f.Key("type"); f.Value("object");
            if (Properties.Count > 0)
            {
                f.Key("properties");
                f.BeginMap(Properties.Count);
                foreach (var kv in Properties)
                {
                    f.Key(kv.Key);
                    kv.Value.ToJson(f);
                }
                f.EndMap();
            }
        }

        static class GenericFieldView<T>
        {
            public static FieldInfo[] GetFields()
            {
                var t = typeof(T);
                return t.GetFields(BindingFlags.Instance | BindingFlags.Public);
            }

            public static void CreateFieldProcessors<G, D>(
                Func<FieldInfo, D> creator,
                Dictionary<string, D> processors
                )
            {
                foreach (var fi in GetFields())
                {
                    processors.Add(fi.Name, creator(fi));
                }
            }
        }

        internal class ValidationResult
        {
            public bool IsIgnorable;
            public JsonSchemaValidationException Ex;
        }

        public static class GenericValidator<T>
        {
            class ObjectValidator
            {
                delegate JsonSchemaValidationException FieldValidator(
                    JsonSchema s, JsonSchemaValidationContext c, T o, out bool isIgnorable);

                Dictionary<string, FieldValidator> m_validators;

                static FieldValidator CreateFieldValidator(FieldInfo fi)
                {
                    var mi = typeof(ObjectValidator).GetMethod("_CreateFieldValidator",
                        BindingFlags.Static | BindingFlags.NonPublic)
                        ;
                    var g = mi.MakeGenericMethod(fi.FieldType);
                    return GenericInvokeCallFactory.StaticFunc<FieldInfo, FieldValidator>(g)(fi);
                }

                static FieldValidator _CreateFieldValidator<U>(FieldInfo fi)
                {
                    var getter = (Func<T, U>)((t) => (U)fi.GetValue(t));

                    return (JsonSchema s, JsonSchemaValidationContext c, T o, out bool isIgnorable) =>
                    {
                        var v = s.Validator;
                        using (c.Push(fi.Name))
                        {
                            var field = getter(o);
                            var ex = v.Validate(c, field);

                            isIgnorable = ex != null && s.IsExplicitlyIgnorableValue(field);

                            return ex;
                        }
                    };
                }

                public static void TellIl2cppToGenerateRequiredGenericMethodCodes()
                {
                    GenericValidator<UnityEngine.Color>.ObjectValidator._CreateFieldValidator<Single>(null);
                    GenericValidator<UnityEngine.Color32>.ObjectValidator._CreateFieldValidator<Byte>(null);
                    GenericValidator<UnityEngine.Rect>.ObjectValidator._CreateFieldValidator<Single>(null);
                    GenericValidator<UnityEngine.RectInt>.ObjectValidator._CreateFieldValidator<Int32>(null);
                    GenericValidator<UnityEngine.RectOffset>.ObjectValidator._CreateFieldValidator<Int32>(null);
                    GenericValidator<UnityEngine.Vector2>.ObjectValidator._CreateFieldValidator<Single>(null);
                    GenericValidator<UnityEngine.Vector2Int>.ObjectValidator._CreateFieldValidator<Int32>(null);
                    GenericValidator<UnityEngine.Vector3>.ObjectValidator._CreateFieldValidator<Single>(null);
                    GenericValidator<UnityEngine.Vector3Int>.ObjectValidator._CreateFieldValidator<Int32>(null);
                    GenericValidator<UnityEngine.Vector4>.ObjectValidator._CreateFieldValidator<Single>(null);

                    throw new System.InvalidOperationException("This method is used for AOT code generation only. Do not call it at runtime.");
                }

                public ObjectValidator()
                {
                    var validators = new Dictionary<string, FieldValidator>();
                    GenericFieldView<T>.CreateFieldProcessors<ObjectValidator, FieldValidator>(
                        CreateFieldValidator, validators);

                    m_validators = validators;
                }

                public JsonSchemaValidationException ValidateProperty(
                    HashSet<string> required,
                    KeyValuePair<string, JsonSchema> property,
                    JsonSchemaValidationContext c,
                    T o,
                    out bool isIgnorable
                    )
                {
                    var fieldName = property.Key;
                    var schema = property.Value;

                    isIgnorable = false;

                    FieldValidator fv;
                    if (m_validators.TryGetValue(fieldName, out fv))
                    {
                        var isRequired = required != null && required.Contains(fieldName);

                        bool isMemberIgnorable;
                        var ex = fv(schema, c, o, out isMemberIgnorable);
                        if (ex != null)
                        {
                            isIgnorable = !isRequired && isMemberIgnorable;

                            if (isRequired // required fields must be checked
                                || c.EnableDiagnosisForNotRequiredFields)
                            {
                                return ex;
                            }
                        }
                    }

                    return null;
                }

                public JsonSchemaValidationException Validate(
                    HashSet<string> required,
                    Dictionary<string, JsonSchema> properties,
                    JsonSchemaValidationContext c, T o)
                {
                    foreach (var kv in properties)
                    {
                        bool isIgnorable;
                        var ex = ValidateProperty(required, kv, c, o, out isIgnorable);
                        if (ex != null && !isIgnorable)
                        {
                            return ex;
                        }
                    }

                    return null;
                }

                public void ValidationResults
                    (HashSet<string> required,
                    Dictionary<string, JsonSchema> properties,
                    JsonSchemaValidationContext c, T o,
                    Dictionary<string, ValidationResult> results)
                {
                    foreach (var kv in properties)
                    {
                        bool isIgnorable;
                        var ex = ValidateProperty(required, kv, c, o, out isIgnorable);

                        results.Add(kv.Key, new ValidationResult {
                            IsIgnorable = isIgnorable,
                            Ex = ex,
                        });
                    }
                }
            }

            static ObjectValidator s_validator;

            static void prepareValidator()
            {
                if (s_validator == null)
                {
                    s_validator = new ObjectValidator();
                }
            }

            public static JsonSchemaValidationException Validate(HashSet<string> required,
                Dictionary<string, JsonSchema> properties,
                JsonSchemaValidationContext c, T o)
            {
                prepareValidator();
                return s_validator.Validate(required, properties, c, o);
            }

            internal static void ValidationResults(HashSet<string> required,
                Dictionary<string, JsonSchema> properties,
                JsonSchemaValidationContext c, T o,
                Dictionary<string, ValidationResult> results)
            {
                prepareValidator();
                s_validator.ValidationResults(required, properties, c, o, results);
            }
        }

        public JsonSchemaValidationException Validate<T>(JsonSchemaValidationContext c, T o)
        {
            if (o == null)
            {
                return new JsonSchemaValidationException(c, "null");
            }

            if (Properties.Count < MinProperties)
            {
                return new JsonSchemaValidationException(c, "no properties");
            }

            return GenericValidator<T>.Validate(Required, Properties, c, o);
        }

        static class GenericSerializer<T>
        {
            class Serializer
            {
                delegate void FieldSerializer(JsonSchema s, JsonSchemaValidationContext c, IFormatter f, T o,
                    Dictionary<string, ValidationResult> vRes, string[] deps);

                Dictionary<string, FieldSerializer> m_serializers;

                static FieldSerializer CreateFieldSerializer(FieldInfo fi)
                {
                    var mi = typeof(Serializer).GetMethod("_CreateFieldSerializer",
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var g = mi.MakeGenericMethod(fi.FieldType);
                    return GenericInvokeCallFactory.StaticFunc<FieldInfo, FieldSerializer>(g)(fi);
                }

                static FieldSerializer _CreateFieldSerializer<U>(FieldInfo fi)
                {
                    Func<T, U> getter = t =>
                    {
                        return (U)fi.GetValue(t);
                    };

                    return (s, c, f, o, vRes, deps) =>
                    {
                        var v = s.Validator;
                        var field = getter(o);

                        if (vRes[fi.Name].Ex != null)
                        {
                            return;
                        }

                        if (deps != null)
                        {
                            foreach(var dep in deps)
                            {
                                if (vRes[dep].Ex != null)
                                {
                                    return;
                                }
                            }
                        }

                        f.Key(fi.Name);
                        v.Serialize(f, c, field);
                    };
                }

                public static void TellIl2cppToGenerateRequiredGenericMethodCodes()
                {
                    GenericSerializer<UnityEngine.Color>.Serializer._CreateFieldSerializer<Single>(null);
                    GenericSerializer<UnityEngine.Color32>.Serializer._CreateFieldSerializer<Byte>(null);
                    GenericSerializer<UnityEngine.Rect>.Serializer._CreateFieldSerializer<Single>(null);
                    GenericSerializer<UnityEngine.RectInt>.Serializer._CreateFieldSerializer<Int32>(null);
                    GenericSerializer<UnityEngine.RectOffset>.Serializer._CreateFieldSerializer<Int32>(null);
                    GenericSerializer<UnityEngine.Vector2>.Serializer._CreateFieldSerializer<Single>(null);
                    GenericSerializer<UnityEngine.Vector2Int>.Serializer._CreateFieldSerializer<Int32>(null);
                    GenericSerializer<UnityEngine.Vector3>.Serializer._CreateFieldSerializer<Single>(null);
                    GenericSerializer<UnityEngine.Vector3Int>.Serializer._CreateFieldSerializer<Int32>(null);
                    GenericSerializer<UnityEngine.Vector4>.Serializer._CreateFieldSerializer<Single>(null);

                    throw new System.InvalidOperationException("This method is used for AOT code generation only. Do not call it at runtime.");
                }

                public Serializer()
                {
                    var serializers = new Dictionary<string, FieldSerializer>();
                    GenericFieldView<T>.CreateFieldProcessors<Serializer, FieldSerializer>(
                        CreateFieldSerializer, serializers);

                    m_serializers = serializers;
                }

                public void Serialize(JsonObjectValidator objectValidator,
                    IFormatter f, JsonSchemaValidationContext c, T o)
                {
                    // Validates fields
                    var validationResults = new Dictionary<string, ValidationResult>();
                    GenericValidator<T>.ValidationResults(
                        objectValidator.Required, objectValidator.Properties,
                        c, o, validationResults);

                    // Serialize fields
                    f.BeginMap(objectValidator.Properties.Count());
                    foreach (var property in objectValidator.Properties)
                    {
                        var fieldName = property.Key;
                        var schema = property.Value;

                        string[] deps = null;
                        objectValidator.Dependencies.TryGetValue(fieldName, out deps);

                        FieldSerializer fs;
                        if (m_serializers.TryGetValue(fieldName, out fs))
                        {
                            fs(schema, c, f, o, validationResults, deps);
                        }
                    }
                    f.EndMap();
                }
            }

            static FieldInfo[] s_fields;
            static Serializer s_serializer;

            public static void Serialize(JsonObjectValidator objectValidator,
                    IFormatter f, JsonSchemaValidationContext c, T value)
            {
                if (s_serializer == null)
                {
                    s_serializer = new Serializer();
                }

                s_serializer.Serialize(objectValidator, f, c, value);
            }
        }

        public void Serialize<T>(IFormatter f, JsonSchemaValidationContext c, T value)
        {
            GenericSerializer<T>.Serialize(this, f, c, value);
        }

        static class GenericDeserializer<S, T>
            where S : IListTreeItem, IValue<S>
        {
            delegate T Deserializer(ListTreeNode<S> src);

            static Deserializer s_d;

            delegate void FieldSetter(ListTreeNode<S> s, object o);
            static FieldSetter GetFieldDeserializer<U>(FieldInfo fi)
            {
                return (s, o) =>
                {
                    var u = default(U);
                    s.Deserialize(ref u);
                    fi.SetValue(o, u);
                };
            }

            static U DeserializeField<U>(JsonSchema prop, ListTreeNode<S> s)
            {
                var u = default(U);
                prop.Validator.Deserialize(s, ref u);
                return u;
            }

            public static void Deserialize(ListTreeNode<S> src, ref T dst, Dictionary<string, JsonSchema> props)
            {
                if (s_d == null)
                {
                    var target = typeof(T);

                    var fields = target.GetFields(BindingFlags.Instance | BindingFlags.Public);
                    var fieldDeserializers = fields.ToDictionary(x => Utf8String.From(x.Name), x =>
                    {
                        /*
                        var mi = typeof(GenericDeserializer<T>).GetMethod("GetFieldDeserializer",
                            BindingFlags.Static | BindingFlags.NonPublic);
                        var g = mi.MakeGenericMethod(x.FieldType);
                        return (FieldSetter)g.Invoke(null, new object[] { x });
                        */
                        JsonSchema prop;
                        if (!props.TryGetValue(x.Name, out prop))
                        {
                            return null;
                        }

                        var mi = typeof(GenericDeserializer<S, T>).GetMethod("DeserializeField",
                            BindingFlags.Static | BindingFlags.NonPublic);
                        var g = mi.MakeGenericMethod(x.FieldType);

                        return (FieldSetter)((s, o) =>
                        {
                            var f = g.Invoke(null, new object[] { prop, s });
                            x.SetValue(o, f);
                        });
                    });

                    s_d = (ListTreeNode<S> s) =>
                    {
                        if (!s.IsMap())
                        {
                            throw new ArgumentException(s.Value.ValueType.ToString());
                        }

                        // boxing
                        var t = (object)Activator.CreateInstance<T>();
                        foreach (var kv in s.ObjectItems())
                        {
                            FieldSetter setter;
                            if (fieldDeserializers.TryGetValue(kv.Key.GetUtf8String(), out setter))
                            {
                                if (setter != null)
                                {
                                    setter(kv.Value, t);
                                }
                            }
                        }
                        return (T)t;
                    };

                }
                dst = s_d(src);
            }
        }

        public void Deserialize<T, U>(ListTreeNode<T> src, ref U dst)
            where T : IListTreeItem, IValue<T>
        {
            GenericDeserializer<T, U>.Deserialize(src, ref dst, Properties);
        }
    }

    public class Il2cppSupport
    {
        public static void TellIl2cppToGenerateRequiredGenericMethodCodes()
        {
            new JsonFormatter().Serialize(default(bool));
            new JsonFormatter().Serialize(default(bool[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<bool>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<bool[]>));
            new JsonFormatter().Serialize(default(byte));
            new JsonFormatter().Serialize(default(byte[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<byte>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<byte[]>));
            new JsonFormatter().Serialize(default(sbyte));
            new JsonFormatter().Serialize(default(sbyte[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<sbyte>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<sbyte[]>));
            new JsonFormatter().Serialize(default(char));
            new JsonFormatter().Serialize(default(char[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<char>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<char[]>));
            new JsonFormatter().Serialize(default(decimal));
            new JsonFormatter().Serialize(default(decimal[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<decimal>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<decimal[]>));
            new JsonFormatter().Serialize(default(double));
            new JsonFormatter().Serialize(default(double[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<double>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<double[]>));
            new JsonFormatter().Serialize(default(float));
            new JsonFormatter().Serialize(default(float[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<float>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<float[]>));
            new JsonFormatter().Serialize(default(int));
            new JsonFormatter().Serialize(default(int[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<int>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<int[]>));
            new JsonFormatter().Serialize(default(uint));
            new JsonFormatter().Serialize(default(uint[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<uint>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<uint[]>));
            new JsonFormatter().Serialize(default(long));
            new JsonFormatter().Serialize(default(long[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<long>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<long[]>));
            new JsonFormatter().Serialize(default(ulong));
            new JsonFormatter().Serialize(default(ulong[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<ulong>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<ulong[]>));
            new JsonFormatter().Serialize(default(object));
            new JsonFormatter().Serialize(default(object[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<object>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<object[]>));
            new JsonFormatter().Serialize(default(short));
            new JsonFormatter().Serialize(default(short[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<short>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<short[]>));
            new JsonFormatter().Serialize(default(ushort));
            new JsonFormatter().Serialize(default(ushort[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<ushort>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<ushort[]>));
            new JsonFormatter().Serialize(default(string));
            new JsonFormatter().Serialize(default(string[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<string>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<string[]>));
            new JsonFormatter().Serialize(default(System.Boolean));
            new JsonFormatter().Serialize(default(System.Boolean[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Boolean>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Boolean[]>));
            new JsonFormatter().Serialize(default(System.Byte));
            new JsonFormatter().Serialize(default(System.Byte[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Byte>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Byte[]>));
            new JsonFormatter().Serialize(default(System.SByte));
            new JsonFormatter().Serialize(default(System.SByte[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.SByte>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.SByte[]>));
            new JsonFormatter().Serialize(default(System.Char));
            new JsonFormatter().Serialize(default(System.Char[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Char>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Char[]>));
            new JsonFormatter().Serialize(default(System.Decimal));
            new JsonFormatter().Serialize(default(System.Decimal[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Decimal>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Decimal[]>));
            new JsonFormatter().Serialize(default(System.Double));
            new JsonFormatter().Serialize(default(System.Double[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Double>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Double[]>));
            new JsonFormatter().Serialize(default(System.Single));
            new JsonFormatter().Serialize(default(System.Single[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Single>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Single[]>));
            new JsonFormatter().Serialize(default(System.Int32));
            new JsonFormatter().Serialize(default(System.Int32[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Int32>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Int32[]>));
            new JsonFormatter().Serialize(default(System.UInt32));
            new JsonFormatter().Serialize(default(System.UInt32[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.UInt32>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.UInt32[]>));
            new JsonFormatter().Serialize(default(System.Int64));
            new JsonFormatter().Serialize(default(System.Int64[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Int64>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Int64[]>));
            new JsonFormatter().Serialize(default(System.UInt64));
            new JsonFormatter().Serialize(default(System.UInt64[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.UInt64>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.UInt64[]>));
            new JsonFormatter().Serialize(default(System.Object));
            new JsonFormatter().Serialize(default(System.Object[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Object>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Object[]>));
            new JsonFormatter().Serialize(default(System.Int16));
            new JsonFormatter().Serialize(default(System.Int16[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Int16>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.Int16[]>));
            new JsonFormatter().Serialize(default(System.UInt16));
            new JsonFormatter().Serialize(default(System.UInt16[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.UInt16>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.UInt16[]>));
            new JsonFormatter().Serialize(default(System.String));
            new JsonFormatter().Serialize(default(System.String[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.String>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<System.String[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Color));
            new JsonFormatter().Serialize(default(UnityEngine.Color[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Color>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Color[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Color32));
            new JsonFormatter().Serialize(default(UnityEngine.Color32[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Color32>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Color32[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Rect));
            new JsonFormatter().Serialize(default(UnityEngine.Rect[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Rect>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Rect[]>));
            new JsonFormatter().Serialize(default(UnityEngine.RectInt));
            new JsonFormatter().Serialize(default(UnityEngine.RectInt[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.RectInt>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.RectInt[]>));
            new JsonFormatter().Serialize(default(UnityEngine.RectOffset));
            new JsonFormatter().Serialize(default(UnityEngine.RectOffset[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.RectOffset>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.RectOffset[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Vector2));
            new JsonFormatter().Serialize(default(UnityEngine.Vector2[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector2>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector2[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Vector2Int));
            new JsonFormatter().Serialize(default(UnityEngine.Vector2Int[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector2Int>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector2Int[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Vector3));
            new JsonFormatter().Serialize(default(UnityEngine.Vector3[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector3>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector3[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Vector3Int));
            new JsonFormatter().Serialize(default(UnityEngine.Vector3Int[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector3Int>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector3Int[]>));
            new JsonFormatter().Serialize(default(UnityEngine.Vector4));
            new JsonFormatter().Serialize(default(UnityEngine.Vector4[]));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector4>));
            new JsonFormatter().SerializeArray(default(System.Collections.Generic.IEnumerable<UnityEngine.Vector4[]>));

            throw new System.InvalidOperationException("This method is used for AOT code generation only. Do not call it at runtime.");
        }
    }
}
