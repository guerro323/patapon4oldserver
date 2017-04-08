using Lidgren.Network;
using PataponPhotonShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Patapon4GlobalServer
{
    public class RoomManager
    {
        public static List<GameRoom> knowRooms => SystemMessage.rooms;
        public static Dictionary<Thread, GameRoom> threadRooms = new Dictionary<Thread, GameRoom>();
        public static List<Process> processRooms = new List<Process>();

        public static void AddRoom(GameRoom room, int gamePort)
        {
            room.thread = new Thread(RoomStart);
            threadRooms[room.thread] = room;

            room.thread.Start(new object[] { room, gamePort });
        }

        public static void RoomStart(object @object)
        {
            object[] objects = (object[])@object;
            GameRoom room = (GameRoom)objects[0];

            room.hasLoaded = false;
            processRooms.Add(Process.Start("C:/Users/Guerro/Documents/Visual Studio 2015/Projects/Patapon4GameServer/Patapon4GameServer/bin/Debug/Patapon4GameServer.exe", $"{room.port} {Program.currentIP} {Program.Server.Port}"));
            /* int gamePort = (int)objects[1];

             Console.WriteLine(room.creatorName);

             AppDomain domain = AppDomain.CreateDomain("PataponGameServer#" + room.guid);
             GameServer gameServer = (GameServer)AppDomain.CurrentDomain.CreateInstanceAndUnwrap(Assembly.GetAssembly(typeof(GameServer)).FullName, typeof(GameServer).FullName);
             gameServer.domain = domain;
             gameServer.Room = room;
             gameServer.launchType = GameServer.LaunchType.Master;
             room.gameServer = gameServer;
             room.hasLoaded = false;

             var configuration = new NetPeerConfiguration("(B.A0001)patapon4-alphaclient")
             {
                 Port = gamePort,
             };
             configuration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
             configuration.AcceptIncomingConnections = true;

             NetPeer wantedPeer = new NetPeer(configuration);

 #if !DEBUG
             try
             {
                 gameServer.Start(wantedPeer, Dns.GetHostAddresses("127.0.0.1").First().ToString(), Program.Server.Port, null);
             }
             catch (Exception ex)
             {
                 Console.WriteLine("ERROR GAMESERVER : " + ex.StackTrace);
             }
 #endif
             // cela permettra de voir les exceptions plus facilement
             gameServer.Start(wantedPeer, Dns.GetHostAddresses("127.0.0.1").First().ToString(), Program.Server.Port, null);*/
        }

        public static int AvailablePort()
        {
			TcpListener l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			int port = ((IPEndPoint)l.LocalEndpoint).Port;
			l.Stop();
			return port;
		}

        public static void Update()
        {

        }
    }
}
