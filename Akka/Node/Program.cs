using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Shared;
using System;

namespace Node
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Attached node";
            var config = ConfigurationFactory.ParseString(@"akka {  
              stdout-loglevel = DEBUG
              loglevel = INFO        
              actor {
                provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
               # provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                serializers {
                  wire = ""Akka.Serialization.WireSerializer, Akka.Serialization.Wire""
                }
                serialization-bindings {
                  ""System.Object"" = wire
                }
              }
              remote {
                log-remote-lifecycle-events = off
                dot-netty.tcp {
                  hostname = ""127.0.0.1""
                  port = 0       
                }
              }
              cluster {
                roles = []
                seed-nodes = [""akka.tcp://singleton-cluster-system@127.0.0.1:2551""]
                auto-down-unreachable-after = 10s
            
                singleton-proxy {
                  singleton-name = ""manager""
                  role = """"
                  singleton-identification-interval = 1s
                  buffer-size = 100
                }
            
                client {
                  initial-contacts = [""akka.tcp://singleton-cluster-system@127.0.0.1:2551/system/receptionist""]
                }
              }
            }");
            using (var system = ActorSystem.Create("singleton-cluster-system", config))
            {
                //RunClusterSingletonClient(system);
                RunDistributedPubSubClient(system);
                RunClusterClient(system);

                Console.ReadLine();
            }
        }

        /// <summary>
        /// Creates an <see cref="EchoReceiver"/> actor which subscribes to the distributed pub/sub topic.
        /// This topic is filled with messages from the cluster seed job.
        /// </summary>
        static void RunDistributedPubSubClient(ActorSystem system)
        {
            var echo = system.ActorOf(Props.Create(() => new EchoReceiver()));
            echo.Tell(new object());
        }

        /// <summary>
        /// Creates a proxy to communicate with cluster singleton initialized by the seed.
        /// </summary>
        static void RunClusterSingletonClient(ActorSystem system)
        {
            var proxyRef = system.ActorOf(ClusterSingletonProxy.Props(
                singletonManagerPath: "/user/manager",
                settings: ClusterSingletonProxySettings.Create(system).WithRole("worker")),
                name: "managerProxy");
        }

        /// <summary>
        /// Creates a cluster client, that allows to connect to cluster even thou current actor system is not part of it.
        /// </summary>
        static void RunClusterClient(ActorSystem system)
        {
            //NOTE: to properly run cluster client set up actor ref provider for nodes on `provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"`
            system.Settings.InjectTopLevelFallback(ClusterClientReceptionist.DefaultConfig());
            var clusterClient = system.ActorOf(ClusterClient.Props(ClusterClientSettings.Create(system)));
            clusterClient.Tell(new ClusterClient.Send("/user/my-service", new Echo("hello from cluster client")));

            Console.WriteLine("Enter the message: ");
            while (true)
            {
                var msg = Console.ReadLine();
                clusterClient.Tell(new ClusterClient.Send("/user/my-service", new Echo(msg)));
            }
        }
    }
}
