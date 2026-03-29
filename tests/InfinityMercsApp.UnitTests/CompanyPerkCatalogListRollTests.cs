using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.UnitTests;

public sealed class CompanyPerkCatalogListRollTests
{
    [Fact]
    public void Roll_1_ReturnsExpectedCharts() => AssertChartsForRoll(
        1,
        new ExpectedChart("initiative-1-7", "Initiative"),
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    [Fact]
    public void Roll_2_ReturnsExpectedCharts() => AssertChartsForRoll(
        2,
        new ExpectedChart("initiative-1-7", "Initiative"),
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    [Fact]
    public void Roll_3_ReturnsExpectedCharts() => AssertChartsForRoll(
        3,
        new ExpectedChart("initiative-1-7", "Initiative"),
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    [Fact]
    public void Roll_4_ReturnsExpectedCharts() => AssertChartsForRoll(
        4,
        new ExpectedChart("initiative-1-7", "Initiative"),
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    [Fact]
    public void Roll_5_ReturnsExpectedCharts() => AssertChartsForRoll(
        5,
        new ExpectedChart("initiative-1-7", "Initiative"),
        new ExpectedChart("cool-5-10", "Cool"));

    [Fact]
    public void Roll_6_ReturnsExpectedCharts() => AssertChartsForRoll(
        6,
        new ExpectedChart("initiative-1-7", "Initiative"),
        new ExpectedChart("cool-5-10", "Cool"));

    [Fact]
    public void Roll_7_ReturnsExpectedCharts() => AssertChartsForRoll(
        7,
        new ExpectedChart("initiative-1-7", "Initiative"),
        new ExpectedChart("cool-5-10", "Cool"));

    [Fact]
    public void Roll_8_ReturnsExpectedCharts() => AssertChartsForRoll(
        8,
        new ExpectedChart("cool-5-10", "Cool"),
        new ExpectedChart("body-8-13", "Body"));

    [Fact]
    public void Roll_9_ReturnsExpectedCharts() => AssertChartsForRoll(
        9,
        new ExpectedChart("cool-5-10", "Cool"),
        new ExpectedChart("body-8-13", "Body"));

    [Fact]
    public void Roll_10_ReturnsExpectedCharts() => AssertChartsForRoll(
        10,
        new ExpectedChart("cool-5-10", "Cool"),
        new ExpectedChart("body-8-13", "Body"));

    [Fact]
    public void Roll_11_ReturnsExpectedCharts() => AssertChartsForRoll(
        11,
        new ExpectedChart("body-8-13", "Body"),
        new ExpectedChart("reflex-11-16", "Reflex"));

    [Fact]
    public void Roll_12_ReturnsExpectedCharts() => AssertChartsForRoll(
        12,
        new ExpectedChart("body-8-13", "Body"),
        new ExpectedChart("reflex-11-16", "Reflex"));

    [Fact]
    public void Roll_13_ReturnsExpectedCharts() => AssertChartsForRoll(
        13,
        new ExpectedChart("body-8-13", "Body"),
        new ExpectedChart("reflex-11-16", "Reflex"));

    [Fact]
    public void Roll_14_ReturnsExpectedCharts() => AssertChartsForRoll(
        14,
        new ExpectedChart("reflex-11-16", "Reflex"),
        new ExpectedChart("intelligence-14-19", "Intelligence"));

    [Fact]
    public void Roll_15_ReturnsExpectedCharts() => AssertChartsForRoll(
        15,
        new ExpectedChart("reflex-11-16", "Reflex"),
        new ExpectedChart("intelligence-14-19", "Intelligence"));

    [Fact]
    public void Roll_16_ReturnsExpectedCharts() => AssertChartsForRoll(
        16,
        new ExpectedChart("reflex-11-16", "Reflex"),
        new ExpectedChart("intelligence-14-19", "Intelligence"));

    [Fact]
    public void Roll_17_ReturnsExpectedCharts() => AssertChartsForRoll(
        17,
        new ExpectedChart("intelligence-14-19", "Intelligence"),
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    [Fact]
    public void Roll_18_ReturnsExpectedCharts() => AssertChartsForRoll(
        18,
        new ExpectedChart("intelligence-14-19", "Intelligence"),
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    [Fact]
    public void Roll_19_ReturnsExpectedCharts() => AssertChartsForRoll(
        19,
        new ExpectedChart("intelligence-14-19", "Intelligence"),
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    [Fact]
    public void Roll_20_ReturnsExpectedCharts() => AssertChartsForRoll(
        20,
        new ExpectedChart("empathy-17-20-1-4", "Empathy"));

    private static void AssertChartsForRoll(int roll, params ExpectedChart[] expectedCharts)
    {
        var actual = CompanyPerkCatalog.RollRandomLists(roll)
            .Select(x => new ExpectedChart(x.Id, x.Name))
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var expected = expectedCharts
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private sealed record ExpectedChart(string Id, string Name);
}
