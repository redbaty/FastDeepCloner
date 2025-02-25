﻿using System;
using System.Reflection;
using System.Linq;

namespace FastDeepCloner
{
    public class FastDeepClonerProperty : IFastDeepClonerProperty
    {
        public Func<object, object> GetMethod { get; set; }

        public Action<object, object> SetMethod { get; set; }

        public bool CanRead { get; private set; }

        public bool CanWrite { get; private set; }

        public bool ReadAble { get; private set; }

        public bool FastDeepClonerIgnore { get; private set; }

        public bool FastDeepClonerPrimaryIdentifire { get; private set; }

        public string Name { get; private set; }

        public string FullName { get; private set; }

        public bool IsInternalType { get; private set; }

        private Type _propertyType;
        public Type PropertyType
        {
            get => _propertyType;
            set
            {
                if (ConfigrationManager.OnPropertTypeSet != null)
                    _propertyType = _propertyType = ConfigrationManager.OnPropertTypeSet?.Invoke(this);
                else _propertyType = value;
            }
        }

        public bool? IsVirtual { get; private set; }

        public AttributesCollections Attributes { get; set; }

        public MethodInfo PropertyGetValue { get; private set; }

        public MethodInfo PropertySetValue { get; private set; }


        internal FastDeepClonerProperty(FieldInfo field)
        {
            CanRead = !(field.IsInitOnly || field.FieldType == typeof(IntPtr) || field.IsLiteral);
            CanWrite = CanRead;
            ReadAble = CanRead;
            FastDeepClonerIgnore = field.GetCustomAttribute<FastDeepClonerIgnore>() != null;
            FastDeepClonerPrimaryIdentifire = field.GetCustomAttribute<FastDeepClonerPrimaryIdentifire>() != null;
            GetMethod = field.GetValue;
            SetMethod = field.SetValue;
            Name = field.Name;
            FullName = field.FieldType.FullName;
            PropertyType = field.FieldType;
            Attributes = new AttributesCollections(field.GetCustomAttributes().ToList());
            IsInternalType = field.FieldType.IsInternalType();
            ConfigrationManager.OnAttributeCollectionChanged?.Invoke(this);
        }

        internal FastDeepClonerProperty(PropertyInfo property)
        {
            CanRead = !(!property.CanWrite || !property.CanRead || property.PropertyType == typeof(IntPtr) || property.GetIndexParameters().Length > 0);
            CanWrite = property.CanWrite;
            ReadAble = property.CanRead;
            FastDeepClonerIgnore = property.GetCustomAttribute<FastDeepClonerIgnore>() != null;
            FastDeepClonerPrimaryIdentifire = property.GetCustomAttribute<FastDeepClonerPrimaryIdentifire>() != null;
            GetMethod = property.GetValue;
            SetMethod = property.SetValue;
            Name = property.Name;
            FullName = property.PropertyType.FullName;
            IsInternalType = property.PropertyType.IsInternalType();
            IsVirtual = property.GetMethod.IsVirtual;
            PropertyGetValue = property.GetMethod;
            PropertySetValue = property.SetMethod;
            PropertyType = property.PropertyType;
            Attributes = new AttributesCollections(property.GetCustomAttributes().ToList());
            ConfigrationManager.OnAttributeCollectionChanged?.Invoke(this);

        }

        public bool ContainAttribute<T>() where T : Attribute
        {
            return Attributes?.ContainedAttributestypes.ContainsKey(typeof(T)) ?? false;
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            return ContainAttribute<T>() ? (T)Attributes?.ContainedAttributestypes[typeof(T)] : null;
        }

        public Attribute GetCustomAttribute(Type type)
        {
            return ContainAttribute(type) ? Attributes?.ContainedAttributestypes[type] : null;
        }

        public bool ContainAttribute(Type type)
        {
            return Attributes?.ContainedAttributestypes.ContainsKey(type) ?? false;
        }

        public void SetValue(object o, object value)
        {
            SetMethod(o, value);
        }

        public object GetValue(object o)
        {
            return GetMethod(o);
        }

        public void Add(Attribute attr)
        {
            Attributes.Add(attr);
            ConfigrationManager.OnAttributeCollectionChanged?.Invoke(this);
        }
    }
}
