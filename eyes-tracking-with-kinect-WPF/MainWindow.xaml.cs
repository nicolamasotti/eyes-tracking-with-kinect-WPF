using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
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
using System.Windows.Media.Media3D;
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
/**/
        Body[] bodies = new Body[6];
/**/

        // Acquires HD face data.
        private HighDefinitionFaceFrameSource _faceSource = null;

        // Reads HD face data.
        private HighDefinitionFaceFrameReader _faceReader = null;
/**/
        // Reads multisurce frame data.
        private MultiSourceFrameReader _multiReader = null;
/**/
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

/**/
                // Listen for multisurce data.
                _multiReader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color);
                _multiReader.MultiSourceFrameArrived += MultiReader_MultiSourceFrameArrived;
/**/
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

            //debugin.Text = String.Format("IN OK");
            /**/
            using (BodyFrame bf = e.FrameReference.AcquireFrame())
            {
                if (bf != null)
                {
                    bf.GetAndRefreshBodyData(bodies);
                    foreach (Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            Joint headJoint = body.Joints[JointType.Head];
                            if (headJoint.TrackingState == TrackingState.Tracked)
                            {
                                Single X = headJoint.Position.X;
                                Single Y = headJoint.Position.Y;
                                Single Z = headJoint.Position.Z;

                                string x = X.ToString("0.0000");
                                string y = Y.ToString("0.0000");
                                string z = Z.ToString("0.0000");

                                consoleHeadJoint.Text = String.Format("   HEAD: \n X: {0} \n Y: {1} \n Z: {2} \n", x, y, z);

                                //introducing time association
                                DateTime NowH = DateTime.Now;
                                consoleNowH.Text = String.Format("    Head date and time: {0: MM/dd/yyy HH:mm:ss.fff}", NowH);
                                /*
                                //creating a string with head data associated with time
                                string NowHString = NowH.ToString("MM/dd/yyy HH:mm:ss.fff");
                                string HeadPositionTimeol = NowHString + "\t\t" + "HX= " + x + "\t\t" + "HY= " + y + "\t\t" + "HZ= " + z + "\n";
                                //writing into .txt file
                                string TxtPath = "C:\\Users\\simon\\Desktop\\";
                                string TxtName = "HeadPositionTime.txt";
                                using (StreamWriter outputFile = new StreamWriter(TxtPath + @"\" + TxtName, true))
                                {
                                    outputFile.WriteLine(HeadPositionTimeol);
                                }
                                */
                                //debugout.Text = String.Format("out OK");
                            }
                        }
                    }
                }
            }
            /**/

            // forse questo codice poteva essere inserito nel blocco precedente, inoltre c'è una ridondanza di "bodies"
            using (var frame = e.FrameReference.AcquireFrame())
            {
                //debugin.Text = String.Format("IN OK");
                if (frame != null)
                {
                    Body[] bodies = new Body[frame.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);
                    //inside the "bodies" array, search those which are tracked and, amongst them, take the first or default one
                    Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                    //as the HDface stream can use only one face, at that face is given the same id of the first or default body
                    if (! _faceSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                        }
                    }
                }
                //debugout.Text = String.Format("out OK");
            }
        }

        
        /**/

        void MultiReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs ecolor)
        {
            // Get a reference to the multi-frame
            var reference = ecolor.FrameReference.AcquireFrame();

            // Open color frame
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                   // camera.Source = ToBitmap(frame);
                }
            }
        }
        /**/
        /**/
        private ImageSource ToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);

        }
        /**/


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
            consoleNow.Text = String.Format("    Eyes date and time: {0: MM/dd/yyy HH:mm:ss.fff}", Now);
            string NowString = Now.ToString("MM/dd/yyy HH:mm:ss.fff");


            //NoseTip position
            var NoseTip = vertices[(int)HighDetailFacePoints.NoseTip]; // sintassi non chiara
            //convert to single
            Single NoseTipX = NoseTip.X;
            Single NoseTipY = NoseTip.Y;
            Single NoseTipZ = NoseTip.Z;

            string NoseTipx = NoseTipX.ToString("0.0000");
            string NoseTipy = NoseTipY.ToString("0.0000");
            string NoseTipz = NoseTipZ.ToString("0.0000");

            consoleNoseTip.Text = String.Format(" NOSE TIP: \n X: {0} \n Y: {1} \n Z: {2} \n", NoseTipx, NoseTipy, NoseTipz);


            //EyeLeft position

            //Catch mid eye top position
            var LefteyeMidtop = vertices[(int)HighDetailFacePoints.LefteyeMidtop];
            //convert to single
            Single LefteyeMidtopX = LefteyeMidtop.X;
            Single LefteyeMidtopY = LefteyeMidtop.Y;
            Single LefteyeMidtopZ = LefteyeMidtop.Z;

            //Catch mid eye bottom position
            var LefteyeMidbottom = vertices[(int)HighDetailFacePoints.LefteyeMidbottom];
            //convert to single
            Single LefteyeMidbottomX = LefteyeMidbottom.X;
            Single LefteyeMidbottomY = LefteyeMidbottom.Y;
            Single LefteyeMidbottomZ = LefteyeMidbottom.Z;

            //Calculate eye position as a mean between top and bottom eye mid
            Single LefteyeX = (LefteyeMidbottomX + LefteyeMidtopX) / 2;
            Single LefteyeY = (LefteyeMidbottomY + LefteyeMidtopY) / 2;
            Single LefteyeZ = (LefteyeMidbottomZ + LefteyeMidtopZ) / 2;

            string Lefteyex = LefteyeX.ToString("0.0000");
            string Lefteyey = LefteyeY.ToString("0.0000");
            string Lefteyez = LefteyeZ.ToString("0.0000");

            consoleEyeLeft.Text = String.Format("  LEFT EYE: \n X: {0} \n Y: {1} \n Z: {2} \n", Lefteyex, Lefteyey, Lefteyez);



            //EyeRight position

            //Catch eye mid top position
            var RighteyeMidtop = vertices[(int)HighDetailFacePoints.RighteyeMidtop];
            //convert to single
            Single RighteyeMidtopX = RighteyeMidtop.X;
            Single RighteyeMidtopY = RighteyeMidtop.Y;
            Single RighteyeMidtopZ = RighteyeMidtop.Z;

            //Catch eye mid bottom position
            var RighteyeMidbottom = vertices[(int)HighDetailFacePoints.RighteyeMidbottom];
            //convert to single
            Single RighteyeMidbottomX = RighteyeMidbottom.X;
            Single RighteyeMidbottomY = RighteyeMidbottom.Y;
            Single RighteyeMidbottomZ = RighteyeMidbottom.Z;

            //Calculate eye position as a mean between top and bottom eye mid
            Single RighteyeX = (RighteyeMidbottomX + RighteyeMidtopX) / 2;
            Single RighteyeY = (RighteyeMidbottomY + RighteyeMidtopY) / 2;
            Single RighteyeZ = (RighteyeMidbottomZ + RighteyeMidtopZ) / 2;

            string Righteyex = RighteyeX.ToString("0.0000");
            string Righteyey = RighteyeY.ToString("0.0000");
            string Righteyez = RighteyeZ.ToString("0.0000");

            consoleEyeRight.Text = String.Format(" RIGHT EYE: \n X: {0} \n Y: {1} \n Z: {2} \n", Righteyex, Righteyey, Righteyez);


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

            consoleEyedistancer.Text = String.Format(" INTEROCULAR \n   DISTANCE: \n   D: {0}", Eyedistancer);

            /*
            //create a .txt file with captured data coupled with time reference,
            //due to be able to plot them on a chart

            //creating a string with eyeposition data associated with time
            string EyePositionTimeol = NowString + "\t\t" + "ED= " + Eyedistancer + "\t\t" + "RX= " + Righteyex + "\t\t" + "RY= " + Righteyey + "\t\t" + "RZ= " + Righteyez + "\t\t" + "LX= " + Lefteyex + "\t\t" + "LY= " + Lefteyey + "\t\t" + "LZ= " + Lefteyez + "\n";
            //writing into .txt file
            string TxtPath = "C:\\Users\\simon\\Desktop\\";
            string TxtName = "EyePositionTime.txt";
            using (StreamWriter outputFile = new StreamWriter(TxtPath + @"\" + TxtName, true))
            {
                outputFile.WriteLine(EyePositionTimeol);
            }
            */

            
            //send to socket
            string message = Lefteyex + "\n" + Lefteyey + "\n" + Lefteyez + "\n" + Righteyex + "\n" + Righteyey + "\n" + Righteyez + "\n" + NoseTipx + "\n" + NoseTipy + "\n" + NoseTipz;

            byte[] stream = Encoding.UTF8.GetBytes(message);
            socket.SendTo(stream, ep);
            


            //drawing vertices to points

            
            if (vertices.Count > 0)
            {
                if (_points.Count == 0)
                {
                    for (int index = 0; index < vertices.Count; index++)
                    {
                        if (index == 1090 || index == 241 || index == 1104 || index == 731)
                        {
                            Ellipse ellipse = new Ellipse
                            {
                                Width = 4.0,
                                Height = 4.0,
                                Fill = new SolidColorBrush(Colors.Yellow)
                            };
                            _points.Add(ellipse);
                        }

                        else
                        {
                          Ellipse ellipse = new Ellipse
                          {
                               Width = 1.0,
                             Height = 1.0,
                             Fill = new SolidColorBrush(Colors.Blue)
                           };
                            _points.Add(ellipse);
                        }
                       
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

                        if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return; // potenzialmente inutile

                        Ellipse ellipse = _points[index];

                        Canvas.SetLeft(ellipse, point.X);
                        Canvas.SetTop(ellipse, point.Y);
                    
                }
            }

            //draw eyes on mapXY

            // se gli oggetti eyeR e eyeL fossero stati definiti una volta per tutte e seccessivamente solo spostati non ci sarebbe bisogno del Clear()
            mapXY.Children.Clear();
            Ellipse eyeRxy = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Green };
            Ellipse eyeLxy = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Red };
            
            Canvas.SetTop(eyeRxy, 145 - 50 * RighteyeY);
            Canvas.SetLeft(eyeRxy, 180 + 50 * RighteyeX);
            Canvas.SetTop(eyeLxy, 145 - 50 * LefteyeY);
            Canvas.SetLeft(eyeLxy, 180 + 50 * LefteyeX);

            //mapXY.Children.Remove(eyeR);
            TranslateTransform trEyeRxy = new TranslateTransform(50 * RighteyeX, -50 * RighteyeY);
            TranslateTransform trEyeLxy = new TranslateTransform(50 * LefteyeX, -50 * LefteyeY);
            eyeRxy.RenderTransform = trEyeRxy;
            eyeLxy.RenderTransform = trEyeLxy;
            mapXY.Children.Add(eyeRxy);
            mapXY.Children.Add(eyeLxy);



            
            //draw eyes on mapXZ
            mapXZ.Children.Clear();
            Ellipse eyeR = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Green };
            Ellipse eyeL = new Ellipse() { Width = 5, Height = 5, Fill = Brushes.Red };

            Canvas.SetTop(eyeR, 50 * RighteyeZ);
            Canvas.SetLeft(eyeR, 180 + 50 * RighteyeX);
            Canvas.SetTop(eyeL, 50 * LefteyeZ);
            Canvas.SetLeft(eyeL, 180 + 50 * LefteyeX);

            //mapXZ.Children.Remove(eyeR);
            TranslateTransform trEyeR = new TranslateTransform(50 * RighteyeX, 50 * RighteyeZ);
            TranslateTransform trEyeL = new TranslateTransform(50 * LefteyeX, 50 * LefteyeZ);
            eyeR.RenderTransform = trEyeR;
            eyeL.RenderTransform = trEyeL;
            mapXZ.Children.Add(eyeR);
            mapXZ.Children.Add(eyeL);
            
        }
    }
}
