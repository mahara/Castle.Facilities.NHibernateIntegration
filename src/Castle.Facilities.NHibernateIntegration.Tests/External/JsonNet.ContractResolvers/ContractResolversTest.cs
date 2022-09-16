namespace JsonNet.ContractResolvers.Tests
{
    using FluentAssertions;

    using JsonNet.ContractResolvers;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    using NUnit.Framework;

#nullable enable
    internal interface IModel
    {
        string? SomeStringValue { get; }
        int SomeIntValue { get; }
    }

    internal class ModelWithPublicCTor : IModel
    {
        public string? SomeStringValue { get; private set; }
        public int SomeIntValue { get; private set; }
    }

    internal class ModelWithPrivateCTorWithoutArgs : IModel
    {
        public string SomeStringValue { get; private set; }
        public int SomeIntValue { get; private set; }

        private ModelWithPrivateCTorWithoutArgs()
        {
            SomeStringValue = "Default value";
            SomeIntValue = -92138674;
        }
    }

    internal class ModelWithPrivateCTorWithArgs : IModel
    {
        public string SomeStringValue { get; private set; }
        public int SomeIntValue { get; private set; }

        private ModelWithPrivateCTorWithArgs(string someStringValue, int someIntValue)
        {
            SomeStringValue = someStringValue;
            SomeIntValue = someIntValue;
        }
    }

    internal abstract class ContractResolverTests<TModel> where TModel : class, IModel
    {
        private readonly IContractResolver _resolver;

        protected ContractResolverTests(IContractResolver resolver)
        {
            _resolver = resolver;
        }

        protected TModel? Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<TModel>(json, new JsonSerializerSettings
            {
                ContractResolver = _resolver
            });
        }

        [Test]
        public void When_proper_case_Should_deserialize_to_private_setters()
        {
            const string json = @"{""SomeStringValue"":""Some value"", ""SomeIntValue"": 42}";

            var model = Deserialize(json)!;

            model.SomeStringValue.Should().Be("Some value");
            model.SomeIntValue.Should().Be(42);
        }

        [Test]
        public void When_camel_case_Should_deserialize_to_private_setters()
        {
            const string json = @"{""someStringValue"":""Some value"", ""someIntValue"": 42}";

            var model = Deserialize(json)!;

            model.SomeStringValue.Should().Be("Some value");
            model.SomeIntValue.Should().Be(42);
        }
    }

    internal class PrivateSetterContractResolver_When_public_cTor : ContractResolverTests<ModelWithPublicCTor>
    {
        public PrivateSetterContractResolver_When_public_cTor() :
            base(new PrivateSetterContractResolver())
        {
        }
    }

    internal class PrivateSetterCamelCasePropertyNamesContractResolver_When_public_cTor : ContractResolverTests<ModelWithPublicCTor>
    {
        public PrivateSetterCamelCasePropertyNamesContractResolver_When_public_cTor() :
            base(new PrivateSetterCamelCasePropertyNamesContractResolver())
        {
        }
    }

    internal class PrivateSetterAndCtorContractResolver_When_private_cTor_has_no_args : ContractResolverTests<ModelWithPrivateCTorWithoutArgs>
    {
        public PrivateSetterAndCtorContractResolver_When_private_cTor_has_no_args() :
            base(new PrivateSetterAndCtorContractResolver())
        {
        }
    }

    internal class PrivateSetterAndCtorContractResolver_When_private_cTor_has_args : ContractResolverTests<ModelWithPrivateCTorWithArgs>
    {
        public PrivateSetterAndCtorContractResolver_When_private_cTor_has_args() :
            base(new PrivateSetterAndCtorContractResolver())
        {
        }
    }

    internal class PrivateSetterAndCtorCamelCasePropertyNamesContractResolver_When_private_cTor_has_no_args : ContractResolverTests<ModelWithPrivateCTorWithoutArgs>
    {
        public PrivateSetterAndCtorCamelCasePropertyNamesContractResolver_When_private_cTor_has_no_args() :
            base(new PrivateSetterAndCtorCamelCasePropertyNamesContractResolver())
        {
        }
    }

    internal class PrivateSetterAndCtorCamelCasePropertyNamesContractResolver_When_private_cTor_has_args : ContractResolverTests<ModelWithPrivateCTorWithArgs>
    {
        public PrivateSetterAndCtorCamelCasePropertyNamesContractResolver_When_private_cTor_has_args() :
            base(new PrivateSetterAndCtorCamelCasePropertyNamesContractResolver())
        {
        }
    }
}