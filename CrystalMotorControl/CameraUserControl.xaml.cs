namespace CrystalMotorControl
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
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
    using Emgu.CV;
    using Emgu.CV.Structure;

    public partial class CameraUserControl : UserControl
    {
        #region Camera Capture Variables
        private Capture _capture = null; //Camera
        private bool _captureInProgress = false; //Variable to track camera state
        int CameraDevice = 0; //Variable to track camera device selected
        ///Video_Device[] WebCams; //List containing all the camera available
        #endregion


        public CameraUserControl()
        {
            InitializeComponent();

            String win1 = "Test Window (Press any key to close)"; //The name of the window
            CvInvoke.NamedWindow(win1); //Create the window using the specific name
            using (Mat frame = new Mat())
            using (VideoCapture capture = new VideoCapture())
                while (CvInvoke.WaitKey(1) == -1)
                {
                    capture.Read(frame);
                    CvInvoke.Imshow(win1, frame);
                }
        }

    }
}
