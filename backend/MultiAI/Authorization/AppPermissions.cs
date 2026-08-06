namespace MultiAI.Authorization
{
    public static class AppClaimTypes
    {
        public const string Permission = "permission";
    }

    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
        public const string User = "User";
    }

    public static class AppPermissions
    {
        public const string AuthMe = "auth.me";
        public const string AuthProfileWrite = "auth.profile.write";
        public const string AuthPasswordChange = "auth.password.change";

        public const string ProvidersRead = "providers.read";
        public const string ProvidersManage = "providers.manage";

        public const string WorkflowsRead = "workflows.read";
        public const string WorkflowsWrite = "workflows.write";
        public const string WorkflowsExecute = "workflows.execute";

        public const string ChatRead = "chat.read";
        public const string ChatWrite = "chat.write";

        public const string UsageRead = "usage.read";

        public const string UsersRead = "users.read";
        public const string UsersManage = "users.manage";
        public const string RolesRead = "roles.read";
        public const string RolesManage = "roles.manage";

        public static readonly string[] All =
        [
            AuthMe,
            AuthProfileWrite,
            AuthPasswordChange,
            ProvidersRead,
            ProvidersManage,
            WorkflowsRead,
            WorkflowsWrite,
            WorkflowsExecute,
            ChatRead,
            ChatWrite,
            UsageRead,
            UsersRead,
            UsersManage,
            RolesRead,
            RolesManage,
        ];

        public static readonly string[] UserDefaults =
        [
            AuthMe,
            AuthProfileWrite,
            AuthPasswordChange,
            ProvidersRead,
            ProvidersManage,
            WorkflowsRead,
            WorkflowsWrite,
            WorkflowsExecute,
            ChatRead,
            ChatWrite,
            UsageRead,
        ];

        public static readonly string[] ModeratorExtras =
        [
            UsersRead,
            UsersManage,
            RolesRead,
            RolesManage,
        ];

        public static string[] ModeratorDefaults =>
            UserDefaults.Concat(ModeratorExtras).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
