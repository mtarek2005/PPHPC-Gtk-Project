using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

class SimpleHttpServer
{
    private static HttpListener listener;
    private static string data_path="data.json";
    private static readonly object filelock =new();
    private static List<Quiz> quizzes = new();
    private static ConcurrentDictionary<int, List<QuestionRecord>> quizQuestions = new();
    private static ConcurrentDictionary<int, ConcurrentDictionary<int,ConcurrentDictionary<int,AnswerSubmission>>> answerSubmissions = new();

    public static void Run()
    {
        // Initialize quizzes and questions
        if(File.Exists(data_path)) LoadData();
        else InitializeData();

        // Create a new HttpListener instance
        listener = new HttpListener();

        // Add prefixes that this listener will respond to
        listener.Prefixes.Add("http://localhost:8080/");

        // Start the listener
        listener.Start();
        Console.WriteLine("Server is listening on http://localhost:8080/");

        // Handle requests asynchronously
        while (true)
        {
            var context = listener.GetContext();
            ThreadPool.QueueUserWorkItem(HandleRequest, context);
        }
    }

    private static void HandleRequest(object state)
    {
        var context = (HttpListenerContext)state;

        try
        {
            // Get the request
            var request = context.Request;
            var response = context.Response;

            Console.WriteLine($"Received request: {request.HttpMethod} {request.Url.AbsolutePath}");

            // Route the request based on the URL
            if (request.Url.AbsolutePath == "/api/quizzes" && request.HttpMethod == "GET")
            {
                HandleQuizzesRequest(response);
            }
            else if (request.Url.AbsolutePath.StartsWith("/api/quiz/") && request.HttpMethod == "GET")
            {
                HandleQuizRequest(request, response);
            }
            else if (request.Url.AbsolutePath == "/api/submit" && request.HttpMethod == "POST")
            {
                HandleSubmitRequest(request, response);
            }
            else
            {
                // Return 404 for unknown paths
                response.StatusCode = 404;
                byte[] buffer = Encoding.UTF8.GetBytes("Not Found");
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request: {ex.Message}");
        }
    }

    private static void HandleQuizzesRequest(HttpListenerResponse response)
    {
        response.ContentType = "application/json";

        string jsonResponse = JsonConvert.SerializeObject(quizzes);
        byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);

        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private static void HandleQuizRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        response.ContentType = "application/json";

        // Extract quiz ID from the URL
        string[] segments = request.Url.AbsolutePath.Split('/');
        Console.WriteLine("segment 4:"+segments[3]);
        
        if (segments.Length < 4 || !int.TryParse(segments[3], out int quizId) || !quizQuestions.ContainsKey(quizId))
        {
            response.StatusCode = 400;
            string errorResponse = "Invalid Quiz ID";
            byte[] errorBuffer = Encoding.UTF8.GetBytes(errorResponse); // Renamed variable to `errorBuffer`
            response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
            return;
        }

        // Get questions for the quiz
        List<QuizQuestion> questions;
        lock(quizQuestions[quizId]) questions = quizQuestions[quizId].ConvertAll((QuestionRecord x)=>x.Question);
        string jsonResponse = JsonConvert.SerializeObject(questions);

        byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse); // Ensure the variable `buffer` is unique in this scope
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
    }

    private static void HandleSubmitRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        response.ContentType = "application/json";

        // Read the request body
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            string body = reader.ReadToEnd();
            var submission = JsonConvert.DeserializeObject<AnswerSubmission>(body);
            QuestionRecord question;
            lock(quizQuestions[submission.QuizId]) question = quizQuestions[submission.QuizId].Find((QuestionRecord q)=>q.Question.Id == submission.QuestionId);
            var correct = question.Answer==submission.Answer;
            lock (answerSubmissions)
            {
                if(!answerSubmissions.ContainsKey(submission.UserId)){
                    answerSubmissions[submission.UserId]=new ();
                }
                if(!answerSubmissions[submission.UserId].ContainsKey(submission.QuizId)){
                    answerSubmissions[submission.UserId][submission.QuizId]=new ();
                }
                answerSubmissions[submission.UserId][submission.QuizId][submission.QuestionId]=submission;
            }
            SaveData();
            string result = JsonConvert.SerializeObject(correct);
            byte[] responseBuffer = Encoding.UTF8.GetBytes(result); 

            response.ContentLength64 = responseBuffer.Length;
            response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
        }
    }

    private static void InitializeData()
    {
        // Add sample quizzes
        quizzes.Add(new Quiz { Id = 1, Name = "General Knowledge" });
        quizzes.Add(new Quiz { Id = 2, Name = "Science Trivia" });

        // Add questions for each quiz
        quizQuestions[1] = new List<QuestionRecord>
        {
            new(new QuizQuestion { Id = 1, Question = "What is the capital of France?", Options = new[] { "Paris", "Berlin", "Madrid", "Rome" } },"Paris"),
            new(new QuizQuestion { Id = 2, Question = "Which planet is known as the Red Planet?", Options = new[] { "Earth", "Mars", "Jupiter", "Saturn" } },"Mars")
        };

        quizQuestions[2] = new List<QuestionRecord>
        {
            new(new QuizQuestion { Id = 3, Question = "What is the chemical symbol for water?", Options = new[] { "H2O", "O2", "CO2", "He" } },"H2O"),
            new(new QuizQuestion { Id = 4, Question = "What is the speed of light?", Options = new[] { "300,000 km/s", "150,000 km/s", "450,000 km/s", "600,000 km/s" } },"300,000 km/s")
        };
        SaveData();
    }
    private static void LoadData() {
        string content;
        lock(filelock) content=File.ReadAllText(data_path);
        var data = JsonConvert.DeserializeAnonymousType(content, new { quizzes, quizQuestions, answerSubmissions });
        quizzes=data.quizzes;
        quizQuestions=data.quizQuestions;
        answerSubmissions=data.answerSubmissions;
    }
    private static void SaveData() {
        string data;
        lock(answerSubmissions)data=JsonConvert.SerializeObject(new { quizzes, quizQuestions, answerSubmissions });
        lock(filelock) File.WriteAllText(data_path,data);
    }
}
