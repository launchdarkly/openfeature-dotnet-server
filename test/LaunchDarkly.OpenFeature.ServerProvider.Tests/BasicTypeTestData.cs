using System.Collections;
using System.Collections.Generic;
using LaunchDarkly.Sdk;
using OpenFeatureSDK.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
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
}
