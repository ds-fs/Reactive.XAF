﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnumsNET;
using Fasterflect;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ReflectionExtensions;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        private static Version _modelMapperModuleVersion;
        private static string[] References(this Type type, PropertyInfo[] propertyInfos,Type[] additionalTypes){
            var referencedTypes = propertyInfos.SelectMany(_ => new[]{_.PropertyType,_.DeclaringType}).ToArray();
            return referencedTypes
                .Concat(additionalTypes.SelectMany(_ => _.PublicProperties().SelectMany(info => info.GetCustomAttributesData().ReferencedTypes())))
                .Concat(additionalTypes.SelectMany(_ => _.GetCustomAttributesData().ReferencedTypes()))
                .Concat(referencedTypes.SelectMany(_ => _.GetCustomAttributesData().ReferencedTypes()))
                .Concat(propertyInfos.SelectMany(_ => _.GetCustomAttributesData().ReferencedTypes()))
                .Concat(new []{type})
                .Select(_ => _.Assembly.Location)
                .Concat(AdditionalReferences)
                .Distinct()
                .ToArray();
        }

        private static IEnumerable<Type> ReferencedTypes(this IList<CustomAttributeData> attributeData){
            return attributeData.Select(data => data.AttributeType).Concat(attributeData
                .SelectMany(customAttributeData => customAttributeData.ConstructorArguments)
                .Select(argument => argument.ArgumentType == typeof(Type) ? (Type) argument.Value : argument.ArgumentType))
                .Where(_ => _!=null);
        }

        private static Type GetRealType(this Type type){
            if (type != typeof(string)){
                if (typeof(IEnumerable).IsAssignableFrom(type)){
                    if (type.IsGenericType){
                        return type.GenericTypeArguments.First();
                    }
                    if (type.IsArray){
                        return type.GetElementType();
                    }

                    if (typeof(ICollection).IsAssignableFrom(type)){
                        var result = GetParameterType(type, "Add");
                        if (result == null){
                            result = GetParameterType(type, "CopyTo");
                            if (result != null)
                                return result;
                        }
                        else{
                            return result;
                        }
                    }
                    var interfaceType = type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType&&typeof(IEnumerable).IsAssignableFrom(_));
                    if (interfaceType!=null&&!interfaceType.GenericTypeArguments.First().IsTupleFamily()){
                        return interfaceType.GenericTypeArguments.First();
                    }

                    return typeof(object);
                }
            }
            return type;
        }

        private static Type GetParameterType(Type type,string name){
            var methodInfo = type.Methods(name).FirstOrDefault(info => info.Parameters().Any(parameterInfo => parameterInfo.ParameterType!=typeof(object)));
            if (methodInfo != null){
                var parameterType = methodInfo.Parameters().First().ParameterType;
                if (parameterType!=type&&parameterType!=typeof(Array)){
                    return parameterType.GetRealType();
                }

                return parameterType;
            }

            return null;
        }

        private static Type[] AdditionalTypes(this PropertyInfo[] propertyInfos,Type type){
            return propertyInfos
                .Where(_ => !_.PropertyType.IsValueType && typeof(string) != _.PropertyType && _.PropertyType != type)
                .SelectMany(_ => new []{_.PropertyType,_.PropertyType.GetRealType()}.Distinct())
                .DistinctWith(_ => $"{(_,type).ModelName()}{_.GetRealType()}")
                .Where(_ => _!=typeof(object))
                .ToArray();
        }

        private static PropertyInfo[] PropertyInfos(this Type type){
#pragma warning disable CS0618 // Type or member is obsolete
            return type.PublicProperties()
                .GetItems<PropertyInfo>(_ => _.PropertyType.GetRealType().PublicProperties(), info => info.PropertyType)
#pragma warning restore CS0618 // Type or member is obsolete
                .ToArray();
        }
        
        private static IEnumerable<PropertyInfo> PublicProperties(this Type type){
            var propertyInfos = type.Properties(Flags.AllMembers)
                .Where(info => !info.PropertyType.IsValueType && info.PropertyType != typeof(string) ||info.CanRead && info.CanWrite)
                .Where(IsValid)
                .Where(info => {
                    if (info.PropertyType == typeof(string) || info.PropertyType.IsNullableType()) return true;
                    var propertyTypeIsReserved = ReservedPropertyTypes.Any(_ => info.PropertyType==_);
                    if (propertyTypeIsReserved){
                        return false;
                    }

                    var reservedPropertyInstances = ReservedPropertyInstances.Any(_ => _.IsAssignableFrom(info.PropertyType));
                    if (reservedPropertyInstances){
                        return false;
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(info.PropertyType)){
                        var realType = info.PropertyType.GetRealType();
                        return !ReservedPropertyTypes.Contains(realType)&&!ReservedPropertyInstances.Any(_ => _.IsAssignableFrom(realType));
                    }
                    return !info.PropertyType.IsGenericType && info.PropertyType != type &&info.PropertyType != typeof(object) ;
                })
                .DistinctWith(info => info.Name);
            
            
            return propertyInfos;
        }

        private static bool IsValid(this PropertyInfo info){

            var isValid = info.AccessModifier() == AccessModifier.Public && !ReservedPropertyNames.Contains(info.Name);
            if (!isValid) return false;
            if (typeof(IEnumerable).IsAssignableFrom(info.PropertyType)){
                return true;
            }

            if (info.PropertyType.IsInterface || info.PropertyType.IsAbstract)
                return false;
            return !(typeof(IEnumerable).IsAssignableFrom(info.PropertyType) && info.PropertyType != typeof(string));
        }


        private static object GetEnums(this Type enumType, object value){
            if (FlagEnums.IsFlagEnum(enumType)&&FlagEnums.HasAnyFlags(enumType,value)){
                return string.Join("|", FlagEnums.GetFlagMembers(enumType, value)
                    .Select(member => $"{enumType.FullName}.{member.Name}"));
            }

            var name = Enum.GetName(enumType, value);
            return $"{enumType.FullName}.{name}";
        }


    }
}