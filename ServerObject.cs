using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Server
{
    public class ServerObject
    {
        static Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP); // сервер для прослушивания
        IPAddress ip = IPAddress.Parse("127.0.0.1");
        List<ClientObject> clients = new List<ClientObject>(); // все подключения
        ClientContext db;

        public ServerObject(ClientContext db)
        {
            IPEndPoint ep = new IPEndPoint(ip, 1024);
            server.Bind(ep);
            server.Listen(10);
            this.db = db;
        }

        protected internal void AddConnection(ClientObject clientObject)
        {
            clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id)
        {
            // получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                clients.Remove(client);
        }
        // прослушивание входящих подключений
        protected internal void Listen()
        {
            try
            {
                while (true)
                {
                    Socket client = server.Accept();

                    ClientObject clientObject = new ClientObject(client, this, db);
                    Task.Run(clientObject.Process);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        // трансляция сообщения подключенным клиентам
        protected internal void BroadcastMessage(string message, string id)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Id != id) // если id клиента не равно id отправляющего
                {
                    clients[i].client.Send(data); //передача данных
                }
            }
        }
        // отключение всех клиентов
        protected internal void Disconnect()
        {
            server.Close(); //остановка сервера

            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }
    }
}
