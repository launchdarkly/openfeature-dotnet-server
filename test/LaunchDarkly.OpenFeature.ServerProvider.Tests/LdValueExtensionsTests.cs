using System.Collections.Generic;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class LdValueExtensionsTests
    {
        [Theory]
        [ClassData(typeof(BasicTypeTestData))]
        public void ItCanConvertBasicTypes(Value expectedValue, LdValue ldValue)
        {
            var ofValue = ldValue.ToValue();

            switch (ldValue.Type)
            {
                case LdValueType.Null:
                    Assert.True(ofValue.IsNull());
                    break;
                case LdValueType.Bool:
                    Assert.True(ofValue.IsBoolean());
                    Assert.Equal(expectedValue.AsBoolean(), ofValue.AsBoolean());
                    break;
                case LdValueType.Number:
                    Assert.True(ofValue.IsNumber());
                    Assert.Equal(expectedValue.AsDouble(), ofValue.AsDouble());
                    break;
                case LdValueType.String:
                    Assert.True(ofValue.IsString());
                    Assert.Equal(expectedValue.AsString(), ofValue.AsString());
                    break;
                case LdValueType.Array:
                case LdValueType.Object:
                default:
                    Assert.True(false, "Test misconfigured");
                    break;
            }
        }

        [Fact]
        public void ItCanConvertArrays()
        {
            var ldValueList = LdValue.ArrayFrom(new List<LdValue>
            {
                LdValue.Null,
                LdValue.Of(true),
                LdValue.Of(false),
                LdValue.Of("string"),
                LdValue.Of(17),
                LdValue.Of(42.5)
            });

            var ofValueList = ldValueList.ToValue();

            Assert.True(ofValueList.AsList()[0].IsNull());
            Assert.True(ofValueList.AsList()[1].AsBoolean());
            Assert.False(ofValueList.AsList()[2].AsBoolean());
            Assert.Equal("string", ofValueList.AsList()[3].AsString());
            Assert.Equal(17, ofValueList.AsList()[4].AsInteger());
            Assert.Equal(42.5, ofValueList.AsList()[5].AsDouble());
        }

        [Fact]
        public void ItCanConvertObjects()
        {
            var ldObject = LdValue.ObjectFrom(new Dictionary<string, LdValue>
            {
                ["true"] = LdValue.Of(true),
                ["number"] = LdValue.Of(42),
                ["string"] = LdValue.Of("string"),
                ["object"] = LdValue.ObjectFrom(new Dictionary<string, LdValue>
                {
                    ["number"] = LdValue.Of(84),
                    ["string"] = LdValue.Of("another-string")
                })
            });

            var ofStructureVal = ldObject.ToValue();

            var ofStructure = ofStructureVal.AsStructure();
            Assert.Equal(true, ofStructure.GetValue("true").AsBoolean());
            Assert.Equal(42, ofStructure.GetValue("number").AsInteger());
            Assert.Equal("string", ofStructure.GetValue("string").AsString());

            var ofStructure2 = ofStructure.GetValue("object").AsStructure();
            Assert.Equal(84, ofStructure2.GetValue("number").AsInteger());
            Assert.Equal("another-string", ofStructure2.GetValue("string").AsString());
        }
    }
}