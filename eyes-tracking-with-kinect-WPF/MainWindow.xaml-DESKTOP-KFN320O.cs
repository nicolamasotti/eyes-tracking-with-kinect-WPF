using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace KinectHDFEyeTracking
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Provides a Kinect sensor reference.
        private KinectSensor _sensor = null;

        // Acquires body frame data.
        private BodyFrameSource _bodySource = null;

        // Reads body frame data.
        private BodyFrameReader _bodyReader = null;

        // Acquires HD face data.
        private HighDefinitionFaceFrameSource _faceSource = null;

        // Reads HD face data.
        private HighDefinitionFaceFrameReader _faceReader = null;

        // Required to access the face vertices.
        private FaceAlignment _faceAlignment = null;

        // Required to access the face model points.
        private FaceModel _faceModel = null;

        // Used to display 1,000 points on screen.
        private List<Ellipse> _points = new List<Ellipse>();


        //------------
        //output configuration

        //setting socket to send output stream. ProtocolType is set to Udp
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //setting IP to send stream to. as I have to send the stream to this very same machine, ip is in Loopback 
        IPAddress IP = IPAddress.Loopback;
        //setting endpoint
        IPEndPoint ep;
        //------------


            
        


        public MainWindow()
        {
            InitializeComponent();

            
            //configuring end point
            ep = new IPEndPoint(IP, 9999);

            //initializing KinectSensor
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                // Listen for body data.
                _bodySource = _sensor.BodyFrameSource;
                _bodyReader = _bodySource.OpenReader();
                _bodyReader.FrameArrived += BodyReader_FrameArrived;

                // Listen for HD face data.
                _faceSource = new HighDefinitionFaceFrameSource(_sensor);
                _faceReader = _faceSource.OpenReader();
                _faceReader.FrameArrived += FaceReader_FrameArrived;

                _faceModel = new FaceModel();
                _faceAlignment = new FaceAlignment();

                // Start tracking!        
                _sensor.Open();
            }
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    Body[] bodies = new Body[frame.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);

                    Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (!_faceSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                        }
                    }
                }
            }
        }

        private void FaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null && frame.IsFaceTracked)
                {
                    frame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                    UpdateFacePoints();
                }
            }
        }

        private void UpdateFacePoints()
        {
            

            if (_faceModel == null) return;

            var vertices = _faceModel.CalculateVerticesForAlignment(_faceAlignment);

            //introducing time association
            DateTime Now = DateTime.Now;
            consoleNow.Text = String.Format("Actual date and time are: {0: MM/dd/yyy HH:mm:ss.fff}", Now);
            string NowString = Now.ToString("MM/dd/yyy HH:mm:ss.fff");
            

            //NoseTip position
            var NoseTip = vertices[(int)HighDetailFacePoints.NoseTip];
            //convert to single
            Single NoseTipX = NoseTip.X;
            Single NoseTipY = NoseTip.Y;
            Single NoseTipZ = NoseTip.Z;

            string NoseTipx = NoseTipX.ToString("0.0000");
            string NoseTipy = NoseTipY.ToString("0.0000");
            string NoseTipz = NoseTipZ.ToString("0.0000");

            consoleNoseTip.Text = String.Format("Your Nose Tip coordinates are: \n X: {0} \n Y: {1} \n Z: {2} \n", NoseTipx, NoseTipy, NoseTipz);


            //EyeLeft position

            //Catch outer eye corner position
            var LefteyeOutercorner = vertices[(int)HighDetailFacePoints.LefteyeOutercorner];
            //convert to single
            Single LefteyeOutercornerX = LefteyeOutercorner.X;
            Single LefteyeOutercornerY = LefteyeOutercorner.Y;
            Single LefteyeOutercornerZ = LefteyeOutercorner.Z;

            //Catch inner eye corner position
            var LefteyeInnercorner = vertices[(int)HighDetailFacePoints.LefteyeInnercorner];
            //convert to single
            Single LefteyeInnercornerX = LefteyeInnercorner.X;
            Single LefteyeInnercornerY = LefteyeInnercorner.Y;
            Single LefteyeInnercornerZ = LefteyeInnercorner.Z;

            //Calculate eye position as a mean between outer and inner eye corner
            Single LefteyeX = (LefteyeInnercornerX + LefteyeOutercornerX) / 2;
            Single LefteyeY = (LefteyeInnercornerY + LefteyeOutercornerY) / 2;
            Single LefteyeZ = (LefteyeInnercornerZ + LefteyeOutercornerZ) / 2;

            string Lefteyex = LefteyeX.ToString("0.0000");
            string Lefteyey = LefteyeY.ToString("0.0000");
            string Lefteyez = LefteyeZ.ToString("0.0000");

            consoleEyeLeft.Text = String.Format("Your Left eye coordinates are: \n X: {0} \n Y: {1} \n Z: {2} \n", Lefteyex, Lefteyey, Lefteyez);



            //EyeRight position

            //Catch outer eye corner position
            var RighteyeOutercorner = vertices[(int)HighDetailFacePoints.RighteyeOutercorner];
            //convert to single
            Single RighteyeOutercornerX = RighteyeOutercorner.X;
            Single RighteyeOutercornerY = RighteyeOutercorner.Y;
            Single RighteyeOutercornerZ = RighteyeOutercorner.Z;

            //Catch inner eye corner position
            var RighteyeInnercorner = vertices[(int)HighDetailFacePoints.RighteyeInnercorner];
            //convert to single
            Single RighteyeInnercornerX = RighteyeInnercorner.X;
            Single RighteyeInnercornerY = RighteyeInnercorner.Y;
            Single RighteyeInnercornerZ = RighteyeInnercorner.Z;

            //Calculate eye position as a mean between outer and inner eye corner
            Single RighteyeX = (RighteyeInnercornerX + RighteyeOutercornerX) / 2;
            Single RighteyeY = (RighteyeInnercornerY + RighteyeOutercornerY) / 2;
            Single RighteyeZ = (RighteyeInnercornerZ + RighteyeOutercornerZ) / 2;

            string Righteyex = RighteyeX.ToString("0.0000");
            string Righteyey = RighteyeY.ToString("0.0000");
            string Righteyez = RighteyeZ.ToString("0.0000");

            consoleEyeRight.Text = String.Format("Your Right eye coordinates are: \n X: {0} \n Y: {1} \n Z: {2} \n", Righteyex, Righteyey, Righteyez);


            //interocular distance

            //Calculate interocular distance 
            Single EyedistanceX = RighteyeX - LefteyeX;
            Single EyedistanceY = RighteyeY - LefteyeY;
            Single EyedistanceZ = RighteyeZ - LefteyeZ;
            //"S" denotes "square": for computational cost it's better to use the square distance 
            //and calculate the squareroot only if need.
            //for the same reason, it's better to calculate a square as a product instead of as a power 2.
            Single EyedistanceS = EyedistanceX * EyedistanceX + EyedistanceY * EyedistanceY + EyedistanceZ * EyedistanceZ;
            //extract squareroot
            double EyedistanceR = Math.Sqrt((double)EyedistanceS);
            //convert to string
            string Eyedistancer = EyedistanceR.ToString("0.0000");

            consoleEyedistancer.Text = String.Format("Your eyes distance is: \n D: {0}", Eyedistancer);


            //create a .txt file with captured data (eg.: eyedistance) coupled with time reference,
            //due to be able to plot them on a chart
            
            //creating a string with eyedistance data associated with time
            string EyePositionTimeol = NowString + "\t\t" + "ED= " + Eyedistancer + "\t\t" + "RX= " + Righteyex + "\t\t" + "RY= " + Righteyey + "\t\t" + "RZ= " + Righteyez + "\t\t" + "LX= " + Lefteyex + "\t\t" + "LY= " + Lefteyey + "\t\t" + "LZ= " + Lefteyez + "\n";
            //writing into .txt file
            string TxtPath = "C:\\Users\\simon\\Desktop\\";
            string TxtName = "EyePositionTime.txt";
            using (StreamWriter outputFile = new StreamWriter(TxtPath + @"\" + TxtName, true))
            {
                outputFile.WriteLine(EyePositionTimeol);
            }
            

            
            //send to socket
            string message = Lefteyex + "\n" + Lefteyey + "\n" + Lefteyez + "\n" + Righteyex + "\n" + Righteyey + "\n" + Righteyez + "\n" + NoseTipx + "\n" + NoseTipy + "\n" + NoseTipz;

            byte[] stream = Encoding.UTF8.GetBytes(message);
            socket.SendTo(stream, ep);
            


            //drawing vertices to points

            //create an ellipse for each visible vertix
            if (vertices.Count > 0)
            {
                if (_points.Count == 0)
                {
                    for (int index = 0; index < vertices.Count; index++)
                    {
                        Ellipse ellipse = new Ellipse
                        {
                            Width = 2.0,
                            Height = 2.0,
                            Fill = new SolidColorBrush(Colors.Blue)
                        };

                        _points.Add(ellipse);
                    }

                    foreach (Ellipse ellipse in _points)
                    {
                        canvas.Children.Add(ellipse);
                    }
                }

                for (int index = 0; index < vertices.Count; index++)
                {
                    CameraSpacePoint vertice = vertices[index];
                    DepthSpacePoint point = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertice);

                    if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return;

                    Ellipse ellipse = _points[index];

                    Canvas.SetLeft(ellipse, point.X);
                    Canvas.SetTop(ellipse, point.Y);
                }
            }

            //draw eyes on mapXZ
            

            Ellipse MXZeyeR = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Red };
            Ellipse MXZeyeL = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Green };

            Canvas.SetTop(MXZeyeR, 50 * RighteyeZ);
            Canvas.SetLeft(MXZeyeR, 180 + 50 * RighteyeX);
            Canvas.SetTop(MXZeyeL, 50 * LefteyeZ);
            Canvas.SetLeft(MXZeyeL, 180 + 50 * LefteyeX);
            
            TranslateTransform trMXZEyeR = new TranslateTransform(50 * RighteyeX, 50 * RighteyeZ);
            TranslateTransform trMXZEyeL = new TranslateTransform(50 * LefteyeX, 50 * LefteyeZ);
            MXZeyeR.RenderTransform = trMXZEyeR;
            MXZeyeL.RenderTransform = trMXZEyeL;
            mapXZ.Children.Add(MXZeyeR);
            mapXZ.Children.Add(MXZeyeL);

            //draw eyes on mapXY

            Ellipse MXYeyeR = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Red };
            Ellipse MXYeyeL = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Green };

            Canvas.SetTop(MXYeyeR, 145 - 50 * RighteyeY);
            Canvas.SetLeft(MXYeyeR, 180 + 50 * RighteyeX);
            Canvas.SetTop(MXYeyeL, 145 - 50 * LefteyeY);
            Canvas.SetLeft(MXYeyeL, 180 + 50 * LefteyeX);

            TranslateTransform trMXYEyeR = new TranslateTransform(50 * RighteyeX, -50 * RighteyeY);
            TranslateTransform trMXYEyeL = new TranslateTransform(50 * LefteyeX, -50 * LefteyeY);
            MXYeyeR.RenderTransform = trMXYEyeR;
            MXYeyeL.RenderTransform = trMXYEyeL;
            mapXY.Children.Add(MXYeyeR);
            mapXY.Children.Add(MXYeyeL);



            //seconda prova con uso di animazione per muovere l'ellisse
            //eyeR.Duration = new Duration(TimeSpan.)
            //mapXZ.Children.Remove(eyeL);

        }

        
    }
}
