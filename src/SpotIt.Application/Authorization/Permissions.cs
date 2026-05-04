using System.Reflection;

namespace SpotIt.Application.Authorization;

public static class Permissions
{
    public static class Posts
    {
        public const string UpdateStatus = "posts:update_status";
    }

    public static class Analytics
    {
        public const string View = "analytics:view";
    }
    public static class Users
    {
        public const string Manage = "users:manage";
    }
    public static class Roles
    {
        public const string Manage="roles:manage";
    }
    public static IEnumerable<string>  GetAll()
    {
        return typeof(Permissions).GetNestedTypes().SelectMany(type=> type.GetFields()).Select(field=> (string) field.GetValue(null)!);
    }
}