// Architecture gate: the implementation assembly must not depend on the test
// stack. Catches the failure class unit tests cannot express: the LLM making
// src reach into xunit/PipelineTests (e.g. generating its own assertions).
// Rule API grounded from resources/articles/netarchtest-readme.md.
using NetArchTest.Rules;
using Xunit;

namespace PipelineTests;

public class ArchitectureTests
{
    [Fact]
    public void SrcMustNotDependOnTestStack()
    {
        var result = Types.InAssembly(typeof(Pricing.DiscountPolicy).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("PipelineTests", "xunit")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "src assembly depends on the test stack: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
