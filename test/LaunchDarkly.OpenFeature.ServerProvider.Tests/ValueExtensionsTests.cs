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

        [Fact]
        public void ItCanConvertArrays()
        {
            var ofValueList = new Value(new List<Value>
            {
                new Value(true),
                new Value(false),
                new Value(17),
                new Value(17.5),
                new Value("string")
            });
            
            ofValueList.Extract(ldValue =>
            {
                var listFromValue = ldValue.List;
                Assert.NotNull(listFromValue);
                Assert.Equal(LdValueType.Bool, listFromValue[0].Type);
                Assert.True(listFromValue[0].AsBool);
                
                Assert.Equal(LdValueType.Bool, listFromValue[1].Type);
                Assert.False(listFromValue[1].AsBool);
                
                Assert.Equal(LdValueType.Number, listFromValue[2].Type);
                Assert.Equal(17, listFromValue[2].AsInt);
                
                Assert.Equal(LdValueType.Number, listFromValue[3].Type);
                Assert.Equal(17.5, listFromValue[3].AsDouble);
                
                Assert.Equal(LdValueType.String, listFromValue[4].Type);
                Assert.Equal("string", listFromValue[4].AsString);
            });
        }
    }
}