# Contributing to the LaunchDarkly OpenFeature provider for the Server-Side SDK for .NET

LaunchDarkly has published an [SDK contributor's guide](https://docs.launchdarkly.com/sdk/concepts/contributors-guide) that provides a detailed explanation of how our SDKs work. See below for additional information on how to contribute to this provider.

## Submitting bug reports and feature requests

The LaunchDarkly SDK team monitors the [issue tracker](https://github.com/launchdarkly/openfeature-dotnet-server/issues) in the provider repository. Bug reports and feature requests specific to this provider should be filed in this issue tracker. The SDK team will respond to all newly filed issues within two business days.

## Submitting pull requests

We encourage pull requests and other contributions from the community. Before submitting pull requests, ensure that all temporary or unintended code is removed. Don't worry about adding reviewers to the pull request; the LaunchDarkly SDK team will add themselves. The SDK team will acknowledge all pull requests within two business days.

## Build instructions

### Prerequisites

To set up your build time environment, you must [download .NET development tools and follow the instructions](https://dotnet.microsoft.com/download). .NET 6.0 is preferred, since the .NET 6.0 tools are able to build for all supported target platforms.

### Building

To install all required packages:

```bash
dotnet restore
```

Then, to build the SDK for all target frameworks:

```bash
dotnet build src/LaunchDarkly.OpenFeature.ServerProvider
```

Or, to build for only one target framework (in this example, .NET Standard 2.0):

```bash
dotnet build src/LaunchDarkly.OpenFeature.ServerProvider -f netstandard2.0
```

### Testing

To run all unit tests:

```bash
dotnet test test/LaunchDarkly.OpenFeature.ServerProvider.Tests/LaunchDarkly.OpenFeature.ServerProvider.Tests.csproj
```
