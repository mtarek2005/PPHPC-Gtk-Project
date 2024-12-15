using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace _1
{
    class AdminPanelWindow : Window{
        [UI] private Stack _windowStack = null;
        [UI] private Box _quizzesBox = null;
        [UI] private TreeView _quizList = null;
        [UI] private Entry _quizNameEntry = null;
        [UI] private Button _addQuizButton = null;
        [UI] private Button _answersButton = null;
        [UI] private Button _editQuizButton = null;
        [UI] private Button _deleteQuizButton = null;
        private int currentQuizId = -1;
        [UI] private Box _questionsBox = null;
        [UI] private Entry _quizNameEntry2 = null;
        [UI] private Box _questionsListBox = null;
        [UI] private Button _addQuestionButton = null;
        [UI] private Button _deleteQuestionButton = null;
        [UI] private Button _saveQuizButton = null;
        [UI] private Box _answersBox = null;
        [UI] private Label _quizNameLabel = null;
        [UI] private Label _submissionsListLabel = null;
        [UI] private Button _backButton = null;
        private ListStore quizStore = new ListStore(typeof(string), typeof(int));
        public AdminPanelWindow() : this(new Builder("AdminPanelWindow.glade")) { }

        private AdminPanelWindow(Builder builder) : base(builder.GetRawOwnedObject("AdminPanelWindow"))
        {
            builder.Autoconnect(this);
            _quizList.Model = quizStore;

            // Configure quiz list view
            TreeViewColumn column = new TreeViewColumn { Title = "Quiz Name" };
            CellRendererText cell = new CellRendererText();
            column.PackStart(cell, true);
            column.AddAttribute(cell, "text", 0);
            _quizList.AppendColumn(column);
            _addQuizButton.Clicked += _addQuizButton_Clicked;
            _answersButton.Clicked += _answersButton_Clicked;
            _editQuizButton.Clicked += _editQuizButton_Clicked;
            _deleteQuizButton.Clicked += _deleteQuizButton_Clicked;
            _saveQuizButton.Clicked += _saveQuizButton_Clicked;
            _addQuestionButton.Clicked += _addQuestionButton_Clicked;
            _deleteQuestionButton.Clicked += _deleteQuestionButton_Clicked;
            _backButton.Clicked += _backButton_Clicked;

            updateQuizList();
            
            _windowStack.VisibleChild=_quizzesBox;
        }
        void updateQuizList(){
            lock(SimpleHttpServer.quizzes){
                quizStore.Clear();
                foreach (var quiz in SimpleHttpServer.quizzes)
                {
                    quizStore.AppendValues(quiz.Name, quiz.Id);
                }
            }
        }
        private void _addQuizButton_Clicked(object sender, EventArgs a)
        {
            if (_quizNameEntry.Text.Length > 0)
            {
                string name=_quizNameEntry.Text;
                lock(SimpleHttpServer.quizzes){
                    int maxId = SimpleHttpServer.quizzes.Max(x => x.Id);
                    SimpleHttpServer.quizzes.Add(new Quiz{Name=name,Id=maxId+1});
                    SimpleHttpServer.quizQuestions[maxId+1]=new ();
                }
                updateQuizList();
                _quizNameEntry.Text="";
                Task.Run(SimpleHttpServer.SaveData);
            }
        }
        private void _answersButton_Clicked(object sender, EventArgs a)
        {
            if (_quizList.Selection.GetSelected(out TreeIter iter))
            {
                int selectedQuizId = (int)quizStore.GetValue(iter, 1);
                currentQuizId=selectedQuizId;

                _submissionsListLabel.Text="";
                
                foreach (var user in SimpleHttpServer.answerSubmissions.Where((x)=>x.Value.ContainsKey(currentQuizId))){
                    int grade;
                    int total;
                    lock(SimpleHttpServer.quizQuestions[currentQuizId]) {
                        grade = user.Value[currentQuizId].Where(x=>SimpleHttpServer.quizQuestions[currentQuizId].Any(q=>q.Question.Id==x.Key&&q.Answer==x.Value.Answer)).Count();
                        total=SimpleHttpServer.quizQuestions[currentQuizId].Count;
                        
                    }
                    _submissionsListLabel.Text += $"User {user.Key} got {grade} out of {total}\n";
                    Console.WriteLine($"g{grade},{total}");
                }
                _quizNameLabel.Text=SimpleHttpServer.quizzes.Find(x => x.Id==currentQuizId).Name;
                _windowStack.VisibleChild=_answersBox;
            }
        }
        private void _editQuizButton_Clicked(object sender, EventArgs a)
        {
            if (_quizList.Selection.GetSelected(out TreeIter iter))
            {
                int selectedQuizId = (int)quizStore.GetValue(iter, 1);
                currentQuizId=selectedQuizId;
                _quizNameEntry2.Text=(string)quizStore.GetValue(iter, 0);
                List<QuestionRecord> currentQuizQuestions;
                lock(SimpleHttpServer.quizQuestions[currentQuizId]) currentQuizQuestions=SimpleHttpServer.quizQuestions[currentQuizId].ToList();
                foreach (var item in _questionsListBox.Children)
                {
                    _questionsListBox.Remove(item);
                }
                foreach (var question in currentQuizQuestions){
                    _questionsListBox.Add(new QuestionBox{QuestionData=question});
                }
                _windowStack.VisibleChild=_questionsBox;
            }
        }
        private void _deleteQuizButton_Clicked(object sender, EventArgs a)
        {
            if (_quizList.Selection.GetSelected(out TreeIter iter))
            {
                int selectedQuizId = (int)quizStore.GetValue(iter, 1);
                lock(SimpleHttpServer.quizzes){
                    int index = SimpleHttpServer.quizzes.FindIndex((q)=>q.Id == selectedQuizId);
                    SimpleHttpServer.quizzes.RemoveAt(index);
                    SimpleHttpServer.quizQuestions.Remove(index,out var value);
                }
                updateQuizList();
                Task.Run(SimpleHttpServer.SaveData);
            }
        }
        private void _saveQuizButton_Clicked(object sender, EventArgs a)
        {
            string quizName = _quizNameEntry2.Text;
            List<QuestionRecord> currentQuizQuestions = new();
            foreach (QuestionBox item in _questionsListBox.Children)
            {
                currentQuizQuestions.Add(item.QuestionData);
            }
            Task.Run(()=>{
                lock(SimpleHttpServer.quizzes) SimpleHttpServer.quizzes.Find(q=>q.Id==currentQuizId).Name=quizName;
                lock(SimpleHttpServer.quizQuestions[currentQuizId]) SimpleHttpServer.quizQuestions[currentQuizId]=currentQuizQuestions.ToList();
                SimpleHttpServer.SaveData();
                Application.Invoke((s,e)=>{
                    updateQuizList();
                    _windowStack.VisibleChild=_quizzesBox;
                });
            });
        }
        private void _addQuestionButton_Clicked(object sender, EventArgs a)
        {
            _questionsListBox.Add(new QuestionBox{QuestionData=new (new QuizQuestion{Id=_questionsListBox.Children.Max(q=>((QuestionBox)q).QuestionData.Question.Id)+1,Question="",Options=["","","",""]},"")});
        }
        private void _deleteQuestionButton_Clicked(object sender, EventArgs a)
        {
            foreach (var item in _questionsListBox.Children.Where(q=>((QuestionBox)q).Selected))
            {
                _questionsListBox.Remove(item);
            }
        }
        private void _backButton_Clicked(object sender, EventArgs a)
        {
            _windowStack.VisibleChild=_quizzesBox;
        }
    }
    class QuestionBox : Box {
        [UI] private Entry questionEntry=null;
        [UI] private CheckButton selectedCheckBox=null;
        [UI] private Entry option1Entry=null;
        [UI] private RadioButton option1RadioButton=null;
        [UI] private Entry option2Entry=null;
        [UI] private RadioButton option2RadioButton=null;
        [UI] private Entry option3Entry=null;
        [UI] private RadioButton option3RadioButton=null;
        [UI] private Entry option4Entry=null;
        [UI] private RadioButton option4RadioButton=null;
        private RadioButton[] options {get{return [option1RadioButton,option2RadioButton,option3RadioButton,option4RadioButton];}}
        public int OptionSelected {get{return options.ToList().FindIndex(x=>x.Active);} set{options[value].Active=true;}}
        private int id=-1;
        public bool Selected{ get{return selectedCheckBox.Active; } set{ selectedCheckBox.Active = value; }}
        public QuestionRecord QuestionData {
            get {
                QuizQuestion question = new QuizQuestion{Id=id,Question=questionEntry.Text,Options=[option1Entry.Text,option2Entry.Text,option3Entry.Text,option4Entry.Text]};
                QuestionRecord questionRecord = new(question,question.Options[OptionSelected]);
                return id>=0?questionRecord:null;
            }
            set {
                id=value.Question.Id;
                questionEntry.Text=value.Question.Question;
                option1Entry.Text=value.Question.Options[0];
                option2Entry.Text=value.Question.Options[1];
                option3Entry.Text=value.Question.Options[2];
                option4Entry.Text=value.Question.Options[3];
                OptionSelected=value.Question.Options.ToList().IndexOf(value.Answer);
            }
        }

        public QuestionBox() : this(new Builder("QuestionBox.glade")) { }

        private QuestionBox(Builder builder) : base(builder.GetRawOwnedObject("QuestionBox"))
        {
            builder.Autoconnect(this);
            QuestionData=new (new QuizQuestion{Id=-1,Question="",Options=["","","",""]},"");
        }
    }
}