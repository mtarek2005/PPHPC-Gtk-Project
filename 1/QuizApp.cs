using Gtk;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

class QuizApp
{
    private Builder builder;
    private HttpClient httpClient;
    private string baseUri="http://localhost:8080";

    // Selection screen
    private Window selectionWindow;
    private TreeView quizListView;
    private Button startQuizButton;
    private ListStore quizStore;
    private List<Quiz> quizzes;

    // Quiz-taking screen
    private Window mainWindow;
    private Label questionLabel;
    private RadioButton option1, option2, option3, option4;
    private Button submitButton;
    private Label resultLabel;
    private int currentQuizId;
    private List<QuizQuestion> currentQuestions;
    private QuizQuestion currentQuestion;
    private int currentQuestionIndex;
    private int grade;
    private int id;

    private Window successWindow;
    private Label successLabel;
    private Button successButton;

    public QuizApp(int id=0)
    {
        this.id=id;
        Console.WriteLine(id);
        
        // Initialize HTTP client
        httpClient = new HttpClient();

        // Load UI from Glade
        builder = new Builder();
        builder.AddFromFile("QuizUI.glade");

        // Initialize Quiz Selection UI
        selectionWindow = (Window)builder.GetObject("SelectionWindow");
        quizListView = (TreeView)builder.GetObject("QuizListView");
        startQuizButton = (Button)builder.GetObject("StartQuizButton");

        // Set up quiz list
        quizStore = new ListStore(typeof(string), typeof(int)); // Quiz name and ID
        quizListView.Model = quizStore;

        // Configure quiz list view
        TreeViewColumn column = new TreeViewColumn { Title = "Quiz Name" };
        CellRendererText cell = new CellRendererText();
        column.PackStart(cell, true);
        column.AddAttribute(cell, "text", 0);
        quizListView.AppendColumn(column);

        startQuizButton.Clicked += OnStartQuizClicked;

        // Initialize Quiz Taking UI
        mainWindow = (Window)builder.GetObject("MainWindow");
        questionLabel = (Label)builder.GetObject("QuestionLabel");
        option1 = (RadioButton)builder.GetObject("Option1");
        option2 = (RadioButton)builder.GetObject("Option2");
        option3 = (RadioButton)builder.GetObject("Option3");
        option4 = (RadioButton)builder.GetObject("Option4");
        submitButton = (Button)builder.GetObject("SubmitButton");
        resultLabel = (Label)builder.GetObject("ResultLabel");

        submitButton.Clicked += OnSubmitClicked;

        successWindow = (Window)builder.GetObject("SuccessWindow");
        successLabel = (Label)builder.GetObject("SuccessLabel");
        successButton = (Button)builder.GetObject("SuccessButton");

        successButton.Clicked += OnDoneClicked;

        selectionWindow.Title+=" | User "+id;
        mainWindow.Title+=" | User "+id;
        successWindow.Title+=" | User "+id;
        
        // Show selection screen
        selectionWindow.ShowAll();
        LoadQuizList();
    }

    private async void LoadQuizList()
    {
        try
        {
            string apiUrl = baseUri+"/api/quizzes";
            string response = await httpClient.GetStringAsync(apiUrl);
            quizzes = JsonConvert.DeserializeObject<List<Quiz>>(response);
            Application.Invoke((sender,e)=>{
                foreach (var quiz in quizzes)
                {
                    quizStore.AppendValues(quiz.Name, quiz.Id);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading quizzes: {ex.Message}");
        }
    }

    private async void OnStartQuizClicked(object sender, EventArgs e)
    {
        if (quizListView.Selection.GetSelected(out TreeIter iter))
        {
            int selectedQuizId = (int)quizStore.GetValue(iter, 1);
            currentQuizId = selectedQuizId;
            try
            {
                string apiUrl = baseUri+$"/api/quiz/{selectedQuizId}";
                string response = await httpClient.GetStringAsync(apiUrl);
                currentQuestions = JsonConvert.DeserializeObject<List<QuizQuestion>>(response);
                grade=0;
            }
            catch (Exception ex)
            {
                resultLabel.Text = $"Error fetching question: {ex.Message}";
            }
            LoadNextQuestion(0);
            selectionWindow.Hide();
            mainWindow.ShowAll();
        }
        else
        {
            Console.WriteLine("No quiz selected.");
        }
    }

    private void LoadNextQuestion(int index)
    {
        try
        {
            currentQuestionIndex=index;
            currentQuestion = currentQuestions[index];

            questionLabel.Text = currentQuestion.Question;
            option1.Label = currentQuestion.Options[0];
            option2.Label = currentQuestion.Options[1];
            option3.Label = currentQuestion.Options[2];
            option4.Label = currentQuestion.Options[3];
        }
        catch (Exception ex)
        {
            resultLabel.Text = $"Error fetching question: {ex.Message}";
        }
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        RadioButton selectedOption = null;
        if (option1.Active) selectedOption = option1;
        if (option2.Active) selectedOption = option2;
        if (option3.Active) selectedOption = option3;
        if (option4.Active) selectedOption = option4;

        if (selectedOption == null)
        {
            resultLabel.Text = "Please select an answer.";
            return;
        }

        try
        {
            var answerData = new AnswerSubmission { UserId = id, QuizId = currentQuizId, QuestionId = currentQuestion.Id, Answer = selectedOption.Label };
            string json = JsonConvert.SerializeObject(answerData);
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            string apiUrl = baseUri+"/api/submit";
            HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
            string result = await response.Content.ReadAsStringAsync();
            bool res=JsonConvert.DeserializeObject<bool>(result);
            if(res)grade++;
            resultLabel.Text = $"Result: {result}";
            if(currentQuestionIndex+1<currentQuestions.Count) LoadNextQuestion(currentQuestionIndex+1);
            else{
                successLabel.Text = $"Successfuly finished quiz!\nYou got {grade} out of {currentQuestions.Count}";
                mainWindow.Hide();
                successWindow.ShowAll();
            }
        }
        catch (Exception ex)
        {
            resultLabel.Text = $"Error submitting answer: {ex.Message}";
            Console.WriteLine(ex.StackTrace);
        }
    }
    private void OnDoneClicked(object sender, EventArgs e)
    {
        successWindow.Hide();
    }
    // public static void Main()
    // {
    //     Application.Init();
    //     new QuizApp();
    //     Application.Run();
    // }
}


