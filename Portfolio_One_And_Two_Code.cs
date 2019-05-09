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
using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using System.Threading;
using System.IO;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        public int choiceNumber;

        //declare variables
        private KinectSensor sensor;
        private const int SKELETON_COUNT = 6;
        private Skeleton[] allSKELETONS = new Skeleton[SKELETON_COUNT];

        //Multiple SKELETONS
        static int SKELETONS_TRACKED = 2;
        Person[] People = new Person[SKELETONS_TRACKED];






        //enabing and disabling video
        private Boolean SHOW_VIDEO = true;


        private void frmKinectInterface_Loading(object sender, RoutedEventArgs e)
        {

            //Required for kinect
            this.initiate_kinect();

        }

        private void frmKinectInterface_Colsing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            //dispose of sensors
            if (sensor != null)
                sensor.Stop();
        }


        private void initiate_kinect()
        {
            try
            {
                //checking for multiple kinect sensors and accessing first one
                if (KinectSensor.KinectSensors.Count > 0)
                {
                    sensor = KinectSensor.KinectSensors[0];
                }
                //check status of kinect
                if (sensor.Status == KinectStatus.Connected)
                {
                    sensor.ColorStream.Enable();
                    sensor.DepthStream.Enable();
                    sensor.SkeletonStream.Enable();
                    //Load People
                    initalizePeople();
                    //start skeleton tracking
                    sensor.AllFramesReady += new
                   EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
                    sensor.Start();
                    //Load Speech
                    initializeSpeech();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace); // If caught create an output of the StackTrace
            }


        }


        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            //retrives source from kinect sensor
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }
                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                int stride = colorFrame.Width * 4;

                //displays video
                if (SHOW_VIDEO)
                {
                    this.imgVideo.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
                }
            }

            //tracks multiple people in loop
            for (int i = 0; i < SKELETONS_TRACKED; i++)
            {

                Skeleton me = null;

                GetSKELETONS(e, ref me, i);

                if (me == null)
                {
                    return;
                }

                //sets values to people array then draws the person
                GetXYD(me, People[i]);

                People[i].DrawPerson();

                //things to check for i.e. gestures grid ...
                this.checkPersonFor(People[i]);
            }


        }


        private void GetSKELETONS(AllFramesReadyEventArgs e, ref Skeleton me, int person)
        {
            //retrieves skelital data from kinect
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return;
                }

                skeletonFrameData.CopySkeletonDataTo(allSKELETONS);
                //query to retrive list from skeleton frame data
                List<Skeleton> tmpSkel = (from s in allSKELETONS where s.TrackingState == SkeletonTrackingState.Tracked select s).Distinct().ToList();
                if (tmpSkel.Count < person + 1)
                {
                    return;
                }



                me = tmpSkel[person];
            }
        }






        private void initalizePeople()
        {

            {
                for (int i = 0; i < SKELETONS_TRACKED; i++)
                {
                    //adds joints to display
                    People[i] = new Person(i);
                    List<Ellipse> Joints = People[i].getJoints();
                    foreach (Ellipse Joint in Joints)
                    {
                        canDraw.Children.Add(Joint);
                    }
                    //adds limbs to display
                    List<Line> Bones = People[i].getBones();
                    foreach (Line Bone in Bones)
                    {
                        canDraw.Children.Add(Bone);
                    }
                    // add additional content to diaply
                    // this is the rectangles for grid selection
                    List<Rectangle> Additional = People[i].getAdditional();
                    foreach (Rectangle Add in Additional)
                    {
                        grdOverlay.Children.Add(Add);
                    }
                }
                this.buildGrid();
            }

        }

        private void GetSkeleton(AllFramesReadyEventArgs e, ref Skeleton me)
        {
            SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame();
            skeletonFrameData.CopySkeletonDataTo(allSKELETONS);
            me = (from s in allSKELETONS
                  where s.TrackingState == SkeletonTrackingState.Tracked
                  select s).FirstOrDefault();

        }
        private void GetXYD(Skeleton me, Person person)
        {
            //head
            DepthImagePoint headDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint
            (me.Joints[JointType.Head].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint headColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint
            (me.Joints[JointType.Head].Position, ColorImageFormat.RgbResolution640x480Fps30);
            person.jHead.X = headColorPoint.X;
            person.jHead.Y = headColorPoint.Y;
            person.jHead.D = headDepthPoint.Depth;



            //right hand
            DepthImagePoint righthandDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint righthandColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.HandRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jHandRight.X = righthandColorPoint.X;
            person.jHandRight.Y = righthandColorPoint.Y;
            person.jHandRight.D = righthandDepthPoint.Depth;

            //leftelbow
            DepthImagePoint rightelbowDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.ElbowRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint rightelbowColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.ElbowRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jElbowRight.X = rightelbowColorPoint.X;
            person.jElbowRight.Y = rightelbowColorPoint.Y;
            person.jElbowRight.D = rightelbowDepthPoint.Depth;

            //lefthand
            DepthImagePoint lefthandDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.HandLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint lefthandColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.HandLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jHandLeft.X = lefthandColorPoint.X;
            person.jHandLeft.Y = lefthandColorPoint.Y;
            person.jHandLeft.D = lefthandDepthPoint.Depth;

            //leftelbow
            DepthImagePoint leftelbowDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.ElbowLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint leftelbowColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.ElbowLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jElbowLeft.X = leftelbowColorPoint.X;
            person.jElbowLeft.Y = leftelbowColorPoint.Y;
            person.jElbowLeft.D = leftelbowDepthPoint.Depth;

            //Body
            DepthImagePoint bodyDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.Spine].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint bodyColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.Spine].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jSpine.X = bodyColorPoint.X;
            person.jSpine.Y = bodyColorPoint.Y;
            person.jSpine.D = bodyDepthPoint.Depth;

            //Hip Left
            DepthImagePoint hipLeftDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.HipLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint hipLeftColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.HipLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jHipLeft.X = hipLeftColorPoint.X;
            person.jHipLeft.Y = hipLeftColorPoint.Y;
            person.jHipLeft.D = hipLeftDepthPoint.Depth;

            //hip right
            DepthImagePoint hipRightDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.HipRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint hipRightColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.HipRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jHipRight.X = hipRightColorPoint.X;
            person.jHipRight.Y = hipRightColorPoint.Y;
            person.jHipRight.D = hipRightDepthPoint.Depth;
            //foot left
            DepthImagePoint FootLeftDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.FootLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint FootLeftColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.FootLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jFootLeft.X = FootLeftColorPoint.X;
            person.jFootLeft.Y = FootLeftColorPoint.Y;
            person.jFootLeft.D = FootLeftDepthPoint.Depth;
            //foot right
            DepthImagePoint FootRightDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.FootRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint FootRightColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.FootRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jFootRight.X = FootRightColorPoint.X;
            person.jFootRight.Y = FootRightColorPoint.Y;
            person.jFootRight.D = FootRightDepthPoint.Depth;
            //hip center
            DepthImagePoint HipCenterDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.HipCenter].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint HipCenterColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.HipCenter].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jHipCenter.X = HipCenterColorPoint.X;
            person.jHipCenter.Y = HipCenterColorPoint.Y;
            person.jHipCenter.D = HipCenterDepthPoint.Depth;
            //KneeRight
            DepthImagePoint KneeRightDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.KneeRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint KneeRightColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.KneeRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jKneeRight.X = KneeRightColorPoint.X;
            person.jKneeRight.Y = KneeRightColorPoint.Y;
            person.jKneeRight.D = KneeRightDepthPoint.Depth;
            //KneeLeft
            DepthImagePoint KneeLeftDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.KneeLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint KneeLeftColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.KneeLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jKneeLeft.X = KneeLeftColorPoint.X;
            person.jKneeLeft.Y = KneeLeftColorPoint.Y;
            person.jKneeLeft.D = KneeLeftDepthPoint.Depth;
            //ShoulderCenter
            DepthImagePoint ShoulderCenterDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.ShoulderCenter].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint ShoulderCenterColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.ShoulderCenter].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jShoulderCenter.X = ShoulderCenterColorPoint.X;
            person.jShoulderCenter.Y = ShoulderCenterColorPoint.Y;
            person.jShoulderCenter.D = ShoulderCenterDepthPoint.Depth;
            //ShoulderLeft
            DepthImagePoint ShoulderLeftDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.ShoulderLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint ShoulderLeftColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.ShoulderLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jShoulderLeft.X = ShoulderLeftColorPoint.X;
            person.jShoulderLeft.Y = ShoulderLeftColorPoint.Y;
            person.jShoulderLeft.D = ShoulderLeftDepthPoint.Depth;
            //ShoulderRight
            DepthImagePoint ShoulderRightDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.ShoulderRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint ShoulderRightColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.ShoulderRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jShoulderRight.X = ShoulderRightColorPoint.X;
            person.jShoulderRight.Y = ShoulderRightColorPoint.Y;
            person.jShoulderRight.D = ShoulderRightDepthPoint.Depth;
            //WristLeft
            DepthImagePoint WristLeftDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.WristLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint WristLeftColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.WristLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jWristLeft.X = WristLeftColorPoint.X;
            person.jWristLeft.Y = WristLeftColorPoint.Y;
            person.jWristLeft.D = WristLeftDepthPoint.Depth;
            //WristRight
            DepthImagePoint WristRightDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.WristRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint WristRightColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.WristRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jWristRight.X = WristRightColorPoint.X;
            person.jWristRight.Y = WristRightColorPoint.Y;
            person.jWristRight.D = WristRightDepthPoint.Depth;
            //AnkleLeft
            DepthImagePoint AnkleLeftDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.AnkleLeft].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint AnkleLeftColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.AnkleLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jAnkleLeft.X = AnkleLeftColorPoint.X;
            person.jAnkleLeft.Y = AnkleLeftColorPoint.Y;
            person.jAnkleLeft.D = AnkleLeftDepthPoint.Depth;
            //AnkleRight
            DepthImagePoint AnkleRightDepthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(me.Joints[JointType.AnkleRight].Position, DepthImageFormat.Resolution640x480Fps30);
            ColorImagePoint AnkleRightColorPoint = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(me.Joints[JointType.AnkleRight].Position, ColorImageFormat.RgbResolution640x480Fps30);

            person.jAnkleRight.X = AnkleRightColorPoint.X;
            person.jAnkleRight.Y = AnkleRightColorPoint.Y;
            person.jAnkleRight.D = AnkleRightDepthPoint.Depth;

        }
        private Thread audioThread;
        private SpeechRecognitionEngine sre;
        private void initializeSpeech()
        {
            RecognizerInfo ri = GetKinectRecognizer();
            Console.WriteLine("ID " + ri.Id + " Name " + ri.Name);
            sre = new SpeechRecognitionEngine(ri.Id);
            //get commands list
            var commands = getChoices();
            //culture support i.e. fr for french
            var gb = new GrammarBuilder();
            gb.Append(commands);
            //load culture into grammer
            var g = new Grammar(gb);
            //load grammer into engine
            sre.LoadGrammar(g);
            //load in event handler for commands
            sre.SpeechRecognized += new
            EventHandler<SpeechRecognizedEventArgs>(Kinect_SpeechRecognized);
            //initiate listening
            audioThread = new Thread(startAudioListening);
            audioThread.Start();
            
        }

        private RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name,
                StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        private void startAudioListening()
        {
            var audioSource = sensor.AudioSource;
            audioSource.AutomaticGainControlEnabled = false;
            Stream aStream = audioSource.Start();
            sre.SetInputToAudioStream(aStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm,
            16000, 16, 1, 32000, 2, null));
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }
        public Choices getChoices()
        {
            var choices = new Choices();

            choices.Add("computer");//Listening for computer
            choices.Add("one");//Listening for one
            choices.Add("two");//Listening for two
            choices.Add("three");//Listening for three
            choices.Add("four");//Listening for four
            choices.Add("five");//Listening for five
            choices.Add("six");//Listening for six
            choices.Add("seven");//Listening     for seven
            choices.Add("eight");//Listening for eight
            choices.Add("nine");//Listening for nine
            choices.Add("ten");//Listening for ten
            choices.Add("eleven");//Listening for eleven
            choices.Add("twelve");//Listening for twelve
            choices.Add("thirteen");//Listening for thirteen
            choices.Add("fourteen");//Listening for fourteen
            choices.Add("fifthteen");//Listening for fifthteen
            choices.Add("sixteen");//Listening for sixteen

            return choices;

        }

        public void Kinect_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Text.ToLower() == "computer" && e.Result.Confidence >= 0.25)
            {
                //YOU SAID COMPUTER AND WROTE HELLO
                txtVoiceCommand.Text = "You Said Computer.";
            }

            if (e.Result.Text.ToLower() == "one" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID ONE AND SELECTED GRID ONE

                txtVoiceCommand.Text = "One"; // Show  on the interface what command has just been outputted

                People[0].selectedColumn = People[0].selectedColumn = 1; // Set what Column you are selecting
                People[0].selectedRow = People[0].selectedRow = 1; // Set what row you are selecting

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 1" + "    col: 1"; // Show on the interface what tile you have chosen
            }

            if (e.Result.Text.ToLower() == "two" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID TWO AND SELECTED GRID TWO
                txtVoiceCommand.Text = "Two";

                People[0].selectedColumn = People[0].selectedColumn = 2;
                People[0].selectedRow = People[0].selectedRow = 1;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 1" + "    col: 2";


            }

            if (e.Result.Text.ToLower() == "three" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID THREE AND SELECTED GRID THREE
                txtVoiceCommand.Text = "Three";

                People[0].selectedColumn = People[0].selectedColumn = 3;
                People[0].selectedRow = People[0].selectedRow = 1;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 1" + "    col: 3";
            }

            if (e.Result.Text.ToLower() == "four" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID FOUR AND SELECTED GRID FOUR
                txtVoiceCommand.Text = "Four";

                People[0].selectedColumn = People[0].selectedColumn = 4;
                People[0].selectedRow = People[0].selectedRow = 1;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 1" + "    col: 4";
            }

            if (e.Result.Text.ToLower() == "five" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID FIVE AND SELECTED GRID FIVE
                txtVoiceCommand.Text = "Five";

                People[0].selectedColumn = People[0].selectedColumn = 1;
                People[0].selectedRow = People[0].selectedRow = 2;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 2" + "    col: 1";
            }

            if (e.Result.Text.ToLower() == "six" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID SIX AND SELECTED GRID SIX
                txtVoiceCommand.Text = "Six";

                People[0].selectedColumn = People[0].selectedColumn = 2;
                People[0].selectedRow = People[0].selectedRow = 2;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 2" + "    col: 2";
            }

            if (e.Result.Text.ToLower() == "seven" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID SEVEN AND SELECTED GRID SEVEN
                txtVoiceCommand.Text = "Seven";

                People[0].selectedColumn = People[0].selectedColumn = 3;
                People[0].selectedRow = People[0].selectedRow = 2;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 1" + "    col: 3";
            }

            if (e.Result.Text.ToLower() == "eight" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID EIGHT AND SELECTED GRID EIGHT
                txtVoiceCommand.Text = "Eight";

                People[0].selectedColumn = People[0].selectedColumn = 4;
                People[0].selectedRow = People[0].selectedRow = 2;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 2" + "    col: 4";
            }

            if (e.Result.Text.ToLower() == "nine" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID NINE AND SELECTED GRID NINE
                txtVoiceCommand.Text = "Nine";

                People[0].selectedColumn = People[0].selectedColumn = 1;
                People[0].selectedRow = People[0].selectedRow = 3;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 3" + "    col: 1";
            }

            if (e.Result.Text.ToLower() == "ten" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID TEN AND SELECTED GRID TEN
                txtVoiceCommand.Text = "Ten";

                People[0].selectedColumn = People[0].selectedColumn = 2;
                People[0].selectedRow = People[0].selectedRow = 3;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 3" + "    col: 2";

            }

            if (e.Result.Text.ToLower() == "eleven" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID ELEVEN AND SELECTED GRID ELEVN
                txtVoiceCommand.Text = "Eleven";

                People[0].selectedColumn = People[0].selectedColumn = 3;
                People[0].selectedRow = People[0].selectedRow = 3;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 3" + "    col: 3";
            }

            if (e.Result.Text.ToLower() == "twelve" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID TWELVE AND SELECTED GRID TWELVE
                txtVoiceCommand.Text = "Twelve";

                People[0].selectedColumn = People[0].selectedColumn = 4;
                People[0].selectedRow = People[0].selectedRow = 3;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 3" + "    col: 4";
            }

            if (e.Result.Text.ToLower() == "thirteen" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID THIRTEEN AND SELECTED GRID THIRTEEN
                txtVoiceCommand.Text = "Thirteen";

                People[0].selectedColumn = People[0].selectedColumn = 1;
                People[0].selectedRow = People[0].selectedRow = 4;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 4" + "    col: 1";
            }

            if (e.Result.Text.ToLower() == "fourteen" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID FOURTEEN AND SELECTED GRID FOURTEEN
                txtVoiceCommand.Text = "Fourteen";

                People[0].selectedColumn = People[0].selectedColumn = 2;
                People[0].selectedRow = People[0].selectedRow = 4;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 4" + "    col: 2";
            }

            if (e.Result.Text.ToLower() == "fifthteen" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID FIFTHEEN AND SELECTED GRID FIFTHEEN
                txtVoiceCommand.Text = "Fiftheen";

                People[0].selectedColumn = People[0].selectedColumn = 3;
                People[0].selectedRow = People[0].selectedRow = 4;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 4" + "    col: 3";
            }

            if (e.Result.Text.ToLower() == "sixteen" && e.Result.Confidence >= 0.25f)
            {
                //YOU SAID SIXTEEN AND SELECTED GRID SIXTEEN
                txtVoiceCommand.Text = "Sixteen";

                People[0].selectedColumn = People[0].selectedColumn = 4;
                People[0].selectedRow = People[0].selectedRow = 4;

                People[0].recSelected.SetValue(Grid.RowProperty, People[0].selectedRow);
                People[0].recSelected.SetValue(Grid.ColumnProperty, People[0].selectedColumn);

                txtSelectedTile.Text = "SELECTED row: 4" + "    col: 4";
            }

        }



        //Grid Details -- Allows users to generate their own grid MATH MUST BE EQUAL TO SIZE OF CAMERA INPUT
        int ROW_COUNT = 4;
        int COLUMN_COUNT = 5;
        double BORDER_TOP = 10;
        double BORDER_LEFT = 5;
        double BORDER_BOTTOM = 10;
        double BORDER_RIGHT = 5;

        //grid generation
        private double BOX_WIDTH;
        private double BOX_HEIGHT;
        private const double HEIGHT = 480;
        private const double WIDTH = 640;

        //GRID FUNCTIONS
        //-------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------
        private void buildGrid()
        {
            //calculating grid
            BOX_WIDTH = ((WIDTH - (BORDER_LEFT + BORDER_RIGHT)) / COLUMN_COUNT);
            BOX_HEIGHT = ((HEIGHT - (BORDER_TOP + BORDER_BOTTOM)) / ROW_COUNT);

            //building GUI grid

            //border rows
            RowDefinition topBorderRow = new RowDefinition();
            topBorderRow.Height = new GridLength(BORDER_TOP);
            RowDefinition bottomBorderRow = new RowDefinition();
            bottomBorderRow.Height = new GridLength(BORDER_BOTTOM);

            //border cols
            ColumnDefinition rightBorderCol = new ColumnDefinition();
            rightBorderCol.Width = new GridLength(BORDER_RIGHT);
            ColumnDefinition leftBorderCol = new ColumnDefinition();
            leftBorderCol.Width = new GridLength(BORDER_LEFT);

            //adding rows to grid
            this.grdOverlay.RowDefinitions.Add(topBorderRow);
            for (int i = 0; i < ROW_COUNT; i++)
            {
                RowDefinition defaultRow = new RowDefinition();
                defaultRow.Height = new GridLength(BOX_HEIGHT);
                this.grdOverlay.RowDefinitions.Add(defaultRow);
            }
            this.grdOverlay.RowDefinitions.Add(bottomBorderRow);

            //adding cols to grid
            this.grdOverlay.ColumnDefinitions.Add(leftBorderCol);
            for (int i = 0; i < COLUMN_COUNT; i++)
            {
                ColumnDefinition defaultCol = new ColumnDefinition();
                defaultCol.Width = new GridLength(BOX_WIDTH);
                this.grdOverlay.ColumnDefinitions.Add(defaultCol);
            }
            this.grdOverlay.ColumnDefinitions.Add(rightBorderCol);




        }

        private void setSelectedTile(Person person)
        {
            // SETTING SELECTED GRID TILE
            person.recSelected.SetValue(Grid.RowProperty, person.selectedRow);
            person.recSelected.SetValue(Grid.ColumnProperty, person.selectedColumn);
            txtSelectedTile.Text = "SELECTED row:" + person.selectedRow + "    col:" + person.selectedColumn;
        }

        private void setHoveredTile(Person person)
        {
            //fills grid

        }

        private void gridCheck(Person person)
        {

            double HX = person.jHandRight.X;
            double HY = person.jHandRight.Y;
            //what row the hand is in
            for (int i = 1; i <= ROW_COUNT; i++)
            {
                if (HY <= ((BOX_HEIGHT * i) + BORDER_TOP) && HY > ((BOX_HEIGHT * (i - 1)) + BORDER_TOP))
                {
                    if (i != person.selectedRow)
                    {
                        person.selectedRow = i;
                        setHoveredTile(person);
                    }
                }
            }
            //what column is the hand in
            for (int i = 1; i <= COLUMN_COUNT; i++)
            {
                if (HX <= ((BOX_WIDTH * i) + BORDER_LEFT) && HX > ((BOX_WIDTH * (i - 1)) + BORDER_LEFT))
                {
                    if (i != person.selectedColumn)
                    {
                        person.selectedColumn = i;
                        setHoveredTile(person);
                    }
                }
            }
        }

        
        private void checkPersonFor(Person person)
        {
            //checking where your hand is on the grid
            gridCheck(person);
            //handles user pushing to select grid
            handlePush(person);
        }
        //HAND TRACKING
        //-------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------
        private void handlePush(Person person)
        {
            double difference;
            //trainking right or left hand
            if (person.TRACK_RIGHT_HAND)
            {
                difference = person.jSpine.D - person.jHandRight.D;
            }
            else
            {
                difference = person.jSpine.D - person.jHandLeft.D;
            }
            //comparisoon of spine and hand depth
            if (difference > person.PUSH_DIFFERANCE)
            {
                this.setSelectedTile(person);
            }
        }

        //KINECT MOVE MOTORS 
        //-------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------
        private void btnCameraUp_Click(object sender, RoutedEventArgs e)
        {
            this.moveMotor(1, 5);
        }

        private void btnCameraDown_Click(object sender, RoutedEventArgs e)
        {
            this.moveMotor(2, 5);
        }

        private void moveMotor(int direction, int amount)
        {
            switch (direction)
            {
                case 1:
                    {
                        //UP
                        if (sensor.ElevationAngle + amount < sensor.MaxElevationAngle)
                        {
                            sensor.ElevationAngle += amount;
                        }
                        break;
                    }
                case 2:
                    {
                        //DOWN
                        if (sensor.ElevationAngle - amount > sensor.MinElevationAngle)
                        {
                            sensor.ElevationAngle -= amount;
                        }
                        break;
                    }
            }
            this.sldCamera.Value = sensor.ElevationAngle;
        }


        //GUI EVENT HANDLERS
        //-------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------
        private void btnCameraMotors(object sender, RoutedEventArgs e)
        {
            sensor.ElevationAngle = Convert.ToInt32(this.sldCamera.Value);
        }







    }
}
