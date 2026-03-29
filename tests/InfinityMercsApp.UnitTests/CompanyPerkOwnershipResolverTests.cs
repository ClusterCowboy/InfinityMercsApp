using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.UnitTests;

public sealed class CompanyPerkOwnershipResolverTests
{
    [Fact]
    public void ResolveOwnedPerkNodeIds_GhulamDoctorProfile_ReturnsDoctorPerkNodes()
    {
        // Starting case: a Ghulam Doctor profile gives us the Doctor skill.
        var ghulamDoctorSkills = new[] { "Doctor" };

        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(ghulamDoctorSkills);

        Assert.Contains("intelligence-14-19-track-2-tier-3", ownedIds);
        Assert.Contains("intelligence-14-19-track-2-tier-4", ownedIds);
    }

    [Fact]
    public void ResolveOwnedPerkNodeIds_NoProfileData_ReturnsEmpty()
    {
        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(skills: []);
        Assert.Empty(ownedIds);
    }
}
