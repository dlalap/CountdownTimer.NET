using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CountdownTimer.Models;

namespace CountdownTimer.ViewModels
{
    public class TaskViewModel : NotificationBase<CountdownTask>
    {
        private CountdownTask _task;
        private DateTime _future;
        private bool timerActive;
        private CancellationTokenSource _token;

        public TaskViewModel()
        {
            _task = new CountdownTask( );
            _future = DateTime.Now.AddMinutes(15);
            timerActive = true;
            _token = new CancellationTokenSource( );

            Task.Factory.StartNew(() => RunTimer(_token.Token), TaskCreationOptions.LongRunning);
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

        public async void RunTimer( CancellationToken _token )
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
