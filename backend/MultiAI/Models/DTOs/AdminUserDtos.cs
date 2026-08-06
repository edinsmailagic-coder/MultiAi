namespace MultiAI.Models.DTOs
{
    public class AdminUserListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public bool LockoutEnabled { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = [];
    }

    public class AdminUserDetailDto : AdminUserListItemDto
    {
        public List<string> Permissions { get; set; } = [];
    }

    public class CreateAdminUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
    }

    public class UpdateAdminUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
        public List<string>? Roles { get; set; }
    }

    public class ResetAdminPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class RoleListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public List<string> Permissions { get; set; } = [];
    }

    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = [];
    }

    public class UpdateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = [];
    }
}
