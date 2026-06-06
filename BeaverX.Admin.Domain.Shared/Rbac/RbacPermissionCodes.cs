namespace BeaverX.Admin.Domain.Shared.Rbac;

public static class RbacPermissionCodes
{
    public const string SuperAdmin = "super_admin";

    public static class System
    {
        public const string Prefix = "system";

        public static class User
        {
            public const string List = "system:user:list";
            public const string Create = "system:user:create";
            public const string Update = "system:user:update";
            public const string Delete = "system:user:delete";
            public const string AssignRoles = "system:user:assign_roles";
            public const string ResetPassword = "system:user:reset_password";
        }

        public static class Role
        {
            public const string List = "system:role:list";
            public const string Create = "system:role:create";
            public const string Update = "system:role:update";
            public const string Delete = "system:role:delete";
            public const string AssignPermissions = "system:role:assign_permissions";
            public const string AssignMenus = "system:role:assign_menus";
        }

        public static class Permission
        {
            public const string List = "system:permission:list";
            public const string Create = "system:permission:create";
            public const string Update = "system:permission:update";
            public const string Delete = "system:permission:delete";
        }

        public static class Menu
        {
            public const string List = "system:menu:list";
            public const string Create = "system:menu:create";
            public const string Update = "system:menu:update";
            public const string Delete = "system:menu:delete";
        }
    }
}
