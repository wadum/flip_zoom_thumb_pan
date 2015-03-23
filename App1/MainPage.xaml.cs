using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Microsoft.WindowsAzure.MobileServices;
using OnlineRandomForest;
using System.Threading;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        volatile TransformGroup _transformGroup;
        volatile MatrixTransform _previousTransform;
        volatile CompositeTransform _compositeTransform;
        volatile ScaleTransform _scaleTransform;
        volatile Accelerometer _accelerometer;
        volatile Gyrometer _gyrometer;
        uint _desiredReportInterval = 200;
        private double _scaleFactorUp = 0.2;
        private double _scaleFactorDown = 0.25;
        private double _zoomThreshold = 30;
        private static volatile AccelerometerReading _previousAccelReading;
        private static volatile GyrometerReading _previousGyroReading;
        static readonly object lockObject = new object();

        private int NOTHING = 0, ROTATE_LEFT = 1, ROTATE_RIGHT = 2, ZOOM_IN = 3, ZOOM_OUT = 4;
        private OnlineRandomForest.OnlineRandomForest orf;
        private IMobileServiceTable<OnlineRandomForest.Input> table;
        private TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs> _gyroEvent;
        private Inclinometer _inclinometer;
        private InclinometerReading _previousInclinometerReading;


        public MainPage()
        {
            this.InitializeComponent();

            ImageCanvas.ManipulationStarting += new ManipulationStartingEventHandler(ManipulateMe_ManipulationStarting);
            ImageCanvas.ManipulationStarted += new ManipulationStartedEventHandler(ManipulateMe_ManipulationStarted);
            ImageCanvas.ManipulationDelta += new ManipulationDeltaEventHandler(ManipulateMe_ManipulationDelta);
            ImageCanvas.ManipulationCompleted += new ManipulationCompletedEventHandler(ManipulateMe_ManipulationCompleted);
            ImageCanvas.ManipulationInertiaStarting += new ManipulationInertiaStartingEventHandler(ManipulateMe_ManipulationInertiaStarting);

            InitManipulationTransforms();
            InitAccelerometer();
            InitGyrometer();
            InitORF();
            InitInclinometer();
        }

        private async void InitORF()
        {
            orf = new OnlineRandomForest.OnlineRandomForest();

            Input item = new Input();
            table = App.MobileService.GetTable<Input>();
            await table.InsertAsync(item);

            item.Features = new List<double>(){1d};

            await table.UpdateAsync(item);
        }

        private void InitManipulationTransforms()
        {
            _transformGroup = new TransformGroup();
            _compositeTransform = new CompositeTransform();
            _scaleTransform = new ScaleTransform();
            _previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };

            _transformGroup.Children.Add(_previousTransform);
            _transformGroup.Children.Add(_compositeTransform);
            _transformGroup.Children.Add(_scaleTransform);

            ManipulateImage.RenderTransform = _transformGroup;
        }

        private void InitGyrometer()
        {
            _gyrometer = Gyrometer.GetDefault();
            _gyrometer.ReportInterval = _desiredReportInterval;
            //_gyroEvent = new TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs>(GyroReadingChanged);
            _gyrometer.ReadingChanged += _gyroEvent;
            _previousGyroReading = _gyrometer.GetCurrentReading();
        }

        async void GyroReadingChanged(object sender, GyrometerReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                GyrometerReading reading = e.Reading;

                double delta = reading.AngularVelocityX - _previousGyroReading.AngularVelocityX;

                if (delta > _zoomThreshold)
                {
                    Debug.WriteLine("reading up: " + delta);
                    Debug.WriteLine("next: " + reading.AngularVelocityX);
                    Debug.WriteLine("prev: " + _previousGyroReading.AngularVelocityX);
                    _compositeTransform.ScaleX = _compositeTransform.ScaleY += _scaleFactorUp;
                }
                else if (delta < -_zoomThreshold)
                {
                    Debug.WriteLine("reading down: " + delta);
                    _compositeTransform.ScaleX = _compositeTransform.ScaleY -= _scaleFactorDown;
                }
                _previousGyroReading = reading;
            });
        }

        private void InitInclinometer()
        {
            _inclinometer = Inclinometer.GetDefault();
            _inclinometer.ReportInterval = _desiredReportInterval;
            _inclinometer.ReadingChanged += new TypedEventHandler<Inclinometer, InclinometerReadingChangedEventArgs>(InclinometerReadingChanged); ;
            _previousInclinometerReading = _inclinometer.GetCurrentReading();
        }

        async void InclinometerReadingChanged(object sender, InclinometerReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _previousTransform.Matrix = _transformGroup.Value;

                Point center = _previousTransform.TransformPoint(new Point(0,0));
                _scaleTransform.CenterX = center.X;
                _scaleTransform.CenterY = center.Y;
                _scaleTransform.ScaleX = _scaleTransform.ScaleY = 1;

                double delta = e.Reading.PitchDegrees - _previousInclinometerReading.PitchDegrees;

                if (delta > _zoomThreshold)
                {
                    _scaleTransform.ScaleX = _scaleTransform.ScaleY += _scaleFactorUp;
                }
                else if (delta < -_zoomThreshold)
                {
                    _scaleTransform.ScaleX = _scaleTransform.ScaleY -= _scaleFactorDown;
                }
                _previousInclinometerReading = e.Reading;
            });
        }

        private void InitAccelerometer()
        {
            _accelerometer = Accelerometer.GetDefault();
            _accelerometer.ReportInterval = _desiredReportInterval;

            //_accelerometer.ReadingChanged += new TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(AccelReadingChanged);
            _previousAccelReading = _accelerometer.GetCurrentReading();
        }

        async private void AccelReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AccelerometerReading reading = e.Reading;
                //if (reading.AccelerationY > _zoomThreshold)
                //{
                //    GyrometerReading gr = _gyrometer.GetCurrentReading();
                //    Debug.WriteLine("Accelerometer x: " + reading.AccelerationX + ", y: " + reading.AccelerationY + ", z: " + reading.AccelerationZ);
                //    Debug.WriteLine("Gyrometer x: " + gr.AngularVelocityX + ", y: " + gr.AngularVelocityY + ", z: " + gr.AngularVelocityZ);
                //    if (gr.AngularVelocityX > 0)
                //    {
                //        _compositeTransform.ScaleX = _compositeTransform.ScaleY += _scaleFactorUp;
                //    }
                //    else
                //    {
                //        _compositeTransform.ScaleX = _compositeTransform.ScaleY -= _scaleFactorDown;
                //    }
                //}

                double delta = reading.AccelerationY - _previousAccelReading.AccelerationY;
                Debug.WriteLine("reading: " + delta);

                if (delta > _zoomThreshold)
                {
                    _compositeTransform.ScaleX = _compositeTransform.ScaleY += _scaleFactorUp;
                }
                else if (delta < -_zoomThreshold)
                {
                    _compositeTransform.ScaleX = _compositeTransform.ScaleY -= _scaleFactorDown;
                }
                _previousAccelReading = reading;
            });
        }


        void ManipulateMe_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            e.Handled = true;
        }

        void ManipulateMe_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        void ManipulateMe_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.Handled = true;
        }

        void ManipulateMe_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            _previousTransform.Matrix = _transformGroup.Value;

            Point center = _previousTransform.TransformPoint(new Point(e.Position.X, e.Position.Y));
            _compositeTransform.CenterX = center.X;
            _compositeTransform.CenterY = center.Y;

            double x = e.Delta.Translation.X;
            double y = e.Delta.Translation.Y;

            double scale = _transformGroup.Value.M11;

            GeneralTransform trans = ManipulateImage.TransformToVisual(null);
            Rect rect = trans.TransformBounds(new Rect(0, 0, ManipulateImage.ActualWidth, ManipulateImage.ActualHeight));

            // Prevent panning when reaching left and right
            if ((rect.Left + x > 0  && x > 0) || (rect.Right - ActualWidth + x < 0 && x < 0))
            {
                x = 0;
            }

            // Prevent panning when reaching top and bottom
            if ((rect.Top + y > 0 && y > 0) || (rect.Bottom - ActualHeight + y < 0 && y < 0))
            {
                y = 0;
            }

            _compositeTransform.ScaleX = _compositeTransform.ScaleY = e.Delta.Scale;
            _compositeTransform.TranslateX = x;
            _compositeTransform.TranslateY = y;

            e.Handled = true;
        }

        void ManipulateMe_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }
    }
}
