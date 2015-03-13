using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        TransformGroup _transformGroup;
        MatrixTransform _previousTransform;
        CompositeTransform _compositeTransform;
        Accelerometer _accelerometer;
        Gyrometer _gyrometer;
        uint _desiredReportInterval = 100;
        private double _scaleFactor = 0.2;
        private double _zoomThreshold = 0.15;


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
        }

        private void InitGyrometer()
        {
            _gyrometer = Gyrometer.GetDefault();
            _gyrometer.ReportInterval = _desiredReportInterval;

            //_gyrometer.ReadingChanged += new TypedEventHandler<Gyrometer, GyrometerReadingChangedEventArgs>(GyroReadingChanged);
        }

        private void InitManipulationTransforms()
        {
            _transformGroup = new TransformGroup();
            _compositeTransform = new CompositeTransform();
            _previousTransform = new MatrixTransform() { Matrix = Matrix.Identity };

            _transformGroup.Children.Add(_previousTransform);
            _transformGroup.Children.Add(_compositeTransform);

            ManipulateImage.RenderTransform = _transformGroup;
        }

        private void InitAccelerometer()
        {
            _accelerometer = Accelerometer.GetDefault();
            _accelerometer.ReportInterval = _desiredReportInterval;

            _accelerometer.ReadingChanged += new TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(AccelReadingChanged);
        }

        async private void AccelReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                AccelerometerReading reading = e.Reading;
                if (reading.AccelerationY > _zoomThreshold)
                {
                    GyrometerReading gr = _gyrometer.GetCurrentReading();
                    Debug.WriteLine("Accelerometer x: " + reading.AccelerationX + ", y: " + reading.AccelerationY + ", z: " + reading.AccelerationZ);
                    Debug.WriteLine("Gyrometer x: " + gr.AngularVelocityX + ", y: " + gr.AngularVelocityY + ", z: " + gr.AngularVelocityZ);
                    if (gr.AngularVelocityX > 0)
                    {
                        _compositeTransform.ScaleX = _compositeTransform.ScaleY += _scaleFactor;
                    }
                    else
                    {
                        _compositeTransform.ScaleX = _compositeTransform.ScaleY -= _scaleFactor;
                    }
                }
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
