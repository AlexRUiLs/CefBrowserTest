using System;
using System.Collections.Generic;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Task<JavascriptResponse> GetCurrentQuestionId()
        {
            var result = this.EvaluateJavaScript("$('.shag').index($('.shag_activ'))");
            return result;
        }

        private async Task<JavascriptResponse> EvaluateJavaScript(string script)
        {
            JavascriptResponse response = null;
            //Check if the browser can execute JavaScript and the ScriptTextBox is filled
            if (this.ChromiumWebBrowser.CanExecuteJavascriptInMainFrame)
            {
                //Evaluate javascript and remember the evaluation result
                response = await this.ChromiumWebBrowser.EvaluateScriptAsync(script)
                    .ContinueWith(t =>
                                   {
                                       var result = t.Result;
                                       return result;
                                   });
            }

            return response;
        }

        private async void ChromiumWebBrowser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(() => this.ChromiumWebBrowser.Visibility = Visibility.Hidden));
            }
            else
            {
                var script = "var element1 = document.getElementsByClassName(\"section1\")[0]; element1.parentNode.removeChild(element1);"
                             + "var element2 = document.getElementsByClassName(\"section2\")[0]; element2.parentNode.removeChild(element2);"
                             + "var element3 = document.getElementsByClassName(\"section3\")[0]; element3.parentNode.removeChild(element3);"
                             + "var element5 = document.getElementsByClassName(\"section5\")[0]; element5.parentNode.removeChild(element5);"
                             + "var element7 = document.getElementsByClassName(\"section7\")[0]; element7.parentNode.removeChild(element7);"
                             + "var element8 = document.getElementsByClassName(\"section8\")[0]; element8.parentNode.removeChild(element8);"
                             + "var element16 = document.getElementsByClassName(\"section16\")[0]; element16.parentNode.removeChild(element16);"
                             + "var elementFooter = document.getElementsByClassName(\"footer\")[0]; elementFooter.parentNode.removeChild(elementFooter);";

                var result = this.EvaluateJavaScript(script);

                //Thread.Sleep(1000);

                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(() => this.ChromiumWebBrowser.Visibility = Visibility.Visible));
            }
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var result = this.GetCurrentQuestionId();
        }
    }
}
