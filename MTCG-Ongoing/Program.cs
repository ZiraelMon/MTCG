using MTCG.Controller;
using MTCG.Server;
using Npgsql;
using System;
using System.Data;
using System.Text;
using System.Threading;



namespace MTCG {
    public static partial class Program {

        static void Main(string[] args) {
            HttpSvr svr = new HttpSvr();
            Console.WriteLine("Server started");
            svr.Incoming += _Svr_Incoming;

            svr.Run();
        }

        public static void _Svr_Incoming(object sender, HttpSvrEventArgs e) {
            // Use a lambda expression to adapt to WaitCallback's expected signature.
            ThreadPool.QueueUserWorkItem(state => {
                if (state is HttpSvrEventArgs evt) {
                    Routing._Svr_Incoming(evt);
                }
            }, e);
        }
    }
}
