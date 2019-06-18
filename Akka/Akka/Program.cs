using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Dsl;
using static Akka.A;

namespace Akka
{
    class Program
    {
        /// <summary>
        /// https://getakka.net/articles/streams/quickstart.html
        /// </summary>
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            using (var materializer = system.Materializer())
            {
                var sink = Sink.ForEach<AMsg>(m =>
                {
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine(m.Value);
                });

                var queue = Source.Queue<AMsg>(10, OverflowStrategy.Backpressure);
                var preMaterializer = queue.PreMaterialize(materializer);

                var runnableQueue = preMaterializer.Item2
                    .ToMaterialized(sink, Keep.Right)
                    .Run(materializer);

                foreach (var v in Enumerable.Range(1, 100))
                {
                    var result = preMaterializer.Item1.OfferAsync(new AMsg(v.ToString())).Result;
                    Console.WriteLine(".");
                }

                //-----------------------------------------------------------------------------------------------------
                //var source = Source.From(Enumerable.Range(1, 100));
                //var sink = Sink.Sum<int>((p, v) => p + v);
                //IRunnableGraph<Task<int>> runnable = source.ToMaterialized(sink, Keep.Right);
                //var result = runnable.Run(materializer).Result;

                //Console.WriteLine($"The result is: {result}");

                //-----------------------------------------------------------------------------------------------------
                //Source<int, NotUsed> source = Source.From(Enumerable.Range(1, 100));
                ////source.RunForeach(i => Console.WriteLine(i.ToString()), materializer);

                //var factorials = source.Scan(new BigInteger(1), (acc, next) =>
                //{
                //    Console.WriteLine($"The factor is working on: {acc}");
                //    return acc * new BigInteger(next);
                //});
                //var result =
                //    factorials
                //        .Select(num => ByteString.FromString($"{num}\n"))
                //        .RunWith(FileIO.ToFile(new FileInfo("factorials.txt"), FileMode.OpenOrCreate), materializer);
                //-----------------------------------------------------------------------------------------------------
            }

            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }

    internal class A : ReceiveActor
    {
        public class AMsg
        {
            public string Value { get; }

            public AMsg(string value)
            {
                Value = value;
            }
        }

        public A()
        {
            Receive<AMsg>(msg =>
            {
                Console.WriteLine(msg.Value);
            });
        }
    }
}
