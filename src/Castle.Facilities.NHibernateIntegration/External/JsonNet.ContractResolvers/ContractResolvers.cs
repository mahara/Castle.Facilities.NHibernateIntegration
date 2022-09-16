namespace JsonNet.ContractResolvers
{
    using System;
    using System.Reflection;

    using JsonNet.ContractResolvers.Internal;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Extends <see cref="DefaultContractResolver"/> with support for private setters.
    /// </summary>
    internal class PrivateSetterContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            return base.CreateProperty(member, memberSerialization).MakeWriteable(member);
        }
    }

    /// <summary>
    /// Extends <see cref="CamelCasePropertyNamesContractResolver"/> with support for private setters.
    /// </summary>
    internal class PrivateSetterCamelCasePropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            return base.CreateProperty(member, memberSerialization).MakeWriteable(member);
        }
    }

    /// <summary>
    /// Extends <see cref="DefaultContractResolver"/> with support for private setters and private constructors.
    /// </summary>
    internal class PrivateSetterAndCtorContractResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            return base.CreateObjectContract(objectType).SupportPrivateCTors(objectType, CreateConstructorParameters);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            return base.CreateProperty(member, memberSerialization).MakeWriteable(member);
        }
    }

    /// <summary>
    /// Extends <see cref="CamelCasePropertyNamesContractResolver"/> with support for private setters and private constructors.
    /// </summary>
    internal class PrivateSetterAndCtorCamelCasePropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            return base.CreateObjectContract(objectType).SupportPrivateCTors(objectType, CreateConstructorParameters);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            return base.CreateProperty(member, memberSerialization).MakeWriteable(member);
        }
    }
}