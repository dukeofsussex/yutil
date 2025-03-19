namespace yUtil
{
    using System;

    internal class CI
    {
        internal static bool Enabled { get; set; }

        internal static int ExitCode { get; set; }

        internal static void Load()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable("CI"), out bool ci) && ci)
            {
                Enabled = true;
            }
        }
    }
}
