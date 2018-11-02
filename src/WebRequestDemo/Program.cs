using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WebRequestDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create("http://www.baidu.com/");
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                // 请求成功的状态码：200
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using(Stream stream = response.GetResponseStream())
                    {
                        using(StreamReader reader = new StreamReader(stream))
                        {
                            string html = reader.ReadToEnd();
                            Console.WriteLine(html);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("服务器返回错误：{0}",response.StatusCode);
                }
            }

            Console.ReadKey();
        }
    }
}
