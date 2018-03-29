using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Server
{
    public partial class frmServer : Form
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);
        //user32->keybd_event
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        const int KEYEVENTF_EXTENDEDKEY = 0x1;
        const uint KEYEVENTF_KEYUP = 0x0002;

        Socket listener = null;
        byte[] data = new byte[200];
        delegate void SenderHandler();
        String ipReceiver = "";

        public frmServer()
        {
            InitializeComponent();
        }

        private void frmServer_Load(object sender, EventArgs e)
        {
            String hostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostByName(hostName);
            String ip = ipEntry.AddressList[0].ToString();
            lblIP.Text = ip;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Parse(ip), 3005));
            listener.Listen(1);
            listener.BeginAccept(new AsyncCallback(AcceptConnect), null);
        }

        private void AcceptConnect(IAsyncResult ar)
        {
            Socket socket = listener.EndAccept(ar);
            socket.BeginReceive(data, 0, data.Length, SocketFlags.None, new AsyncCallback(ReceiveData), socket);
            listener.BeginAccept(new AsyncCallback(AcceptConnect), null);
        }

        private void ReceiveData(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            int dataLengthReceive = socket.EndReceive(ar);
            byte[] dataReceive = new byte[dataLengthReceive];
            Array.Copy(data, dataReceive, dataReceive.Length);
            String strReceive = Encoding.ASCII.GetString(dataReceive);
            if (strReceive.Contains("Accept"))
            {
                ipReceiver = strReceive.Substring(0, strReceive.IndexOf('|'));
                SendResolutionScreen();
                SenderHandler sender = ImageSender;
                sender.BeginInvoke(new AsyncCallback(EndSend), null);
            }
            else
            {
                if (strReceive.Contains("MouseMove"))
                {
                    int x = Int32.Parse(strReceive.Substring(0, strReceive.IndexOf(':')));
                    int y = Int32.Parse(strReceive.Substring(strReceive.IndexOf(':') + 1, strReceive.IndexOf('|') - strReceive.IndexOf(':') - 1));
                    Cursor.Position = new Point(x, y);
                }
                else if (strReceive.Contains("MouseDown"))
                {
                    String mouse = strReceive.Substring(0, strReceive.IndexOf(':'));
                    if (mouse == "Left")
                        mouse_event((uint)MouseEventFlags.LEFTDOWN, 0, 0, 0, 0);
                    else if (mouse == "Right") mouse_event((uint)MouseEventFlags.RIGHTDOWN, 0, 0, 0, 0);
                    else mouse_event((uint)MouseEventFlags.MIDDLEDOWN, 0, 0, 0, 0);
                }
                else if (strReceive.Contains("MouseUp"))
                {
                    String mouse = strReceive.Substring(0, strReceive.IndexOf(':'));
                    if (mouse == "Left")
                        mouse_event((uint)MouseEventFlags.LEFTUP, 0, 0, 0, 0);
                    else if (mouse == "Right") mouse_event((uint)MouseEventFlags.RIGHTUP, 0, 0, 0, 0);
                    else mouse_event((uint)MouseEventFlags.MIDDLEUP, 0, 0, 0, 0);
                }
                else
                {
                    byte keyCode = byte.Parse(strReceive);
                    keybd_event(keyCode, 0x45, KEYEVENTF_EXTENDEDKEY, 0);
                }
            }
        }


        private void ImageSender()
        {
            try
            {
                while (true)
                {
                    Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sender.Connect(IPAddress.Parse(ipReceiver), 9099);
                    byte[] image = ImageScreenDesktop();
                    sender.Send(image, 0, image.Length, SocketFlags.None);
                    sender.Close();
                }
            }
            catch { }
        }

        /// <summary>
        /// Gửi độ phân giải màn hình
        /// </summary>
        /// <param name="resolutionScreen">Độ phân giải màn hình</param>
        public void SendResolutionScreen(String resolutionScreen)
        {
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(IPAddress.Parse(ipReceiver), 9099);
            byte[] resolution = Encoding.ASCII.GetBytes(resolutionScreen);
            sender.Send(resolution, 0, resolution.Length, SocketFlags.None);
            sender.Close();
        }

        public void SendResolutionScreen()
        {
            String resolutionScreen = Screen.PrimaryScreen.Bounds.Width.ToString() + ":" + Screen.PrimaryScreen.Bounds.Height.ToString() + "|";
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(IPAddress.Parse(ipReceiver), 9099);
            byte[] resolution = Encoding.ASCII.GetBytes(resolutionScreen);
            sender.Send(resolution, 0, resolution.Length, SocketFlags.None);
            sender.Close();
        }

        private byte[] ImageScreenDesktop()
        {
            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(bmp.Width, bmp.Height));
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.GetBuffer();
        }

        private void EndSend(IAsyncResult ar)
        {

        }
    }
}
