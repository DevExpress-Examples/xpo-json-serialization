using DevExpress.Data.Filtering.Helpers;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XpoSerialization {
    public class XpoModelJsonConverter<T> : JsonConverter<T> where T : PersistentBase {
        readonly XPDictionary dictionary;
        public XpoModelJsonConverter(XPDictionary dictionary) {
            this.dictionary = dictionary;
        }
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, T theObject, JsonSerializerOptions options) {
            XPClassInfo classInfo = dictionary.GetClassInfo(typeof(T));
            writer.WriteStartObject();
            foreach(XPMemberInfo member in classInfo.Members) {
                if(CanSerialize(member)) {
                    writer.WritePropertyName(member.Name);
                    object propertyValue = member.GetValue(theObject);
                    if(propertyValue != null && member.ReferenceType != null && !member.IsAggregated && !EvaluatorProperty.GetIsThisProperty(member.Name)) {
                        propertyValue = member.ReferenceType.KeyProperty.GetValue(propertyValue);
                    }
                    JsonSerializer.Serialize(writer, propertyValue, options);
                }
            }
            writer.WriteEndObject();
        }
        static bool CanSerialize(XPMemberInfo member) {
            return member.IsPublic
                &&
                member.IsVisibleInDesignTime
                &&
                !member.IsCollection
                &&
                !EvaluatorProperty.GetIsThisProperty(member.Name);
        }
    }
    public class XpoModelJsonConverterFactory : JsonConverterFactory {
        readonly XPDictionary dictionary;
        public XpoModelJsonConverterFactory(XPDictionary dictionary) {
            this.dictionary = dictionary;
        }
        public override bool CanConvert(Type typeToConvert) {
            return typeof(PersistentBase).IsAssignableFrom(typeToConvert);
        }
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
            Type converterType = typeof(XpoModelJsonConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType, dictionary);
        }
    }
}
