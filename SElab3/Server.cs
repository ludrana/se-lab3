using System;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

// git : git push origin master:part2


// match ids to files
// everything else after file upload
namespace Server
{
    internal class Program
    {
        public static Semaphore semaphore = new Semaphore(1, 1);
        int counter = 0;

        static void Main()
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipPoint);
            socket.Listen(1000);
            Console.WriteLine("Server started!");

            while (true)
            {
                Socket client = socket.Accept();

                new Thread(async() => await Interaction(client)).Start();
            }

            
        }
        static async Task Interaction(Socket client)
        {
            var responseBytes = new byte[512];
            var bytes = await client.ReceiveAsync(responseBytes);
            string response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
            if (response == "exit")
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                Environment.Exit(0);
            }
            string[] clientArgs = response.Split(' ', 2);
            string message = "";
            string strExeFilePath = Assembly.GetExecutingAssembly().Location;
            string strWorkPath = Path.GetDirectoryName(strExeFilePath);
            string dir = Path.Combine(strWorkPath, "server\\data");
            Directory.CreateDirectory(dir);
            var filename = Path.Combine(dir, clientArgs[1]);

            // response handling
            if (clientArgs[0] == "PUT")
            {
                if (File.Exists(filename))
                {
                    message += "403";
                }
                else
                {
                    try
                    {
                        var buffer = new byte[512];
                        int b;
                        FileStream fs = new(filename, FileMode.OpenOrCreate);
                        do
                        {
                            b = await client.ReceiveAsync(buffer);
                            fs.Write(buffer, 0, b);

                        } while (b > 0);
                        fs.Dispose();
                        fs.Close();
                        message += "200";
                    }
                    catch
                    {
                        message += "403";
                    }

                }
            }
            else if (clientArgs[0] == "DELETE")
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        File.Delete(filename);
                        message += "200";
                    }
                    catch
                    {
                        message += "404";
                    }
                }
                else
                {
                    message += "404";
                }

            }
            else
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        string content = File.ReadAllText(filename);
                        message += "200 " + content;
                    }
                    catch
                    {
                        message += "404";
                    }
                }
                else
                {
                    message += "404";
                }
            }
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await client.SendAsync(messageBytes);

            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
    }
}