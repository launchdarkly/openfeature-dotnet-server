# Change log

All notable changes to the LaunchDarkly OpenFeature provider for the Server-Side SDK for .NET will be documented in this file. This project adheres to [Semantic Versioning](http://semver.org).

## [2.0.0](https://github.com/launchdarkly/openfeature-dotnet-server/compare/1.0.0...2.0.0) (2024-08-26)


### ⚠ BREAKING CHANGES

* Support version 2.0 of the OpenFeature SDK. ([#38](https://github.com/launchdarkly/openfeature-dotnet-server/issues/38))

### Features

* Support version 2.0 of the OpenFeature SDK. ([#38](https://github.com/launchdarkly/openfeature-dotnet-server/issues/38)) ([1ebe21c](https://github.com/launchdarkly/openfeature-dotnet-server/commit/1ebe21c8f85aa53f6af45e2331d7a77ca7b089d3))

## [1.0.0](https://github.com/launchdarkly/openfeature-dotnet-server/compare/0.8.0...1.0.0) (2024-06-07)


### ⚠ BREAKING CHANGES

* 1.0.0 release. ([#35](https://github.com/launchdarkly/openfeature-dotnet-server/issues/35))

### Features

* 1.0.0 release. ([#35](https://github.com/launchdarkly/openfeature-dotnet-server/issues/35)) ([0440673](https://github.com/launchdarkly/openfeature-dotnet-server/commit/0440673e5640a863832918d2441fb5ad6de3c727))

## [0.8.0](https://github.com/launchdarkly/openfeature-dotnet-server/compare/0.7.0...0.8.0) (2024-06-04)


### Features

* Add support for the TargetingKey field and setter. ([#32](https://github.com/launchdarkly/openfeature-dotnet-server/issues/32)) ([c9e6a83](https://github.com/launchdarkly/openfeature-dotnet-server/commit/c9e6a8373e87ce15bca9638bca3d674d355b6be6))

## [0.7.0](https://github.com/launchdarkly/openfeature-dotnet-server/compare/0.6.0...0.7.0) (2024-04-08)


### Features

* Require OpenFeature SDK 1.5 or greater. ([#31](https://github.com/launchdarkly/openfeature-dotnet-server/issues/31)) ([2515fcd](https://github.com/launchdarkly/openfeature-dotnet-server/commit/2515fcd4ee21aabedbc79d591949ff57561d569b))
* Update to SDK 8.1.0. Support wrapper headers. ([#29](https://github.com/launchdarkly/openfeature-dotnet-server/issues/29)) ([08c1fa5](https://github.com/launchdarkly/openfeature-dotnet-server/commit/08c1fa5ed97ebaa38c67803563da50c950934452))

## [0.6.0](https://github.com/launchdarkly/openfeature-dotnet-server/compare/0.5.0...0.6.0) (2024-02-23)


### Features

* Add support for configuration changed event. ([#27](https://github.com/launchdarkly/openfeature-dotnet-server/issues/27)) ([3e61faa](https://github.com/launchdarkly/openfeature-dotnet-server/commit/3e61faa8bc0d4f270e88853264dc3dd644c242e2))
* Add support for initialization, shutdown, and provider status events. ([#26](https://github.com/launchdarkly/openfeature-dotnet-server/issues/26)) ([fe8db98](https://github.com/launchdarkly/openfeature-dotnet-server/commit/fe8db9883b2f8ad84dc71c9f8b24e3c61abc9c6d))
* Support the LaunchDarkly for .NET Server-Side 8.x SDK. ([#22](https://github.com/launchdarkly/openfeature-dotnet-server/issues/22)) ([da4a264](https://github.com/launchdarkly/openfeature-dotnet-server/commit/da4a264399825dc4b7ac282b781cc7a3a82fed7c))

## [0.5.0] - 2023-02-14
This version adds support for contexts. For a detailed explanation of contexts please refer to the [LaunchDarkly.ServerSdk 7.0.0 release notes.](https://github.com/launchdarkly/dotnet-server-sdk/releases/tag/7.0.0) The README contains a number of examples demonstrating how to use contexts.

### Changed:
- Upgraded to the `LaunchDarkly.ServerSdk` version `7.0.0`.

## [0.4.0] - 2022-10-28
### Changed:
- Updated to OpenFeature `dotnet-sdk` version `1.0.0`.
- Updated usage instructions in the readme.

## [0.3.0] - 2022-10-17
### Changed:
- Updated to the OpenFeature dotnet-sdk version 0.5.0. (Thanks, [@toddbaert](https://github.com/launchdarkly/openfeature-dotnet-server/pull/13)!)

## [0.2.0] - 2022-10-14
### Changed:
- Updated to the OpenFeature `dotnet-sdk` version `0.4.0`.

## [0.1.0] - 2022-10-07
Initial beta release of the LaunchDarkly OpenFeature provider for the Server-Side SDK for .NET.
