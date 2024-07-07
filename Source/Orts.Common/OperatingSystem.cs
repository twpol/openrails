// COPYRIGHT 2009 - 2024 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace ORTS.Common
{
#if !NET5_0_OR_GREATER
    public class OperatingSystem
    {
        static bool IsOSVersionAtLeast(int major, int minor, int build, int revision)
        {
            var version = Environment.OSVersion.Version;
            if (version.Major != major) return version.Major >= major;
            if (version.Minor != minor) return version.Minor >= minor;
            if (version.Build != build) return version.Build >= build;
            return version.Revision >= revision;
        }

        public static bool IsOSPlatform(string platform) => "Windows".Equals(platform, StringComparison.OrdinalIgnoreCase);
        public static bool IsOSPlatformVersionAtLeast(string platform, int major, int minor = 0, int build = 0, int revision = 0) => IsOSPlatform(platform) && IsOSVersionAtLeast(major, minor, build, revision);
        public static bool IsLinux() => false;
        public static bool IsMacOS() => false;
        public static bool IsWindows() => true;
        public static bool IsWindowsVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0) => IsWindows() && IsOSVersionAtLeast(major, minor, build, revision);
    }
#endif
}
