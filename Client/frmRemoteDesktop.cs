using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class frmRemoteDesktop : Form
    {
        Socket server = null;
        byte[] data = new byte[9999999];
        String ipSender = "";
        public frmRemoteDesktop(String ip)
        {
            CheckForIllegalCrossThreadCalls = false;
            ipSender = ip;
            InitializeComponent();
        }

        private void frmRemoteDesktop_Load(object sender, EventArgs e)
        {
            String hostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostByName(hostName);
            SendIP(ipEntry.AddressList[0].ToString());
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Parse(ipEntry.AddressList[0].ToString()), 9099));
            server.Listen(1);
            server.BeginAccept(new AsyncCallback(Connect), null);
        }

        private void Connect(IAsyncResult ar)
        {
            Socket socket = server.EndAccept(ar);
            socket.BeginReceive(data, 0, data.Length, SocketFlags.None, new AsyncCallback(ReceiveData), socket);
            server.BeginAccept(new AsyncCallback(Connect), null);
        }

        private void ReceiveData(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            int dataLengthReceive = socket.EndReceive(ar);
            byte[] dataReceive = new byte[dataLengthReceive];
            Array.Copy(data, dataReceive, dataReceive.Length);


            if (dataLengthReceive < 50)
            {
                String receive = Encoding.ASCII.GetString(dataReceive);
                int width = Int32.Parse(receive.Substring(0, receive.IndexOf(':')));
                int height = Int32.Parse(receive.Substring(receive.IndexOf(':') + 1, receive.IndexOf('|') - receive.IndexOf(':') - 1));
                this.Size = new Size(width + 16, height + 38);
            }
            else
            {
                MemoryStream ms = new MemoryStream(dataReceive);
                Image image = Bitmap.FromStream(ms);
                pbImage.BackgroundImage = image;
                //ms.Position = 0;
            }
        }

        private void SendIP(String ip)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Parse(ipSender), 3005);
            byte[] byteSend = Encoding.ASCII.GetBytes(ip + "|Accept");
            socket.Send(byteSend);
            socket.Close();
        }

        private void pbImage_MouseDown(object sender, MouseEventArgs e)
        {
            byte[] byteSend = null;
            if (e.Button == MouseButtons.Left)
                byteSend = Encoding.ASCII.GetBytes("Left:MouseDown");
            else if (e.Button == MouseButtons.Right)
            {
                byteSend = Encoding.ASCII.GetBytes("Right:MouseDown");
            }
            else if (e.Button == MouseButtons.Middle)
                byteSend = Encoding.ASCII.GetBytes("Middle:MouseDown");
            else
                return;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Parse(ipSender), 3005);
            socket.Send(byteSend);
            socket.Close();
        }

        private void pbImage_MouseMove(object sender, MouseEventArgs e)
        {
            int x = e.X;
            int y = e.Y;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Parse(ipSender), 3005);
            byte[] byteSend = Encoding.ASCII.GetBytes(x.ToString() + ":" + y.ToString() + "|MouseMove");
            socket.Send(byteSend);
            socket.Close();
        }

        private void pbImage_MouseUp(object sender, MouseEventArgs e)
        {
            byte[] byteSend = null;
            if (e.Button == MouseButtons.Left)
                byteSend = Encoding.ASCII.GetBytes("Left:MouseUp");
            else if (e.Button == MouseButtons.Right)
            {
                byteSend = Encoding.ASCII.GetBytes("Right:MouseUp");
            }
            else if (e.Button == MouseButtons.Middle)
                byteSend = Encoding.ASCII.GetBytes("Middle:MouseUp");
            else return;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Parse(ipSender), 3005);
            socket.Send(byteSend);
            socket.Close();
        }

        private void frmRemoteDesktop_KeyUp(object sender, KeyEventArgs e)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Parse(ipSender), 3005);
            byte[] byteSend = Encoding.ASCII.GetBytes(e.KeyValue.ToString());
            socket.Send(byteSend);
            socket.Close();
        }
    }
}
