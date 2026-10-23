using Plumix.Foundation;
using Plumix.Widgets;
using Xunit;

// Dart parity source (reference): pub.dev/packages/provider (composition behavior regression tests)
//
// Widget-tree behavior (Of/Watch/Consumer rebuilds) is not covered here yet: hosting a
// widget tree requires internal Plumix test infrastructure (BuildOwner pumping, a root element).
// These tests cover the publicly observable composition contracts.

namespace Plumix.Provider.Tests;

public sealed class MultiProviderTests
{
    [Fact]
    public void Build_ComposesProvidersOutermostFirst()
    {
        var innerChild = new Marker("child");
        var received = new List<Widget>();

        Widget Wrap(string name, Widget child)
        {
            received.Add(child);
            return new Provider<string>(name, child);
        }

        var multiProvider = new MultiProvider(
            child: innerChild,
            providers:
            [
                child => Wrap("outer", child),
                child => Wrap("middle", child),
                child => Wrap("inner", child),
            ]);

        var result = multiProvider.Build(multiProvider.CreateElement());

        // The last factory wraps the child directly; the first factory produces the outermost widget.
        var outer = Assert.IsType<Provider<string>>(result);
        Assert.Equal("outer", outer.Value);

        var middle = Assert.IsType<Provider<string>>(outer.Child);
        Assert.Equal("middle", middle.Value);

        var inner = Assert.IsType<Provider<string>>(middle.Child);
        Assert.Equal("inner", inner.Value);
        Assert.Same(innerChild, inner.Child);

        // Factories run bottom-up: "inner" is invoked first and receives the original child.
        Assert.Same(innerChild, received[0]);
    }

    [Fact]
    public void Build_WithoutProviders_ReturnsChildUnchanged()
    {
        var child = new Marker("child");
        var multiProvider = new MultiProvider(child, providers: []);

        Assert.Same(child, multiProvider.Build(multiProvider.CreateElement()));
    }

    [Fact]
    public void Provider_StoresValueAndChild()
    {
        var child = new Marker("child");
        var key = new ValueKey<string>("provider");
        var provider = new Provider<int>(42, child, key);

        Assert.Equal(42, provider.Value);
        Assert.Same(child, provider.Child);
        Assert.Same(key, provider.Key);
        Assert.IsType<InheritedElement>(provider.CreateElement());
    }

    [Fact]
    public void Provider_UpdateShouldNotify_ComparesValues()
    {
        var child = new Marker("child");

        Assert.False(new Provider<int>(1, child).UpdateShouldNotify(new Provider<int>(1, child)));
        Assert.True(new Provider<int>(2, child).UpdateShouldNotify(new Provider<int>(1, child)));
    }

    private sealed class Marker(string name) : StatelessWidget
    {
        public string Name { get; } = name;

        public override Widget Build(BuildContext context)
            => throw new NotSupportedException("Marker is never built.");
    }
}
