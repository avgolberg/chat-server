using System;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_Server
{
    public class ClientObject
    {
        protected internal string Id { get; private set; }
        string userName;
        protected internal Socket client;
        ServerObject server; // объект сервера
        ClientContext db;
        static SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
     

        public ClientObject(Socket сlient, ServerObject serverObject, ClientContext db)
        {
            Id = Guid.NewGuid().ToString(); //уникальный идентификатор
            this.client = сlient;
            server = serverObject;
            serverObject.AddConnection(this);
            this.db = db;
        }

        public void Process()
        {
            try
            {
                string message = GetMessage();
                bool exist = false;
                string email = "";
                char[] msg = message.ToCharArray();
                int count = 0;
                foreach (char c in msg)
                {
                    if (c == '$') count++;
                }

                if (count == 1 || count == 2)
                {
                    if (count == 1)
                    {
                        string[] data = new string[2];
                        data = message.Split('$');
                        email = data[0];
                        string password = data[1];

                        foreach (Client client in db.Clients)
                        {
                            if (client.Email == email && client.Password == password)
                            {
                                exist = true;
                                break;
                            }
                        }
                    }
                    if (count == 2)
                    {
                        string[] data = new string[3];
                        data = message.Split('$');
                        email = data[0];
                        string password = data[1];
                        string name = data[2];
                        bool doubleEmail = false;

                        foreach (Client client in db.Clients)
                        {
                            if (client.Email == email)
                            {
                                doubleEmail = true;
                                break;
                            }
                        }
                        if (doubleEmail == true)
                        {
                            Close();
                        }
                        else
                        {
                            string code = Code();
                            string yourMail = "";
                            string applicationPassword = "";
                            smtp.Credentials = new NetworkCredential(yourMail, applicationPassword);
                            smtp.EnableSsl = true;

                            MailAddress from = new MailAddress(yourMail, "SMTP");
                            MailAddress to = new MailAddress(email);

                            MailMessage m = new MailMessage(from, to);

                            m.Subject = "Code";
                            m.Body = "Ваш код : " + code;
                            smtp.Send(m);

                            //ждем от клиента код
                            byte[] buffer = new byte[1024];
                            int length = client.Receive(buffer);
                            string clientCode = Encoding.Unicode.GetString(buffer, 0, length);

                            if (clientCode == code)
                            {
                                db.Clients.Add(new Client { Email = email, Password = password, Name = name });
                                db.SaveChanges();
                                exist = true;
                            }
                            else
                            {
                                exist = false;
                            }
                        }
                    }
                    if (exist == true)
                    {
                        byte[] data = Encoding.Unicode.GetBytes("1");
                        client.Send(data);

                        foreach (Client client in db.Clients)
                        {
                            if (client.Email == email)
                            {
                                userName = client.Name;
                                break;
                            }
                        }

                        message = DateTime.Now.ToLongTimeString() + " | " + userName + " в сети";
                        // посылаем сообщение о входе в чат всем подключенным пользователям
                        server.BroadcastMessage(message, this.Id);
                        Server.textBox1.Text += Environment.NewLine + message;
                        // в бесконечном цикле получаем сообщения от клиента

                        while (true)
                        {
                            try
                            {
                                 message = GetMessage();
                                message = DateTime.Now.ToLongTimeString() + " | " + userName + ": " + message;
                                Server.textBox1.Text += Environment.NewLine + message;
                                server.BroadcastMessage(message, this.Id);
                            }
                            catch
                            {
                                 message = DateTime.Now.ToLongTimeString() + " | " + userName + " вышел из сети";
                                Server.textBox1.Text += Environment.NewLine + message;
                                server.BroadcastMessage(message, this.Id);
                                break;
                            }
                        }
                    }

                    else
                    {
                        byte[] data = Encoding.Unicode.GetBytes("0");
                        client.Send(data);
                        Close();
                    }
                }
                else
                {
                    Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // в случае выхода из цикла закрываем ресурсы
                server.RemoveConnection(this.Id);
            }
        }
       
        static string Code()
        {
            Random random = new Random();
            string code = "";
            for (int i = 0; i < 10; i++)
            {
                code += random.Next(0, 10).ToString();
            }
            return code;

        }
        // чтение входящего сообщения и преобразование в строку
        private string GetMessage()
        {
            byte[] buffer = new byte[1024]; // буфер для получаемых данных
            int length = client.Receive(buffer);
            string clientMessage = Encoding.Unicode.GetString(buffer, 0, length);

            return clientMessage;
        }

        // закрытие подключения
        protected internal void Close()
        {
            if (client != null)
                client.Close();
        }
    }
}
