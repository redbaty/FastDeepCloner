﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace FastDeepCloner
{
    internal class ClonerShared
    {
        private readonly SafeValueType<string, object> _alreadyCloned = new SafeValueType<string, object>();
        private readonly FastDeepClonerSettings _settings;
        private readonly ICollection<PropertyInfo> _ignoredPropertyInfos;

        internal ClonerShared(ICollection<PropertyInfo> ignoredPropertyInfos, FieldType fieldType) : this(fieldType)
        {
            _ignoredPropertyInfos = ignoredPropertyInfos;
        }

        internal ClonerShared(ICollection<PropertyInfo> ignoredPropertyInfos, FastDeepClonerSettings settings) : this(settings)
        {
            _ignoredPropertyInfos = ignoredPropertyInfos;
        }

        internal ClonerShared(FieldType fieldType)
        {
            _settings = new FastDeepClonerSettings {FieldType = fieldType};
        }

        internal ClonerShared(FastDeepClonerSettings settings)
        {
            if (settings != null)
                _settings = settings;
            else _settings = new FastDeepClonerSettings() {FieldType = FieldType.PropertyInfo};
        }

        private object ReferenceTypeClone(Dictionary<string, IFastDeepClonerProperty> properties, Type primaryType,
            object objectToBeCloned, object appendToValue = null)
        {
            var identifier = objectToBeCloned.GetFastDeepClonerIdentifier();
            if (identifier != null && _alreadyCloned.ContainsKey(identifier))
                return _alreadyCloned[identifier];

            var resObject = appendToValue ?? _settings.OnCreateInstance(primaryType);

            if (identifier != null)
                _alreadyCloned.Add(identifier, resObject);

            foreach (var property in properties.Values)
            {
                if (!property.CanRead || property.FastDeepClonerIgnore)
                    continue;
                var value = property.GetValue(objectToBeCloned);
                if (value == null)
                    continue;

                if ((property.IsInternalType || value.GetType().IsInternalType()))
                    property.SetValue(resObject, value);
                else
                {
                    if (_settings.CloneLevel == CloneLevel.FirstLevelOnly)
                        continue;
                    property.SetValue(resObject, Clone(value));
                }
            }

            return resObject;
        }

        internal object Clone(object objectToBeCloned)
        {
            if (objectToBeCloned == null)
                return null;
            var primaryType = objectToBeCloned.GetType();
            if (primaryType.IsArray && primaryType.GetArrayRank() > 1)
                return ((Array) objectToBeCloned).Clone();

            if (objectToBeCloned.IsInternalObject())
                return objectToBeCloned;

            object resObject;
            if (primaryType.IsArray || (objectToBeCloned as IList) != null)
            {
                resObject = primaryType.IsArray
                    ? Array.CreateInstance(primaryType.GetIListType(), ((Array) objectToBeCloned).Length)
                    : Activator.CreateInstance(primaryType.GetIListType());
                var i = 0;
                var ilist = resObject as IList;
                var array = resObject as Array;

                foreach (var item in ((IList) objectToBeCloned))
                {
                    object clonedIteam = null;
                    if (item != null)
                    {
                        clonedIteam = item.GetType().IsInternalType() ? item : Clone(item);
                    }

                    if (!primaryType.IsArray)
                        ilist?.Add(clonedIteam);
                    else
                        array?.SetValue(clonedIteam, i);
                    i++;
                }

                foreach (var prop in primaryType.GetFastDeepClonerProperties(_ignoredPropertyInfos).Where(x =>
                    FastDeepClonerCachedItems.GetFastDeepClonerProperties(typeof(List<string>))
                        .All(a => a.Key != x.Key)))
                {
                    var property = prop.Value;
                    if (!property.CanRead || property.FastDeepClonerIgnore)
                        continue;
                    var value = property.GetValue(objectToBeCloned);
                    if (value == null)
                        continue;
                    var clonedIteam = value.GetType().IsInternalType() ? value : Clone(value);
                    property.SetValue(resObject, clonedIteam);
                }
            }
            else if (objectToBeCloned is IDictionary)
            {
                resObject = Activator.CreateInstance(primaryType);
                var resDic = resObject as IDictionary;
                var dictionary = (IDictionary) objectToBeCloned;
                foreach (var key in dictionary.Keys)
                {
                    var item = dictionary[key];
                    object clonedIteam = null;
                    if (item != null)
                    {
                        clonedIteam = item.GetType().IsInternalType() ? item : Clone(item);
                    }

                    resDic?.Add(key, clonedIteam);
                }
            }
            else if (primaryType.IsAnonymousType()) // dynamic types
            {
                var props = primaryType.GetFastDeepClonerProperties(_ignoredPropertyInfos);
                resObject = new ExpandoObject();
                var d = resObject as IDictionary<string, object>;
                foreach (var prop in props.Values)
                {
                    var item = prop.GetValue(objectToBeCloned);
                    var value = item == null || prop.IsInternalType || (item?.IsInternalObject() ?? true)
                        ? item
                        : Clone(item);
                    if (!d.ContainsKey(prop.Name))
                        d.Add(prop.Name, value);
                }
            }
            else
            {
                resObject = ReferenceTypeClone(
                    (_settings.FieldType == FieldType.FieldInfo
                        ? FastDeepClonerCachedItems.GetFastDeepClonerFields(primaryType)
                        : primaryType.GetFastDeepClonerProperties(_ignoredPropertyInfos)), primaryType,
                    objectToBeCloned);
                if (_settings.FieldType == FieldType.Both)
                    resObject = ReferenceTypeClone(
                        FastDeepClonerCachedItems.GetFastDeepClonerFields(primaryType).Values.ToList()
                            .Where(x => !primaryType.GetFastDeepClonerProperties(_ignoredPropertyInfos)
                                .ContainsKey(x.Name)).ToDictionary(x => x.Name, x => x), primaryType, objectToBeCloned,
                        resObject);
            }

            return resObject;
        }
    }
}