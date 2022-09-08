using System.Collections;
using System.Collections.Generic;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class ValueExtensionsTests
    {
        class BasicTypeTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] {new Value(true), LdValue.Of(true) };
                yield return new object[] {new Value(false), LdValue.Of(false) };
                yield return new object[] {new Value(17), LdValue.Of(17) };
                yield return new object[] {new Value(17.5), LdValue.Of(17.5) };
                yield return new object[] {new Value("string"), LdValue.Of("string") };
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(BasicTypeTestData))]
        public void ItCanConvertBasicTypes(Value ofValue, LdValue expectedValue)
        {
            ofValue.Extract((ldValue) =>
            {
                Assert.Equal(expectedValue.Type, ldValue.Type);
                Assert.Equal(expectedValue.AsString, ldValue.AsString);
            });
        }
    }
}