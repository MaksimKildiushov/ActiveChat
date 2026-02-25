namespace Ac.Abstractions.Helpers;

public static class SysAccountsHlp
{
    public static class Ai
    {
        public static Guid Id = new Guid("fade0000-7807-43cc-b73f-3f0c6d3c3151");
        public const string Name = "ai@service.account";
    }

    public static class Auth
    {
        public static Guid Id = new Guid("fade0001-7807-43cc-b73f-3f0c6d3c3151");
        public const string Name = "auth@service.account";
    }

    public static class Api
    {
        public static Guid Id = new Guid("fade0002-7807-43cc-b73f-3f0c6d3c3151");
        public const string Name = "api@service.account";
    }
    
    public static class Channel
    {
        public static Guid Id = new Guid("fade0004-7807-43cc-b73f-3f0c6d3c3151");
        public const string Name = "channel@service.account";
    }

    public static class Job
    {
        public static Guid Id = new Guid("fade0005-7807-43cc-b73f-3f0c6d3c3151");
        public const string Name = "job@service.account";
    }
}
