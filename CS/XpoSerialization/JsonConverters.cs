using DevExpress.Xpo.Metadata;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevExpress.Xpo.Helpers {
    public class PersistentBaseConverter<T> :  JsonConverter<T> where T : PersistentBase {
        IServiceProvider _serviceProvider;
        public PersistentBaseConverter(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }
        UnitOfWork GetUnitOfWork() {
            return (UnitOfWork)_serviceProvider.GetService(typeof(UnitOfWork));
        }
        public override T Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) {
            UnitOfWork uow = GetUnitOfWork();
            if(reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            XPClassInfo classInfo = uow.GetClassInfo(typeToConvert);
            Dictionary<string, object> resultDict = CollectPropertyValues(ref reader, options, classInfo, uow);
            return (T)PopulateObjectProperties(null, resultDict, uow, classInfo);
        }
        static PersistentBase PopulateObjectProperties(PersistentBase persistentObject, Dictionary<string, object> propertyValues, UnitOfWork uow, XPClassInfo classInfo) {
            object keyValue;
            if(persistentObject == null && propertyValues.TryGetValue(classInfo.KeyProperty.Name, out keyValue)) {
                persistentObject = (PersistentBase)uow.GetObjectByKey(classInfo, keyValue);
            }
            if(persistentObject == null) persistentObject = (PersistentBase)classInfo.CreateNewObject(uow);
            foreach(KeyValuePair<string, object> pair in propertyValues) {
                XPMemberInfo memberInfo = classInfo.FindMember(pair.Key);
                if(memberInfo.IsReadOnly)
                    continue;
                if(memberInfo.ReferenceType != null) {
                    PopulateReferenceProperty(persistentObject, uow, pair.Value, memberInfo);
                } else {
                    PopulateScalarProperty(persistentObject, pair.Value, memberInfo);
                }
            }
            return persistentObject;
        }

        private static void PopulateScalarProperty(PersistentBase theObject, object theValue, XPMemberInfo memberInfo) {
            if(memberInfo == theObject.ClassInfo.OptimisticLockField) {
                SetOptimisticLockField(theObject, (int)theValue);
            } else {
                memberInfo.SetValue(theObject, theValue);
            }
        }

        private static void PopulateReferenceProperty(PersistentBase parent, UnitOfWork uow, object theValue, XPMemberInfo memberInfo) {
            if(memberInfo.IsAggregated) {
                PersistentBase propertyValue = (PersistentBase)memberInfo.GetValue(parent);
                propertyValue = PopulateObjectProperties(propertyValue, (Dictionary<string, object>)theValue, uow, memberInfo.ReferenceType);
                memberInfo.SetValue(parent, propertyValue);
            } else {
                memberInfo.SetValue(parent, uow.GetObjectByKey(memberInfo.MemberType, theValue));
            }
        }

        static Dictionary<string, object> CollectPropertyValues(ref Utf8JsonReader reader, JsonSerializerOptions options, XPClassInfo classInfo, UnitOfWork uow) {
            Dictionary<string, object> propertyValues = new Dictionary<string, object>();
            while(reader.Read()) {
                switch(reader.TokenType) {
                    case JsonTokenType.EndObject:
                        return propertyValues;
                    case JsonTokenType.PropertyName:
                        ReadPropertyValue(ref reader, options, classInfo, uow, propertyValues);
                        break;
                }
            }
            throw new JsonException();
        }
        private static void SkipObject(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            int startCount = 0;
            while(reader.Read()) {
                switch(reader.TokenType) {
                    case JsonTokenType.EndObject:
                        startCount -= 1;
                        if(startCount == 0)
                            return;
                        else
                            break;
                    case JsonTokenType.EndArray:
                        startCount -= 1;
                        if(startCount == 0)
                            return;
                        else
                            break;
                    case JsonTokenType.StartObject:
                        startCount += 1;
                        break;
                    case JsonTokenType.StartArray:
                        startCount += 1;
                        break;
                    default:
                        if(startCount == 0) return;
                        else break;
                }

            }
        }
        private static void ReadPropertyValue(ref Utf8JsonReader reader, JsonSerializerOptions options, XPClassInfo classInfo, UnitOfWork uow, Dictionary<string, object> propertyValues) {
            string propertyName = reader.GetString();
            var member = classInfo.FindMember(propertyName);
            if(member != null && CanSerializeProperty(member)) {
                reader.Read();
                if(member.IsCollection || member.IsNonAssociationList && !member.IsPersistent) {
                    SkipArray(ref reader);
                } else {
                    try {
                        if(member.ReferenceType == null)
                            propertyValues[propertyName] = JsonSerializer.Deserialize(ref reader, member.MemberType, options);
                        else {
                            if(member.IsAggregated) {
                                propertyValues[propertyName] = CollectPropertyValues(ref reader, options, member.ReferenceType, uow);
                            } else {
                                propertyValues[propertyName] = JsonSerializer.Deserialize(ref reader, member.ReferenceType.KeyProperty.MemberType, options);
                            }
                        }
                    } catch(JsonException) { throw new JsonException("BadJsonFormat"); }
                }
            } else SkipObject(ref reader, options);
        }

        private static void SkipArray(ref Utf8JsonReader reader) {
            int count = 1;
            while(true) {
                reader.Read();
                if(reader.TokenType == JsonTokenType.StartArray) count += 1;
                if(reader.TokenType == JsonTokenType.EndArray) count -= 1;
                if(count == 0) break;
            }
        }

        static void SetOptimisticLockField(PersistentBase obj, int newValue) {
            obj.ClassInfo.OptimisticLockField?.SetValue(obj, newValue);
            obj.ClassInfo.OptimisticLockFieldInDataLayer?.SetValue(obj, newValue);
        }
        public override void Write(
            Utf8JsonWriter writer,
            T Value,
            JsonSerializerOptions options) {
            if(writer.CurrentDepth > options.MaxDepth) throw new JsonException("Cycles are not supported");
            UnitOfWork uow = GetUnitOfWork();
            XPClassInfo classInfo = uow.GetClassInfo(Value);
            writer.WriteStartObject();
            foreach(var member in classInfo.Members) {
                if(member != null && CanSerializeProperty(member) && member.IsPublic && !member.IsCollection) { //ispersistent
                    object value = member.GetValue(Value);
                    writer.WritePropertyName(member.Name);
                    if(!typeof(PersistentBase).IsAssignableFrom(member.MemberType))
                        JsonSerializer.Serialize(writer, value, member.MemberType, options);
                    else if(member.IsAggregated)
                        JsonSerializer.Serialize(writer, value, options);
                    else {
                        if(value != null)
                            value = uow.GetKeyValue(value);
                        JsonSerializer.Serialize(writer, value, options);
                    }
                }
            }
            writer.WriteEndObject();
        }
        static bool CanSerializeProperty(XPMemberInfo member) {
            return member.Owner.ClassType != typeof(PersistentBase) && member.Owner.ClassType != typeof(XPBaseObject);
        }
        public override bool CanConvert(Type typeToConvert) {
            return typeof(PersistentBase).IsAssignableFrom(typeToConvert);
        }
    }
    public class PersistentBaseConverterFactory : JsonConverterFactory {
        readonly IServiceProvider serviceProvider;
        public PersistentBaseConverterFactory(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
        }
        public override bool CanConvert(Type typeToConvert) {
            return typeof(PersistentBase).IsAssignableFrom(typeToConvert);
        }
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
            Type converterType = typeof(PersistentBaseConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType, serviceProvider);
        }
    }
}