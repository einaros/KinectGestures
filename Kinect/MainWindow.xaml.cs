namespace Kinect
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Research.Kinect.Nui;
    using NAudio.Wave;
    using Vector = Microsoft.Research.Kinect.Nui.Vector;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Camera _camera;
        private readonly List<IGesture> _gestures;
        private readonly Runtime _kinect;
        private readonly Dictionary<string, string> _soundLibrary;
        private double _dampeningFactor;
        private Body _smoothBody;

        public MainWindow()
        {
            InitializeComponent();
            var device = new Device();
            _kinect = new Runtime(0);
            _kinect.Initialize(RuntimeOptions.UseSkeletalTracking);
            _kinect.SkeletonFrameReady += OnFrameReady;
            _camera = _kinect.NuiCamera;
            InitializeTiltSlider();
            _soundLibrary = new Dictionary<string, string>
                                {
                                    {"comedy", @"SoundEffects\comedy.mp3"},
                                    {"trombone", @"SoundEffects\trombone.mp3"},
                                    {"punch", @"SoundEffects\punch.mp3"},
                                    {"entry", @"SoundEffects\entry.mp3"},
                                };
            _gestures = new List<IGesture>
                            {
                                new JointOverHeadGesture(JointID.HandLeft, () => PlaySound("trombone")),
                                new JointOverHeadGesture(JointID.HandRight, () => PlaySound("comedy")),
                                new HorizontalPunchLikeGesture(0.40f, JointID.HandRight, () => PlaySound("punch")),
                                new HorizontalPunchLikeGesture(0.40f, JointID.HandLeft, () => PlaySound("punch")),
                            };
        }

        public void PlaySound(string name, Action done = null)
        {
            FileStream ms = File.OpenRead(_soundLibrary[name]);
            var rdr = new Mp3FileReader(ms);
            WaveStream wavStream = WaveFormatConversionStream.CreatePcmStream(rdr);
            var baStream = new BlockAlignReductionStream(wavStream);
            var waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(baStream);
            waveOut.Play();
            var bw = new BackgroundWorker();
            bw.DoWork += (s, o) =>
                             {
                                 while (waveOut.PlaybackState == PlaybackState.Playing)
                                 {
                                     Thread.Sleep(100);
                                 }
                                 waveOut.Dispose();
                                 baStream.Dispose();
                                 wavStream.Dispose();
                                 rdr.Dispose();
                                 ms.Dispose();
                                 if (done != null) done();
                             };
            bw.RunWorkerAsync();
        }

        private void DrawPositions(Body body)
        {
            dotGreen.SetValue(Canvas.LeftProperty, 0.5*(canvas.ActualWidth + canvas.ActualWidth*body.Head.X));
            dotGreen.SetValue(Canvas.TopProperty, 0.5*(canvas.ActualHeight - canvas.ActualHeight*body.Head.Y));
            dotBlue.SetValue(Canvas.LeftProperty, 0.5*(canvas.ActualWidth + canvas.ActualWidth*body.RightHand.X));
            dotBlue.SetValue(Canvas.TopProperty, 0.5*(canvas.ActualHeight - canvas.ActualHeight*body.RightHand.Y));
            dotRed.SetValue(Canvas.LeftProperty, 0.5*(canvas.ActualWidth + canvas.ActualWidth*body.LeftHand.X));
            dotRed.SetValue(Canvas.TopProperty, 0.5*(canvas.ActualHeight - canvas.ActualHeight*body.LeftHand.Y));
        }

        private void InitializeTiltSlider()
        {
            try
            {
                Observable.FromEventPattern<
                    RoutedPropertyChangedEventHandler<double>,
                    RoutedPropertyChangedEventArgs<double>>(
                        x => tiltSlider.ValueChanged += x,
                        x => tiltSlider.ValueChanged -= x)
                    .Throttle(TimeSpan.FromSeconds(1))
                    .Subscribe(a => _camera.ElevationAngle = (int) a.EventArgs.NewValue);
                tiltSlider.Value = _camera.ElevationAngle;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void OnDampeningChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _dampeningFactor = e.NewValue/10;
        }

        private void OnFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            string status = "";
            try
            {
                status += "Skeletons: " + e.SkeletonFrame.Skeletons.Count() + "\n";
                IEnumerable<SkeletonData> tracked = e.SkeletonFrame.Skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked);
                SkeletonData skeleton = tracked.FirstOrDefault();
                if (skeleton == null) return;
                status += "Has tracked skeleton\n";
                Body currentBody = Body.FromSkeleton(skeleton);
                SmoothenBody(currentBody);
                DrawPositions(_smoothBody);
                foreach (IGesture gesture in _gestures)
                {
                    gesture.ProcessBodyFrame(_smoothBody);
                }
            }
            finally
            {
                UpdateStatus(status);
            }
        }

        private void SmoothenBody(Body currentBody)
        {
            if (_smoothBody == null) _smoothBody = currentBody;
            else
            {
                Vector head = _smoothBody.Head;
                head.X = (float) (_smoothBody.Head.X + _dampeningFactor*(currentBody.Head.X - _smoothBody.Head.X));
                head.Y = (float) (_smoothBody.Head.Y + _dampeningFactor*(currentBody.Head.Y - _smoothBody.Head.Y));
                _smoothBody.Head = head;
                Vector left = _smoothBody.LeftHand;
                left.X = (float) (_smoothBody.LeftHand.X + _dampeningFactor*(currentBody.LeftHand.X - _smoothBody.LeftHand.X));
                left.Y = (float) (_smoothBody.LeftHand.Y + _dampeningFactor*(currentBody.LeftHand.Y - _smoothBody.LeftHand.Y));
                _smoothBody.LeftHand = left;
                Vector right = _smoothBody.RightHand;
                right.X = (float) (_smoothBody.RightHand.X + _dampeningFactor*(currentBody.RightHand.X - _smoothBody.RightHand.X));
                right.Y = (float) (_smoothBody.RightHand.Y + _dampeningFactor*(currentBody.RightHand.Y - _smoothBody.RightHand.Y));
                _smoothBody.RightHand = right;
            }
        }

        private void UpdateStatus(string status)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateStatus(status)));
                return;
            }
            outputTextbox.Text = status;
        }
    }
}