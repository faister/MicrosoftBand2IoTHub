using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace MicrosoftBandFieldGateway
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application, INotifyPropertyChanged
    {
#if WINDOWS_PHONE_APP
        private TransitionCollection transitions;
#endif

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        public static new App Current
        {
            get { return (App)Application.Current; }
        }

        private string statusMessage = "Choose a duration and click Start to pair a Microsoft Band with your device.";
        public string StatusMessage
        {
            get { return statusMessage; }
            set
            {
                statusMessage = value;

                if (PropertyChanged != null)
                {
                    //PropertyChanged(this, new PropertyChangedEventArgs("StatusMessage"));
                    PropChange("StatusMessage");
                }
            }
        }

        private int _ingestDuration;
        public int IngestDuration
        {
            get { return _ingestDuration; }
            set
            {
                _ingestDuration = value;

                if (PropertyChanged != null)
                {
                    //PropertyChanged(this, new PropertyChangedEventArgs("IngestDuration"));
                    PropChange("IngestDuration");
                }
            }
        }

        private string _buttonContent;
        public string ButtonContent
        {
            get
            {
                return _buttonContent;
            }
            set
            {
                _buttonContent = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("ButtonContent"));
                PropChange("ButtonContent");
            }
        }

        private string _heartRate;
        public string HeartRate
        {
            get
            {
                return _heartRate;
            }
            set
            {
                _heartRate = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("HeartRate"));
                PropChange("HeartRate");
            }
        }

        private string _SkinTemperature;
        public string SkinTemperature
        {
            get
            {
                return _SkinTemperature;
            }
            set
            {
                _SkinTemperature = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("SkinTemperature"));
                PropChange("SkinTemperature");
            }
        }

        private string _Pedometer;
        public string Pedometer
        {
            get
            {
                return _Pedometer;
            }
            set
            {
                _Pedometer = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("Pedometer"));
                PropChange("Pedometer");
            }
        }


        private string _Distance;
        public string Distance
        {
            get
            {
                return _Distance;
            }
            set
            {
                _Distance = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("Distance"));
                PropChange("Distance");
            }
        }


        private string _Calories;
        public string Calories
        {
            get
            {
                return _Calories;
            }
            set
            {
                _Calories = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("Calories"));
                PropChange("Calories");
            }
        }

        private string _AirPressure;
        public string AirPressure
        {
            get
            {
                return _AirPressure;
            }
            set
            {
                _AirPressure = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("AirPressure"));
                PropChange("AirPressure");
            }
        }


        private string _GsrResistance;
        public string GsrResistance
        {
            get
            {
                return _GsrResistance;
            }
            set
            {
                _GsrResistance = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("GsrResistance"));
                PropChange("GsrResistance");

            }
        }

        private string _Brightness;
        public string Brightness
        {
            get
            {
                return _Brightness;
            }
            set
            {
                _Brightness = value;
                PropertyChanged(this, new PropertyChangedEventArgs("/*Brightness*/"));
                PropChange("Brightness");
            }
        }

        private string _AltimeterRate;
        public string AltimeterRate
        {
            get
            {
                return _AltimeterRate;
            }
            set
            {
                _AltimeterRate = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("AltimeterRate"));
                PropChange("AltimeterRate");
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
#if WINDOWS_PHONE_APP
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
#endif

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }
#endif

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private async void PropChange(string evt)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //HERE GOES THE UI ACCESS 
                PropertyChanged(this, new PropertyChangedEventArgs(evt));
            });
        }

    }
}