namespace System
{
    public static class VersionExtensions
    {
        public static Version FromString(string version, bool setDefaultToMax = false)
        {
            int major = 0;
            int minor = 0;
            int buuld = 0;
            int revision = 0;

            if (setDefaultToMax)
            {
                major = int.MaxValue;
                minor = int.MaxValue;
                buuld = int.MaxValue;
                revision = int.MaxValue;
            }

            var split = version.Split(".");
            if (split.Length > 3 && split[3].IsNullOrWhiteSpace().Not())
                revision = int.Parse(split[3]);
            if (split.Length > 2 && split[2].IsNullOrWhiteSpace().Not())
                buuld = int.Parse(split[2]);
            if (split.Length > 1 && split[1].IsNullOrWhiteSpace().Not())
                minor = int.Parse(split[1]);
            if (split.Length > 0 && split[0].IsNullOrWhiteSpace().Not())
                major = int.Parse(split[0]);

            return new Version(major, minor, buuld, revision);
        }
    }
}