using InfinityMercsApp.Domain.Models.Perks;

namespace InfinityMercsApp.UnitTests;

public sealed class CompanyPerkCatalogPerkNodeTests
{
    [Fact]
    public void GetPerkNodeLists_BuildsRootsForEveryPerkList()
    {
        var lists = CompanyPerkCatalog.GetPerkNodeLists();
        var catalogLists = CompanyPerkCatalog.GetPerkListCatalogEntries();

        Assert.Equal(catalogLists.Count, lists.Count);
        Assert.All(lists, list =>
        {
            Assert.False(string.IsNullOrWhiteSpace(list.ListId));
            Assert.False(string.IsNullOrWhiteSpace(list.ListName));
            Assert.NotEmpty(list.Roots);
        });
    }
}
