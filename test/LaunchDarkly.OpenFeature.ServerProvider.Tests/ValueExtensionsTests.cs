using System;
using System.Collections.Generic;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class ValueExtensionsTests
    {
        [Theory]
        [ClassData(typeof(BasicTypeTestData))]
        public void ItCanConvertBasicTypes(Value ofValue, LdValue expectedValue)
        {
            var ldValue = ofValue.ExtractValue();

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
        }

        [Fact]
        public void ItCanConvertDates()
        {
            var date = new DateTime(0);
            var dateString = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var ofValue = new Value(date);
            var ldValue = ofValue.ExtractValue();
            Assert.Equal(dateString, ldValue.AsString);
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

            var ldValue = ofValueList.ExtractValue();

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
            var value = ofValue.ExtractValue();

            Assert.Equal(LdValueType.Object, value.Type);
            var valDict = value.Dictionary;
            Assert.True(valDict["true"].AsBool);
            Assert.Equal(42, valDict["number"].AsDouble);
            Assert.Equal("string", valDict["string"].AsString);

            var secondDict = valDict["structure"].Dictionary;
            Assert.Equal(84, secondDict["number"].AsDouble);
            Assert.Equal("another-string", secondDict["string"].AsString);
        }
    }
}