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
            public const string AssignMenus = "system:role:assign_menus";
        }

        public static class Menu
        {
            public const string List = "system:menu:list";
            public const string Create = "system:menu:create";
            public const string Update = "system:menu:update";
            public const string Delete = "system:menu:delete";
        }

        public static class Dict
        {
            public const string List = "system:dict:list";

            public static class Type
            {
                public const string Create = "system:dict_type:create";
                public const string Update = "system:dict_type:update";
                public const string Delete = "system:dict_type:delete";
            }

            public static class Data
            {
                public const string Create = "system:dict_data:create";
                public const string Update = "system:dict_data:update";
                public const string Delete = "system:dict_data:delete";
            }
        }

        public static class Config
        {
            public const string List = "system:config:list";
            public const string Create = "system:config:create";
            public const string Update = "system:config:update";
            public const string Delete = "system:config:delete";
        }

        public static class Message
        {
            public const string Send = "system:message:send";
        }

        public static class Job
        {
            public const string List = "system:job:list";
            public const string Create = "system:job:create";
            public const string Update = "system:job:update";
            public const string Delete = "system:job:delete";
            public const string Trigger = "system:job:trigger";
        }

        public static class OnlineUser
        {
            public const string List = "system:online_user:list";
            public const string Kick = "system:online_user:kick";
        }
    }

    public static class Payment
    {
        public static class Channel
        {
            public const string List = "payment:channel:list";
            public const string Create = "payment:channel:create";
            public const string Update = "payment:channel:update";
            public const string Delete = "payment:channel:delete";
        }

        public static class Order
        {
            public const string List = "payment:order:list";
            public const string Create = "payment:order:create";
            public const string Query = "payment:order:query";
            public const string Close = "payment:order:close";
            public const string Refund = "payment:order:refund";
        }

        public static class Refund
        {
            public const string List = "payment:refund:list";
        }
    }

    public static class Ticket
    {
        public static class Work
        {
            public const string List = "ticket:work:list";
            public const string Create = "ticket:work:create";
            public const string Update = "ticket:work:update";
            public const string Delete = "ticket:work:delete";
            public const string Process = "ticket:work:process";
        }
    }
}
