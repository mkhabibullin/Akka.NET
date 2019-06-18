using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Shared;
using System;

namespace Seed
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Seed node";
            var config = ConfigurationFactory.ParseString(@"akka {  
              stdout-loglevel = DEBUG
              loglevel = INFO
              actor {
                provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
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
                  port = 2551        
                }
              }
              cluster {
                roles = [""seed""]
                seed-nodes = [""akka.tcp://singleton-cluster-system@127.0.0.1:2551""]
                auto-down-unreachable-after = 10s
            
                singleton {
                  singleton-name = ""manager""
                  role = """"
                  hand-over-retry-interval = 1s
                }
              }
            }");
            using (var system = ActorSystem.Create("singleton-cluster-system", config))
            {
                //RunClusterSingletonSeed(system);
                RunDistributedPubSubSeed(system);
                RunClusterClientSeed(system);

                Console.ReadLine();
            }
        }

        ///// <summary>
        ///// Initializes cluster singleton of the <see cref="WorkerManager"/> actor.
        ///// </summary>
        ///// <param name="system"></param>
        //static void RunClusterSingletonSeed(ActorSystem system)
        //{
        //    var aref = system.ActorOf(ClusterSingletonManager.Props(
        //        singletonProps: Props.Create(() => new WorkerManager()),
        //        terminationMessage: PoisonPill.Instance,
        //        settings: ClusterSingletonManagerSettings.Create(system)),
        //        name: "manager");
        //}

        /// <summary>
        /// Starts a job, which publishes <see cref="Echo"/> message to distributed cluster pub sub in 5 sec periods.
        /// </summary>
        static void RunDistributedPubSubSeed(ActorSystem system)
        {
            var mediator = DistributedPubSub.Get(system).Mediator;

            system.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), mediator,
                new Publish("echo", new Echo("hello world")), ActorRefs.NoSender);
        }

        /// <summary>
        /// Starts a job, which establishes cluster client receptionist for target <see cref="EchoReceiver"/> actor,
        /// making it visible from outside of the cluster.
        /// </summary>
        static void RunClusterClientSeed(ActorSystem system)
        {
            var receptionist = ClusterClientReceptionist.Get(system);
            receptionist.RegisterService(system.ActorOf(Props.Create<EchoReceiver>(), "my-service"));
        }
    }
}
