using MultiAI.Authorization;

namespace MultiAI.Tests
{
    public class AppPermissionsTests
    {
        [Fact]
        public void All_ContainsUserDefaults()
        {
            foreach (var permission in AppPermissions.UserDefaults)
            {
                Assert.Contains(permission, AppPermissions.All);
            }
        }

        [Fact]
        public void Permissions_UseResourceActionFormat()
        {
            foreach (var permission in AppPermissions.All)
            {
                Assert.Matches(@"^[a-z]+\.[a-z.]+$", permission);
            }
        }

        [Fact]
        public void Roles_AreExpected()
        {
            Assert.Equal("Admin", AppRoles.Admin);
            Assert.Equal("User", AppRoles.User);
        }
    }
}
