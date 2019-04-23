using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CefBrowserTest
{
    using System.Threading;
    using System.Windows.Threading;

    using CefSharp;

    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private int lastKnownQuestion = 14;

        public GameWindow()
        {
            InitializeComponent();
        }

        public async Task<JavascriptResponse> GetCurrentQuestionId()
        {
            var result = await this.EvaluateJavaScript("$('.question_list ul li').index($('.org_bg'))");
            return result;
        }

        public async Task StartTimer(CancellationToken ct)
        {
            for (int i = 10; i >= 0 && !ct.IsCancellationRequested; i--)
            {
                try
                {
                    await Task.Delay(1000, ct);

                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine(ex);
                }
                this.SecondsBackCounter.Content = i;
            }

            this.SecondsBackCounter.Content = string.Empty;
        }

        public async Task SelectAnswerAndWaitForNextQuestion(string answer)
        {
            var scriptX = $"$('#{answer.ToLower()}').offset().left;";
            var scriptY = $"$('#{answer.ToLower()}').offset().top;";
            var resX = await this.EvaluateJavaScript(scriptX);
            var resY = await this.EvaluateJavaScript(scriptY);
            var x = (int)resX.Result;
            var y = (int)resY.Result;
            MouseClick(x, y);
            await WaitForQuestionChangedAsync();
        }

        public void MouseClick(int x, int y)
        {
            this.ChromiumWebBrowser.GetBrowser().GetHost().SendMouseClickEvent(x, y, MouseButtonType.Left, false, 1, CefEventFlags.None);
            Thread.Sleep(15);
            this.ChromiumWebBrowser.GetBrowser().GetHost().SendMouseClickEvent(x, y, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }

        private async Task<JavascriptResponse> EvaluateJavaScript(string script)
        {
            JavascriptResponse response = null;

            if (this.ChromiumWebBrowser.CanExecuteJavascriptInMainFrame)
            {
                response = await this.ChromiumWebBrowser.EvaluateScriptAsync(script);
            }

            return response;
        }

        private async void ChromiumWebBrowser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.ChromiumWebBrowser.Visibility = Visibility.Hidden));
            }
            else
            {
                RemoveBlocksOnPage();
                this.ChromiumWebBrowser.ExecuteScriptAsync(File.ReadAllText(@"jquery-3.4.0.min.js"));
                //Thread.Sleep(1000);

                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => this.ChromiumWebBrowser.Visibility = Visibility.Visible));
            }
        }

        private async Task RemoveBlocksOnPage()
        {
            var script = "document.getElementsByTagName(\"h1\")[0].remove();"
                         + "document.getElementsByClassName(\"text\")[0].remove();"
                         + "document.getElementsByClassName(\"menu\")[0].remove();"
                         + "document.getElementsByClassName(\"st_banner\")[0].remove();"
                         + "document.getElementsByClassName(\"adsbygoogle\")[0].remove();"
                         + "document.getElementsByClassName(\"adsbygoogle\")[0].remove();"
                         + "document.getElementsByClassName(\"adsbygoogle\")[0].remove();"
                         + "document.getElementsByClassName(\"adsbygoogle\")[0].remove();"
                         + "document.getElementsByTagName(\"h2\")[0].remove();"
                         + "document.getElementsByClassName(\"copy\")[0].remove();";

            //await Task.Delay(5000);

            await this.EvaluateJavaScript(script);
        }

        private async void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            await SelectAnswerAndWaitForNextQuestion("a");
        }

        private async Task WaitForQuestionChangedAsync()
        {
            var currentQuestion = -1;

            do
            {
                var response = await this.GetCurrentQuestionId();
                currentQuestion = (int?) response.Result ?? lastKnownQuestion;
                await Task.Delay(300);
            } while (lastKnownQuestion.Equals(currentQuestion));

            lastKnownQuestion = currentQuestion;
        }
    }
}
