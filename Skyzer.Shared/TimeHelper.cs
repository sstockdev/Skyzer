using System.Text;

namespace Skyzer.Shared
{
    public static class TimeHelper
    {
        /// <summary>
        /// Helper method that takes in milliseconds since unix epoch and returns the associated
        /// DateTime object (UTC).
        /// </summary>
        public static DateTime UnixEpochToUTCDateTime(long millisecondsSinceEpoch)
        {
            return DateTime.UnixEpoch.AddMilliseconds(millisecondsSinceEpoch);
        }

        // Source - https://stackoverflow.com/a/5438743
        // Posted by Harry Steinhilber, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-09-01, License - CC BY-SA 3.0

        public static string ToPrettyFormat(this TimeSpan span)
        {

            if (span == TimeSpan.Zero) return "0 minutes";

            var sb = new StringBuilder();
            if (span.Days > 0)
                sb.AppendFormat("{0} day{1} ", span.Days, span.Days > 1 ? "s" : String.Empty);
            if (span.Hours > 0)
                sb.AppendFormat("{0} hour{1} ", span.Hours, span.Hours > 1 ? "s" : String.Empty);
            if (span.Minutes > 0)
                sb.AppendFormat("{0} minute{1} ", span.Minutes, span.Minutes > 1 ? "s" : String.Empty);
            return sb.ToString();

        }

    }
}
