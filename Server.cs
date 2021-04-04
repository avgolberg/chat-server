using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Chat_Server
{
    public partial class Server : Form
    {
        ClientContext db;
        static ServerObject server;
        public Server()
        {
            InitializeComponent();
            try
            {
                db = new ClientContext();
                server = new ServerObject(db);

                Thread listen = new Thread(server.Listen);
                listen.IsBackground = true;
                listen.Start();
                textBox1.Text += "Сервер запущен: ожидание подключений...";
            }
            catch (Exception ex)
            {
                if(server!=null)
                server.Disconnect();
                Console.WriteLine(ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            server.Disconnect();
        }
    }
}
