using WinformTTC.E2E.Infrastructure;
using Xunit;

namespace WinformTTC.E2E.Tests;

[CollectionDefinition("E2E")]
public class E2ECollection : ICollectionFixture<AppFixture>;
