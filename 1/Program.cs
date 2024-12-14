using System;
using System.Threading.Tasks;
using Gtk;

namespace _1
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var server = Task.Run(SimpleHttpServer.Run);
            Application.Init();

            var app = new Application("org._1._1", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            var win = new MainWindow();
            app.AddWindow(win);

            win.Show();
            Application.Run();
        }
    }
}
