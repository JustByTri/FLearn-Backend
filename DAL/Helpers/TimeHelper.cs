namespace DAL.Helpers
{
    public static class TimeHelper
    {
        public static DateTime GetVietnamTime()
        {
            var utcNow = DateTime.UtcNow;

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
            }
            catch (InvalidTimeZoneException)
            {
                return utcNow.AddHours(7);
            }
        }
    }
}
