namespace DAL.Type
{
    public enum LessonLogType
    {
        ContentRead = 1,    // user scrolled/read HTML content
        VideoProgress = 2,  // value = percent (0-100)
        PdfOpened = 3,      // opened pdf (value null)
        ExercisePassed = 4, // one exercise passed (value null or exerciseId in metadata)
        ExerciseFailed = 5, // failed attempt
        Generic = 99        // other events
    }
}
