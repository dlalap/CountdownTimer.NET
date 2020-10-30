using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CountdownTimer.Models;
using Microsoft.VisualStudio.PlatformUI;
using Prism.Commands;

namespace CountdownTimer.ViewModels
{
    public class TaskViewModel : NotificationBase<CountdownTask>
    {
        private CountdownTask _task;
        private DateTime _future;
        private bool timerActive;
        private CancellationTokenSource _token;
        private CameraCapture cameraCapture;

        public TaskViewModel()
        {
            cameraCapture = new CameraCapture( );
            _task = new CountdownTask( );
            _future = DateTime.Now.AddMinutes(15);
            timerActive = true;
            _token = new CancellationTokenSource( );
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
            ButtonClick = new DelegateCommand(async () => await ResetTimerAsync());
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
            Task.Factory.StartNew(async () => await RunTimerAsync(_token.Token), TaskCreationOptions.LongRunning);
        }

        public string Name
        {
            get { return This.Name; }
            set
            {
                SetProperty(This.Name, value, () => This.Name = value);
            }
        }

        public string CurrentTime
        {
            get { return This.CurrentTime; }
            set
            {
                SetProperty(This.CurrentTime, value, () => This.CurrentTime = value);
            }
        }

        public DelegateCommand ButtonClick { get; set; }

        private async Task ResetTimerAsync()
        {
            _future = DateTime.Now.AddMinutes(15);
            await cameraCapture.CaptureImageAsync( );
        }

        public async Task RunTimerAsync( CancellationToken _token )
        {
            while ( true )
            {
                if ( timerActive )
                {
                    var _currTime = _future - DateTime.Now;

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var _semiParsedTime = _currTime.ToString("t", CultureInfo.CreateSpecificCulture("en-US"));
                            CurrentTime = string.Join("", _semiParsedTime.Skip(3).Take(9).ToArray( ));
                        });
                }
            }
        }
    }
}
