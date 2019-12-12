using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace TactorMatching
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Tactor Methods
        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll")]
        public static extern IntPtr GetVersionNumber();

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int Discover(int type);

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int Connect(string name, int type, IntPtr _callback);

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int InitializeTI();

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pulse(int deviceID, int tacNum, int msDuration, int delay);

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetDiscoveredDeviceName(int index);

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int DiscoverLimited(int type, int amount);

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
           CallingConvention = CallingConvention.Cdecl)]
        public static extern int ChangeGain(int deviceID, int tacNum, int gainval, int delay);

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseAll();

        [DllImport(@"C:\Users\minisim\Desktop\Tactors\TDKAPI_1.0.6.0\libraries\Windows\TactorInterface.dll",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int UpdateTI();
        #endregion

        #region Variables/Constants
        private const int SEAT_BELT_HIGH = 225;
        private const int SEAT_BELT_LOW = 75;

        private enum Type{
            Pan = 1,
            wear = 2,
            back = 3
        };

        private int[] seat_back_highs;
        private int[] seat_back_lows;
        private int[] seat_pan_highs;
        private int[] seat_pan_lows;
        private int[] seat_wear_highs;
        private int[] seat_wear_lows;
        private bool[] completed;
        private bool[][]finisdhedHighsAndLows;
        private Button[] startButtons;

        private int completedTasks;
        private int iterations;
        private int currentTask;
        private int LowOrHigh;
        private bool startedTask;
        private bool highOrLowClicked;
        private int minGain;
        private int maxGain;
        private int currentGain;

        private int back_high_avg;
        private int back_low_avg;
        private int pan_high_avg;
        private int pan_low_avg;
        private int wear_high_avg;
        private int wear_low_avg;

        private string data = "";
        #endregion

        public void testVibrations()
        {
            for (int x = 1; x <= 16; x++)
            {
                Pulse(0, x, 300, 0);
                Thread.Sleep(500);
            }
        }

        public void setupTactors()
        {
            if (InitializeTI() == 0)
            {
                System.Diagnostics.Debug.WriteLine("TDK Initialized");
            }

            int tactors = Discover(1);
            string name = Marshal.PtrToStringAnsi((IntPtr)GetDiscoveredDeviceName(0));

            if (Connect(name, 1, IntPtr.Zero) >= 0)
            {
                System.Diagnostics.Debug.WriteLine("Connected");
            }
        }

        public void InitializeStart()
        {
            seat_pan_highs = new int[3];
            seat_pan_lows = new int[3];
            seat_back_highs = new int[3];
            seat_back_lows = new int[3];
            seat_wear_highs = new int[3];
            seat_wear_lows = new int[3];
            completed = new bool[3];
            finisdhedHighsAndLows = new bool[3][];
            finisdhedHighsAndLows[0] = new bool[2];
            finisdhedHighsAndLows[1] = new bool[2];
            finisdhedHighsAndLows[2] = new bool[2];
            

            completedTasks = 0;
            startedTask = false;

            StartHighMatch.Visibility = Visibility.Hidden;
            StartLowMatch.Visibility = Visibility.Hidden;

            TestWearHigh.Visibility = Visibility.Hidden;
            TestWearLow.Visibility = Visibility.Hidden;
            TestBackHigh.Visibility = Visibility.Hidden;
            TestBackLow.Visibility = Visibility.Hidden;
            TestPanHigh.Visibility = Visibility.Hidden;
            TestPanLow.Visibility = Visibility.Hidden;

            iterations = 0;

            startButtons = new Button[]
            {
                StartPanMatch,
                StartWearMatch,
                StartBackMatch
            };
        }

        public void HideStartButtons()
        {
            StartBackMatch.Visibility = Visibility.Hidden;
            StartPanMatch.Visibility = Visibility.Hidden;
            StartWearMatch.Visibility = Visibility.Hidden;
        }

        public void ToggleLowAndHighBtns()
        {

            if (!finisdhedHighsAndLows[currentTask - 1][0])
            {
                StartLowMatch.Visibility = Visibility.Visible;
            }
            else
            {
                StartLowMatch.Visibility = Visibility.Hidden;
            }
            if (!finisdhedHighsAndLows[currentTask - 1][1])
            {
                StartHighMatch.Visibility = Visibility.Visible;
            }
            else
            {
                StartHighMatch.Visibility = Visibility.Hidden;
            }
        }

        public void SetTactorsGain()
        {
            switch (currentTask)
            {
                case (int)Type.Pan:
                    ChangeGain(0, 13, minGain, 0);
                    ChangeGain(0, 14, minGain, 0);
                    ChangeGain(0, 15, minGain, 0);
                    ChangeGain(0, 16, minGain, 0);
                    break;
                case (int)Type.wear:
                    ChangeGain(0, 7, minGain, 0);
                    ChangeGain(0, 8, minGain, 0);
                    break;
                case (int)Type.back:
                    ChangeGain(0, 9, minGain, 0);
                    ChangeGain(0, 10, minGain, 0);
                    ChangeGain(0, 11, minGain, 0);
                    ChangeGain(0, 12, minGain, 0);
                    break;
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            InitializeStart();
            setupTactors();

            
            
        }

        private void SeatBeltHigh_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 1, SEAT_BELT_HIGH, 0);
            ChangeGain(0, 2, SEAT_BELT_HIGH, 0);
            ChangeGain(0, 3, SEAT_BELT_HIGH, 0);
            Pulse(0, 1, 500, 0);
            Pulse(0, 2, 500, 0);
            Pulse(0, 3, 500, 0);
        }

        private void SeatBeltLow_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 1, SEAT_BELT_LOW, 0);
            ChangeGain(0, 2, SEAT_BELT_LOW, 0);
            ChangeGain(0, 3, SEAT_BELT_LOW, 0);
            Pulse(0, 1, 500, 0);
            Pulse(0, 2, 500, 0);
            Pulse(0, 3, 500, 0);
        }

        private void StartBackMatch_Click(object sender, RoutedEventArgs e)
        {
            InstructionLbl.Content = "Use the 'A' and 'D' keys to control the vibration" + Environment.NewLine
                         + "Press 'Start High' or 'Start Low' to begin";
            currentTask = (int)Type.back;
            HideStartButtons();
            ToggleLowAndHighBtns();
        }

        private void StartWearMatch_Click(object sender, RoutedEventArgs e)
        {
            InstructionLbl.Content = "Use the 'A' and 'D' keys to control the vibration" + Environment.NewLine
                         + "Press 'Start High' or 'Start Low' to begin";
            currentTask = (int)Type.wear;
            HideStartButtons();
            ToggleLowAndHighBtns();
        }

        private void StartPanMatch_Click(object sender, RoutedEventArgs e)
        {
            InstructionLbl.Content = "Use the 'A' and 'D' keys to control the vibration" + Environment.NewLine
                         + "Press 'Start High' or 'Start Low' to begin";
            currentTask = (int)Type.Pan;
            HideStartButtons();
            ToggleLowAndHighBtns();
        }


        private void StartHighMatch_Click(object sender, RoutedEventArgs e)
        {
            if (!startedTask)
            {
                startedTask = true;
                LowOrHigh = 1;
                currentGain = 150;
                minGain = 150;
                maxGain = 255;
                InstructionLbl.Content = "0/3 completed";
            }
        }

        private void StartLowMatch_Click(object sender, RoutedEventArgs e)
        {
            if (!startedTask)
            {
                startedTask = true;
                LowOrHigh = 0;
                currentGain = 45;
                minGain = 45;
                maxGain = 105;
                InstructionLbl.Content = "0/3 completed";
            }
           
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            switch (currentTask)
            {
                case (int)Type.Pan:
                    if(LowOrHigh == 1)
                    {
                        seat_pan_highs[iterations] = currentGain;
                    }
                    else
                    {
                        seat_pan_lows[iterations] = currentGain;
                    }
                    break;
                case (int)Type.wear:
                    if (LowOrHigh == 1)
                    {
                        seat_wear_highs[iterations] = currentGain;
                    }
                    else
                    {
                        seat_wear_lows[iterations] = currentGain;
                    }
                    break;
                case (int)Type.back:
                    if (LowOrHigh == 1)
                    {
                        seat_back_highs[iterations] = currentGain;
                    }
                    else
                    {
                        seat_back_lows[iterations] = currentGain;
                    }
                    break;
            }



            InstructionLbl.Content = iterations+1 + "/3 completed";
            iterations++;
            if (iterations == 3)
            {
                finisdhedHighsAndLows[currentTask-1][LowOrHigh] = true;
                startedTask = false;
                completed[currentTask - 1] = true;
                if (finisdhedHighsAndLows[currentTask - 1][0] && finisdhedHighsAndLows[currentTask - 1][1])
                {
                    for (int x = 0; x < completed.Length; x++)
                    {
                        if (!completed[x])
                        {
                            startButtons[x].Visibility = Visibility.Visible;
                        }
                    }
                }

                ToggleLowAndHighBtns();
                iterations = 0;
                completedTasks++;

                InstructionLbl.Content = "";
                if(completedTasks % 2 == 0)
                {
                    InstructionLbl.Content = "Completed task, please pick another one";
                }

                if (completedTasks == 6)
                {
                    InstructionLbl.Content = "Done! Here are the results";
                    int panh = 0, panl = 0, wearh = 0, wearl = 0, backh = 0, backl = 0;

                    for (int x = 0; x < 3; x++)
                    {
                        panh += seat_pan_highs[x];
                    }

                    for (int x = 0; x < 3; x++)
                    {
                        panl += seat_pan_lows[x];
                    }

                    for (int x = 0; x < 3; x++)
                    {
                        wearh += seat_wear_highs[x];
                    }

                    for (int x = 0; x < 3; x++)
                    {
                        wearl += seat_wear_lows[x];
                    }

                    for (int x = 0; x < 3; x++)
                    {
                        backh += seat_back_highs[x];
                    }

                    for (int x = 0; x < 3; x++)
                    {
                        backl += seat_back_lows[x];
                    }

                    pan_high_avg = panh / 3;
                    pan_low_avg = panl / 3;
                    wear_high_avg = wearh / 3;
                    wear_low_avg = wearl / 3;
                    back_high_avg = backh / 3;
                    back_low_avg = backl / 3;


                    /* make test buttons visible */
                    TestWearHigh.Visibility = Visibility.Visible;
                    TestWearLow.Visibility = Visibility.Visible;
                    TestBackHigh.Visibility = Visibility.Visible;
                    TestBackLow.Visibility = Visibility.Visible;
                    TestPanHigh.Visibility = Visibility.Visible;
                    TestPanLow.Visibility = Visibility.Visible;

                    string time = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
                    var fileName = @"C:\Users\minisim\source\repos\TactorMatching\Data\Data.txt";

                    data = "Time: " + time + Environment.NewLine
                         + "Wearable values" + Environment.NewLine
                         + "--------------------" + Environment.NewLine
                         + "High = { " + seat_wear_highs[0] + ", " + seat_wear_highs[1] + ", " + seat_wear_highs[2] + " }, Average = " + wear_high_avg + Environment.NewLine
                         + "Low = { " + seat_wear_lows[0] + ", " + seat_wear_lows[1] + ", " + seat_wear_lows[2] + " }, Average = " + wear_low_avg + Environment.NewLine + Environment.NewLine
                         + "Back values" + Environment.NewLine
                         + "--------------------" + Environment.NewLine
                         + "High = { " + seat_back_highs[0] + ", " + seat_back_highs[1] + ", " + seat_back_highs[2] + " }, Average = " + back_high_avg + Environment.NewLine
                         + "Low = { " + seat_back_lows[0] + ", " + seat_back_lows[1] + ", " + seat_back_lows[2] + " }, Average = " + back_low_avg + Environment.NewLine + Environment.NewLine
                         + "Pan values" + Environment.NewLine
                         + "--------------------" + Environment.NewLine
                         + "High = { " + seat_pan_highs[0] + ", " + seat_pan_highs[1] + ", " + seat_pan_highs[2] + " }, Average = " + pan_high_avg + Environment.NewLine
                         + "Low = { " + seat_pan_lows[0] + ", " + seat_pan_lows[1] + ", " + seat_pan_lows[2] + " }, Average = " + pan_low_avg + Environment.NewLine
                         + Environment.NewLine + "==============================================================================" +  Environment.NewLine;

                    System.IO.File.AppendAllText(fileName, data);




                }

            }

            currentGain = minGain;
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (startedTask)
            {
                if (e.Key == Key.A)
                {
   
                    currentGain -= 15;
                    if (currentGain <= minGain)
                    {
                        currentGain = minGain;
                    }
                }
                if (e.Key == Key.D)
                {
                    currentGain += 15;
                    if (currentGain >= maxGain)
                    {
                        currentGain = maxGain;
                    }
                }

                switch (currentTask)
                {
                    case (int)Type.Pan:
                        ChangeGain(0, 13, currentGain, 0);
                        ChangeGain(0, 14, currentGain, 0);
                        ChangeGain(0, 15, currentGain, 0);
                        ChangeGain(0, 16, currentGain, 0);
                        Pulse(0, 13, 500, 0);
                        Pulse(0, 14, 500, 0);
                        Pulse(0, 15, 500, 0);
                        Pulse(0, 16, 500, 0);
                        break;
                    case (int)Type.wear:
                        ChangeGain(0, 7, currentGain, 0);
                        ChangeGain(0, 8, currentGain, 0);
                        Pulse(0, 7, 500, 0);
                        Pulse(0, 8, 500, 0);
                        break;
                    case (int)Type.back:
                        ChangeGain(0, 9, currentGain, 0);
                        ChangeGain(0, 10, currentGain, 0);
                        ChangeGain(0, 11, currentGain, 0);
                        ChangeGain(0, 12, currentGain, 0);
                        Pulse(0, 9, 500, 0);
                        Pulse(0, 10, 500, 0);
                        Pulse(0, 11, 500, 0);
                        Pulse(0, 12, 500, 0);
                        break;
                }
            }
        }

        private void TestWearHigh_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 7, wear_high_avg, 0);
            ChangeGain(0, 7, wear_high_avg, 0);
            Pulse(0, 7, 500, 0);
            Pulse(0, 8, 500, 0);
        }

        private void TestWearLow_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 7, wear_low_avg, 0);
            ChangeGain(0, 7, wear_low_avg, 0);
            Pulse(0, 7, 500, 0);
            Pulse(0, 8, 500, 0);
        }

        private void TestBackHigh_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 9, back_high_avg, 0);
            ChangeGain(0, 10, back_high_avg, 0);
            ChangeGain(0, 11, back_high_avg, 0);
            ChangeGain(0, 12, back_high_avg, 0);
            Pulse(0, 9, 500, 0);
            Pulse(0, 10, 500, 0);
            Pulse(0, 11, 500, 0);
            Pulse(0, 12, 500, 0);
        }

        private void TestBackLow_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 9, back_low_avg, 0);
            ChangeGain(0, 10, back_low_avg, 0);
            ChangeGain(0, 11, back_low_avg, 0);
            ChangeGain(0, 12, back_low_avg, 0);
            Pulse(0, 9, 500, 0);
            Pulse(0, 10, 500, 0);
            Pulse(0, 11, 500, 0);
            Pulse(0, 12, 500, 0);
        }

        private void TestPanHigh_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 13, pan_high_avg, 0);
            ChangeGain(0, 14, pan_high_avg, 0);
            ChangeGain(0, 15, pan_high_avg, 0);
            ChangeGain(0, 16, pan_high_avg, 0);
            Pulse(0, 13, 500, 0);
            Pulse(0, 14, 500, 0);
            Pulse(0, 15, 500, 0);
            Pulse(0, 16, 500, 0);
        }

        private void TestPanLow_Click(object sender, RoutedEventArgs e)
        {
            ChangeGain(0, 13, pan_low_avg, 0);
            ChangeGain(0, 14, pan_low_avg, 0);
            ChangeGain(0, 15, pan_low_avg, 0);
            ChangeGain(0, 16, pan_low_avg, 0);
            Pulse(0, 13, 500, 0);
            Pulse(0, 14, 500, 0);
            Pulse(0, 15, 500, 0);
            Pulse(0, 16, 500, 0);
        }
    }
}
