using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026071201)]
    public class CreateIdentityTables:Migration
    {
        public override void Up()
        {

            Create.Table("Users")
                .WithColumn("Id").AsString(450).PrimaryKey()
                .WithColumn("UserName").AsString(256).Nullable()
                .WithColumn("NormalizedUserName").AsString(256).Nullable()
                .WithColumn("Email").AsString(256).Nullable()
                .WithColumn("NormalizedEmail").AsString(256).Nullable()
                .WithColumn("EmailConfirmed").AsBoolean().NotNullable()
                .WithColumn("PasswordHash").AsString().Nullable()
                .WithColumn("SecurityStamp").AsString().Nullable()
                .WithColumn("ConcurrencyStamp").AsString().Nullable()
                .WithColumn("PhoneNumber").AsString().Nullable()
                .WithColumn("PhoneNumberConfirmed").AsBoolean().NotNullable()
                .WithColumn("TwoFactorEnabled").AsBoolean().NotNullable()
                .WithColumn("LockoutEnd").AsDateTimeOffset().Nullable()
                .WithColumn("LockoutEnabled").AsBoolean().NotNullable()
                .WithColumn("AccessFailedCount").AsInt32().NotNullable()


                .WithColumn("FirstName").AsString(100).NotNullable()
                .WithColumn("LastName").AsString(100).NotNullable()


                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);


            Create.Table("Roles")
                .WithColumn("Id").AsString(450).PrimaryKey()
                .WithColumn("Name").AsString(256).Nullable()
                .WithColumn("NormalizedName").AsString(256).Nullable()
                .WithColumn("ConcurrencyStamp").AsString().Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);


            Create.Table("UserRoles")
                .WithColumn("UserId").AsString(450).PrimaryKey().ForeignKey("FK_UserRoles_Users", "Users", "Id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("RoleId").AsString(450).PrimaryKey().ForeignKey("FK_UserRoles_Roles", "Roles", "Id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);

            Create.Table("RoleClaims")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("RoleId").AsString(450).NotNullable().ForeignKey("FK_RoleClaims_Roles", "Roles", "Id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("ClaimType").AsString().Nullable()
                .WithColumn("ClaimValue").AsString().Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);


            Create.Table("UserClaims")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("UserId").AsString(450).NotNullable().ForeignKey("FK_UserClaims_Users", "Users", "Id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("ClaimType").AsString().Nullable()
                .WithColumn("ClaimValue").AsString().Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);


            Create.Table("UserLogins")
                .WithColumn("LoginProvider").AsString(128).PrimaryKey()
                .WithColumn("ProviderKey").AsString(128).PrimaryKey()
                .WithColumn("ProviderDisplayName").AsString().Nullable()
                .WithColumn("UserId").AsString(450).NotNullable().ForeignKey("FK_UserLogins_Users", "Users", "Id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);


            Create.Table("UserTokens")
                .WithColumn("UserId").AsString(450).PrimaryKey().ForeignKey("FK_UserTokens_Users", "Users", "Id").OnDelete(System.Data.Rule.Cascade)
                .WithColumn("LoginProvider").AsString(128).PrimaryKey()
                .WithColumn("Name").AsString(128).PrimaryKey()
                .WithColumn("Value").AsString().Nullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);
        }

        public override void Down()
        {
            // Brišemo tabele obrnutim redoslijedom zbog stranih ključeva (Foreign Keys)
            Delete.Table("UserTokens");
            Delete.Table("UserLogins");
            Delete.Table("UserClaims");
            Delete.Table("RoleClaims");
            Delete.Table("UserRoles");
            Delete.Table("Roles");
            Delete.Table("Users");
        }
    }
}
