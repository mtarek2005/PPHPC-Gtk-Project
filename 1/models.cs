// Data classes for quizzes and questions
class Quiz
{
    public int Id { get; set; }
    public string Name { get; set; }
}

class QuizQuestion
{
    public int Id { get; set; }
    public string Question { get; set; }
    public string[] Options { get; set; }
}

class AnswerSubmission
{
    public int UserId { get; set;}
    public int QuizId { get; set; }
    public int QuestionId { get; set; }
    public string Answer { get; set; }
}

record QuestionRecord(QuizQuestion Question, string Answer);

