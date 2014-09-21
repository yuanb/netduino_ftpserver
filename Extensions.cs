using System;
using Microsoft.SPOT;

namespace ntools.Networking
{
    public static class Extensions
    {
        public static string Replace(this string str, string what, string with)
        {
            int index = -1;

            while ((index = str.IndexOf(what)) != -1)
            {
                if (index > 0)
                {
                    str = str.Substring(0, index) + with + str.Substring(index + what.Length);
                }
            }

            return str;
        }
    }
}
