namespace DAL.Type
{
    public enum ExerciseType
    {
        None = 0,
        PhonemeDrill = 1,  // Luyện 1 âm cụ thể
        WordPronunciation = 2, // Đọc từ đơn -> phân tích phát âm
        SentencePronunciation = 3, // Đọc câu -> chấm từng từ, nhấn trọng âm
        ListenAndRepeat = 4, // Nghe audio -> nhắc lại
        ListeningMultipleChoice = 5,
        ListeningFillInTheBlank = 6,
        UnscrambleWords = 7,
    }
}
