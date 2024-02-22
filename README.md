[![Build and Test](https://github.com/launchdarkly/openfeature-dotnet-server/actions/workflows/ci.yml/badge.svg)](https://github.com/launchdarkly/openfeature-dotnet-server/actions/workflows/ci.yml)

# LaunchDarkly OpenFeature provider for the Server-Side SDK for .NET

This provider allows for using LaunchDarkly with the OpenFeature SDK for .NET.

This provider is designed primarily for use in multi-user systems such as web servers and applications. It follows the server-side LaunchDarkly model for multi-user contexts. It is not intended for use in desktop and embedded systems applications.

> [!WARNING]
> This is a beta version. The API is not stabilized and may introduce breaking changes.

> [!NOTE]
> This OpenFeature provider uses production versions of the LaunchDarkly SDK, which adhere to our standard [versioning policy](https://docs.launchdarkly.com/home/relay-proxy/versioning).

# LaunchDarkly overview

[LaunchDarkly](https://www.launchdarkly.com) is a feature management platform that serves trillions of feature flags daily to help teams build better software, faster. [Get started](https://docs.launchdarkly.com/home/getting-started) using LaunchDarkly today!

[![Twitter Follow](https://img.shields.io/twitter/follow/launchdarkly.svg?style=social&label=Follow&maxAge=2592000)](https://twitter.com/intent/follow?screen_name=launchdarkly)

## Supported .NET versions

This version of the SDK is built for the following targets:
* .NET 6.0: runs on .NET 6.0 and above.
* .NET Framework 4.7.1: runs on .NET Framework 4.7.1 and above.
* .NET Standard 2.0: runs in any project that is targeted to .NET Standard 2.x rather than to a specific runtime platform.

## Getting started

### Installation

```bash
dotnet add package LaunchDarkly.ServerSdk
dotnet add package LaunchDarkly.OpenFeature.ServerProvider
dotnet add package OpenFeature
```

### Usage
```csharp
using LaunchDarkly.OpenFeature.ServerProvider;
using LaunchDarkly.Sdk.Server;

var config = Configuration.Builder("my-sdk-key")
    .StartWaitTime(TimeSpan.FromSeconds(10))
    .Build();

var provider = new Provider(config);

// If you need access to the LdClient, then you can use GetClient().
// This can be used for use-cases that are not supported by OpenFeature such as migration flags and track events.
var ldClient = provider.GetClient()

OpenFeature.Api.Instance.SetProvider(provider);
```

Refer to the [SDK reference guide](https://docs.launchdarkly.com/sdk/server-side/dotnet) for instructions on getting started with using the SDK.

For information on using the OpenFeature client please refer to the [OpenFeature Documentation](https://docs.openfeature.dev/docs/reference/concepts/evaluation-api/).

## OpenFeature Specific Considerations

LaunchDarkly evaluates contexts, and it can either evaluate a single-context, or a multi-context. When using OpenFeature both single and multi-contexts must be encoded into a single `EvaluationContext`. This is accomplished by looking for an attribute named `kind` in the `EvaluationContext`.

There are 4 different scenarios related to the `kind`:
1. There is no `kind` attribute. In this case the provider will treat the context as a single context containing a "user" kind.
2. There is a `kind` attribute, and the value of that attribute is "multi". This will indicate to the provider that the context is a multi-context.
3. There is a `kind` attribute, and the value of that attribute is a string other than "multi". This will indicate to the provider a single context of the kind specified.
4. There is a `kind` attribute, and the attribute is not a string. In this case the value of the attribute will be discarded, and the context will be treated as a "user". An error message will be logged.

The `kind` attribute should be a string containing only contain ASCII letters, numbers, `.`, `_` or `-`.

The OpenFeature specification allows for an optional targeting key, but LaunchDarkly requires a key for evaluation. A targeting key must be specified for each context being evaluated. It may be specified using either `targetingKey`, as it is in the OpenFeature specification, or `key`, which is the typical LaunchDarkly identifier for the targeting key. If a `targetingKey` and a `key` are specified, then the `targetingKey` will take precedence.

There are several other attributes which have special functionality within a single or multi-context. 
- A key of `privateAttributes`. Must be an array of string values. [Equivalent to the 'Private' builder method in the SDK.](https://launchdarkly.github.io/dotnet-server-sdk/api/LaunchDarkly.Sdk.ContextBuilder.html#LaunchDarkly_Sdk_ContextBuilder_Private_System_String___)
- A key of `anonymous`. Must be a boolean value.  [Equivalent to the 'Anonymous' builder method in the SDK.](https://launchdarkly.github.io/dotnet-server-sdk/api/LaunchDarkly.Sdk.Context.html#LaunchDarkly_Sdk_Context_Anonymous)
- A key of `name`. Must be a string. [Equivalent to the 'Name' builder method in the SDK.](https://launchdarkly.github.io/dotnet-server-sdk/api/LaunchDarkly.Sdk.ContextBuilder.html#LaunchDarkly_Sdk_ContextBuilder_Name_System_String_)

### Examples

#### A single user context

```csharp
var evaluationContext = EvaluationContext.Builder()
  .Set("targetingKey", "my-user-key") // Could also use "key" instead of "targetingKey".
  .Build();
```

#### A single context of kind "organization"

```csharp
var evaluationContext = EvaluationContext.Builder()
  .Set("kind", "organization")
  .Set("targetingKey", "my-org-key") // Could also use "key" instead of "targetingKey".
  .Build();
```

#### A multi-context containing a "user" and an "organization"

```csharp
var evaluationContext = EvaluationContext.Builder()
  .Set("kind", "multi") // Lets the provider know this is a multi-context
  // Every other top level attribute should be a structure representing
  // individual contexts of the multi-context.
  // (non-conforming attributes will be ignored and a warning logged).
  .Set("organization", new Structure(new Dictionary<string, Value>
  {
    {"targetingKey", new Value("my-org-key")},
    {"name", new Value("the-org-name")},
    {"myCustomAttribute", new Value("myAttributeValue")}
  }))
  .Set("user", new Structure(new Dictionary<string, Value> {
    {"targetingKey", new Value("my-user-key")},
  }))
  .Build();
```

#### Setting private attributes in a single context

```csharp
var evaluationContext = EvaluationContext.Builder()
  .Set("kind", "organization")
  .Set("name", "the-org-name")
  .Set("targetingKey", "my-org-key")
  .Set("anonymous", true)
  .Set("myCustomAttribute", "myCustomValue")
  .Set("privateAttributes", new Value(new List<Value>{new Value("myCustomAttribute")}))
  .Build();
```

#### Setting private attributes in a multi-context

```csharp
var evaluationContext = EvaluationContext.Builder()
  .Set("kind", "multi")
  .Set("organization", new Structure(new Dictionary<string, Value>
  {
    {"targetingKey", new Value("my-org-key")},
    {"name", new Value("the-org-name")},
    // This will ONLY apply to the "organization" attributes.
    {"privateAttributes", new Value(new List<Value>{new Value("myCustomAttribute")})}
    // This attribute will be private.
    {"myCustomAttribute", new Value("myAttributeValue")},
  }))
  .Set("user", new Structure(new Dictionary<string, Value> {
    {"targetingKey", new Value("my-user-key")},
    {"anonymous", new Value(true)},
    // This attribute will not be private.
    {"myCustomAttribute", new Value("myAttributeValue")},
  }))
  .Build();
```

### Advanced Usage

#### Asynchronous Initialization

The LaunchDarkly SDK by default blocks on construction for up to 5 seconds for initialization. If you require construction to be non-blocking, then you can adjust the `startWaitTime` to `TimeSpan.Zero`. Initialiation will be completed asynchronously and OpenFeature will emit a ready event when the provider has initialized.

```csharp
var config = Configuration.Builder("my-sdk-key")
    .StartWaitTime(TimeSpan.Zero)
    .Build();
```

#### Provider Shutdown

This provider cannot be re-initialized after being shutdown. This will not impact typical usage, as the LaunchDarkly provider will be set once and used throughout the execution of the application. If you remove the LaunchDarkly Provider, by replacing the default provider or any named providers aliased to the LaunchDarkly provider, then you must create a new provider instance.

```csharp
var ldProvider = new Provider(config);

OpenFeature.Api.Instance.SetProvider(ldProvider);
OpenFeatute.Api.Instance.SetProvider(new SomeOtherProvider());
/// The LaunchDarkly provider will be shutdown and SomeOtherProvider will start handling requests.

// This provider will never finish initializing.
OpenFeature.Api.Instance.SetProvider(ldProvider);

// Instead you should create a new provider.
var ldProvider2 = new Provider(config);
OpenFeature.Api.Instance.SetProvider(ldProvider2);

```


## Learn more

Read our [documentation](http://docs.launchdarkly.com) for in-depth instructions on configuring and using LaunchDarkly. You can also head straight to the [complete reference guide for this SDK](https://docs.launchdarkly.com/sdk/server-side/dotnet).

The authoritative description of all properties and methods is in the [dotnet documentation](https://launchdarkly.github.io/dotnet-server-sdk/).

## Contributing

We encourage pull requests and other contributions from the community. Check out our [contributing guidelines](CONTRIBUTING.md) for instructions on how to contribute to this SDK.

## About LaunchDarkly

* LaunchDarkly is a continuous delivery platform that provides feature flags as a service and allows developers to iterate quickly and safely. We allow you to easily flag your features and manage them from the LaunchDarkly dashboard.  With LaunchDarkly, you can:
    * Roll out a new feature to a subset of your users (like a group of users who opt-in to a beta tester group), gathering feedback and bug reports from real-world use cases.
    * Gradually roll out a feature to an increasing percentage of users, and track the effect that the feature has on key metrics (for instance, how likely is a user to complete a purchase if they have feature A versus feature B?).
    * Turn off a feature that you realize is causing performance problems in production, without needing to re-deploy, or even restart the application with a changed configuration file.
    * Grant access to certain features based on user attributes, like payment plan (eg: users on the ‘gold’ plan get access to more features than users in the ‘silver’ plan). Disable parts of your application to facilitate maintenance, without taking everything offline.
* LaunchDarkly provides feature flag SDKs for a wide variety of languages and technologies. Read [our documentation](https://docs.launchdarkly.com/sdk) for a complete list.
* Explore LaunchDarkly
    * [launchdarkly.com](https://www.launchdarkly.com/ "LaunchDarkly Main Website") for more information
    * [docs.launchdarkly.com](https://docs.launchdarkly.com/  "LaunchDarkly Documentation") for our documentation and SDK reference guides
    * [apidocs.launchdarkly.com](https://apidocs.launchdarkly.com/  "LaunchDarkly API Documentation") for our API documentation
    * [blog.launchdarkly.com](https://blog.launchdarkly.com/  "LaunchDarkly Blog Documentation") for the latest product updates
