using System;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace _1
{
    class MainWindow : Window
    {
        [UI] private Label _label1 = null;
        [UI] private Button _button1 = null;
        [UI] private SpinButton _spin1 = null;
        [UI] private Adjustment _adjustment1 = null;

        private int _counter;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            _button1.Clicked += Button1_Clicked;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void Button1_Clicked(object sender, EventArgs a)
        {
            int v=(int)_adjustment1.Value;
            Console.WriteLine(v);
            _counter++;
            _label1.Text = "Hello World! This button has been clicked " + _counter + " time(s).";
            //object lock=new();
            Parallel.For(0,v,(int i)=>{
                Application.Invoke((sender, e) =>
                {
                    new QuizApp(i);
                });
            });
        }
    }
}
