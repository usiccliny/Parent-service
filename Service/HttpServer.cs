using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    internal class HttpServer
    {
        private HttpListener listener;

        public HttpServer(string url)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);

            listener.Start();
        }

        public void StartListening()
        {
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod == "POST")
            {
                    using (Stream body = request.InputStream)
                    {
                        using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
                        {
                            string requestBody = reader.ReadToEnd();
                            Console.WriteLine($"Получен POST запрос: { requestBody}");

                            // Добавьте здесь вашу логику обработки полученного запроса
                        }
                    }

                    HttpListenerResponse response = context.Response;
                    string responseString = "OK";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;

                    using (Stream output = response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        public void StopListening()
        {
            listener.Stop();
            listener.Close();
        }
    }
}

