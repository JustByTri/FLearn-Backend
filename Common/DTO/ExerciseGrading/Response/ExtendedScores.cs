namespace Common.DTO.ExerciseGrading.Response
{
    public class ExtendedScores
    {
        public int Pronunciation { get; set; } // 0-10
        public int Fluency { get; set; } // 0-10
        public int Coherence { get; set; } // 0-10
        public int Accuracy { get; set; } // 0-10 (task accuracy)
        public int Intonation { get; set; } // 0-10
        public int Grammar { get; set; } // 0-10
        public int Vocabulary { get; set; } // 0-10
    }
}
