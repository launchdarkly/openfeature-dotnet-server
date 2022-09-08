using System;
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
                yield return new object[] {new Value(), LdValue.Null };
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(BasicTypeTestData))]
        public void ItCanConvertBasicTypes(Value ofValue, LdValue expectedValue)
        {
            var valueExtracted = false;
            ofValue.Extract((ldValue) =>
            {
                valueExtracted = true;
                Assert.Equal(expectedValue.Type, ldValue.Type);
                switch (expectedValue.Type)
                {
                    case LdValueType.Null:
                        // Type check is all we need here.
                        break;
                    case LdValueType.Bool:
                        Assert.Equal(expectedValue.AsBool, ldValue.AsBool);
                        break;
                    case LdValueType.Number:
                        Assert.Equal(expectedValue.AsDouble, ldValue.AsDouble);
                        break;
                    case LdValueType.String:
                        Assert.Equal(expectedValue.AsString, ldValue.AsString);
                        break;
                    case LdValueType.Array:
                    case LdValueType.Object:
                    default:
                        Assert.True(false, "Test misconfigured");
                        break;
                }
            });
            Assert.True(valueExtracted);
        }

        [Fact]
        public void ItCanConvertDates()
        {
            var date = new DateTime(0);
            var dateString = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var ofValue = new Value(date);
            var valueExtracted = false;
            ofValue.Extract(ldValue =>
            {
                valueExtracted = true;
                Assert.Equal(dateString, ldValue.AsString);
            });
            Assert.True(valueExtracted);
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
            var valueExtracted = false;
            
            ofValueList.Extract(ldValue =>
            {
                valueExtracted = true;
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
            Assert.True(valueExtracted);
        }

        [Fact]
        public void ItCanConvertStructures()
        {
            var ofStructure = new Structure
            {
                {"true", true},
                {"number", 42},
                {"string", "string"}
            };
            var secondStructure = new Structure
            {
                {"number", 84},
                {"string", "another-string"}
            };
            ofStructure.Add("structure", secondStructure);
            var ofValue = new Value(ofStructure);
            var valueExtracted = false;
            ofValue.Extract(value =>
            {
                valueExtracted = true;
                Assert.Equal(LdValueType.Object, value.Type);
                var valDict = value.Dictionary;
                Assert.True(valDict["true"].AsBool);
                Assert.Equal(42, valDict["number"].AsDouble);
                Assert.Equal("string", valDict["string"].AsString);

                var secondDict = valDict["structure"].Dictionary;
                Assert.Equal(84, secondDict["number"].AsDouble);
                Assert.Equal("another-string", secondDict["string"].AsString);
            });
            Assert.True(valueExtracted);
        }
    }
}
