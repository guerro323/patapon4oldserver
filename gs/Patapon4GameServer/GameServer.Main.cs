using CSScriptLibrary;
using Lidgren.Network;
using Patapon4GameServer.Core;
using Patapon4GameServer.Core.Assets;
using Patapon4GameServer.Core.Network;
using Patapon4GameServer.MSGSerializer;
using Patapon4GameServer.Play;
using PataponPhotonShared;
using PataponPhotonShared.Enums;
using PataponPhotonShared.Helper;
using PataponPhotonShared.Schemes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using static PataponPhotonShared.MessageExtension;

namespace Patapon4GameServer
{
    [Serializable]
    public partial class GameServer
    {
        const int FPS = 30;

        public enum LaunchType
        {
            Manual,
            Unity,
            Master
        }

		/// <summary>
		/// Current running gameServer
		/// </summary>
		/// <remarks>
		/// There should be only one per dll!
		/// </remarks>
		public static GameServer gameServer;
		/// <summary>
		/// Global peer (server <> client, server <> masterServer)
		/// </summary>
		public static NetPeer UsingPeer = null;
		public static GameRoom Room;

        public static NetConnection MasterConnection;

        public static GameUser HostUser;
        public static GamePlayer HostPlayer;

        public LaunchType launchType;

        /// <summary>
        /// Managing Domain
        /// </summary>
        public AppDomain domain;

        /// <summary>
        /// Liste d'attente des joueurs qui ont reçu le message dans le désordre (connection -> EVENT_ON_PLAYERCONNECT)
        /// </summary>
        public Dictionary<string, NetConnection> waitedUsers = new Dictionary<string, NetConnection>();
        /// <summary>
        /// Current user list
        /// </summary>
        public Dictionary<string, GameUser> usersInRoom = new Dictionary<string, GameUser>();
        /// <summary>
        /// Current player list created from the user list
        /// </summary>
        public Dictionary<string, GamePlayer> playersInRoom = new Dictionary<string, GamePlayer>();
        /// <summary>
        /// The first array is the team, the second is the index army
        /// </summary>
        public PlayerEntity[][] EntitiesInRoom = new PlayerEntity[2][];

        public static RythmEngine RythmEngine = new RythmEngine();

        public bool clientSide;
        public bool stop;

        double timeLeft_BeforeShutdown = -1f;

        public static void Main(string[] args)
        {
            Console.Title = "Initializing server.";

            // Load maps
            MissionManager.DefaultMapsInfo = GameMap.ListAll(File.ReadAllText("Maps/map_List.xml"))
                .ToDictionary(key => "d_" + key.Id.ToLower(), value => value);
            // Load entities
            MissionManager.DefaultEntitiyAssets = EntityAssetInfo.ListAll(File.ReadAllText("Scripts/Entities/entity_List.xml"))
                .ToDictionary(key => "d_" + key.Id.ToLower(), value => value);

            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;
            CSScript.ShareHostRefAssemblies = true;
            CSScript.AssemblyResolvingEnabled = true;

            gameServer = new GameServer();

            gameServer.launchType = LaunchType.Manual;

            if (args == null || args.Count() == 0) args = new string[] { "42348", "127.0.0.1", "45646" };
            var configuration = new NetPeerConfiguration("(B.A0001)patapon4-alphaclient")
            {
                ReceiveBufferSize = 1310710,
                SendBufferSize = 1310710,
                Port = int.Parse(args[0]),
            };
            Console.WriteLine(configuration.SendBufferSize);
            configuration.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            configuration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            configuration.AcceptIncomingConnections = true;

            NetPeer wantedPeer = new NetPeer(configuration);

            gameServer.Start(wantedPeer, args[1].ToString(), int.Parse(args[2]), null);
        }

        public void Start(NetPeer knowPeer, string masterServerAdress, int masterServerPort, object caller)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            entitySystem = new UnityActor.ActorSystem("entitySystem")
            {
                Options = new UnityActor.ActorSystemOptions()
                {
                    MultipleSameIdAllowed = false,
                    ThrowErrorOnExistingID = false, //: 1
                }
            };

            //: 1
            // Cela permettra d'avoir plusieurs entités d'un même groupe sans problème
            // Puis de toute façon, il n y aura pas de problèmes avec les joueurs :
            // Ils seront remplacé.

            gameServer = this;

            UsingPeer = knowPeer;

            UsingPeer.RegisterReceivedCallback(new SendOrPostCallback((o) =>
            { try { ReceiveIncomingMessage((NetPeer)o); } catch (Exception ex) { Console.WriteLine($"ERROR: {ex.Message}\n{ex.StackTrace}"); }
            }));

            UsingPeer.Start();

            var hailMessage = UsingPeer.CreateMessage("CONNECT");
            hailMessage.Write("GAMEROOM");


            UsingPeer.Connect(masterServerAdress, masterServerPort, hailMessage);

            //peer.Connect("127.0.0.1", 7641);
            Time.StartTime = TimeSpan.Zero;

            Time.Timer = new System.Timers.Timer(10);
            Time.Timer.Elapsed += new System.Timers.ElapsedEventHandler((object obj, System.Timers.ElapsedEventArgs args) =>
            {
                Time.LastTime = Time.TimeElapsed;
                Time.TickElapsed = args.SignalTime.Millisecond;
            });
            Time.Timer.Enabled = true;

            if (caller != null && caller.GetType() == typeof(MonoBehaviour))
                caller.GetType().GetMethod("print").Invoke(this, new object[] { "started!" });

            bool createdNew;
            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, (Guid.NewGuid().ToString() + Guid.NewGuid().ToString()), out createdNew);
            var signaled = false;

            //var timer = new Timer(, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            Console.Title = "Connecting";

            while (UsingPeer.Status != NetPeerStatus.Running) ;
            //Room.hasLoaded = true;

            /*Log($@"Room info:
Creator Name: {Room.creatorName}
Room Description: {Room.description}
Adress: {Room.ipAdress}:{Room.port}
");*/

           /* var testMsg = UsingPeer.CreateMessage().Start(Constants.SYSTEM.DEBUG);
            testMsg.Subscribe((netMsg) => Console.WriteLine("Seems like it worked, no?"));

            UsingPeer.SendUnconnectedToSelf(testMsg);*/

            Console.WriteLine("ke");

            timeLeft_BeforeShutdown = -1f;
            shutdownSent = false;
            currentGameState = EGameState.Basement;

            do
            {
                signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(1f / FPS));
                Update();
            }
            while (!signaled && !stop);

            while (!shutdownSent) ;
        }

        bool shutdownSent = false;
        bool isTrigger = false;

        public void Update()
        {
            Time.TimeElapsed = (float)Environment.TickCount / 1000f;

            if (Room != null)
            Console.Title = $"#{GameServer.Room.guid}, pc {GameServer.gameServer.usersInRoom.Count}, cb {GameServer.Room.creatorName}";

            UpdateTimeDelta();
            GameLoop();

            //Console.WriteLine("hello");

            if (usersInRoom.Count() <= 0 || UsingPeer.ConnectionsCount <= 1)
            {
                if (timeLeft_BeforeShutdown == -1f)
                {
                    Log(">>> triggered!");
                    timeLeft_BeforeShutdown = Time.TimeElapsed + 20f;
                }

                if (timeLeft_BeforeShutdown < Time.TimeElapsed)
                {
                    Stop(1); // No Players Reason
                }
            }
            else
            {
                timeLeft_BeforeShutdown = -1f;
            }

            if (isTrigger)
            {
                isTrigger = !isTrigger;
            }
        }

        /// <summary>
        /// Stop the game server
        /// </summary>
        /// <param name="reason">1: No Players 2: Restart</param>
        public void Stop(int reason = 0)
        {
            /*if (reason == 1)
                Log($"[Server #{Room.guid} {Room.creatorName}]Server Stopped: No players on the server!");
            */
            stop = true;
            UsingPeer.Shutdown("STOP");
            //AppDomain.Unload(domain);

            //Environment.Exit(0);
        }

		/// <summary>
		/// On va lire les messages des clients et du masterserver
		/// </summary>
        async void ReceiveIncomingMessage(NetPeer peer)
        {
            var msg = peer.ReadMessage();
             
            NetOutgoingMessage replyMessage = null;

            if (msg != null)
            {
                Log("t: " + msg.MessageType);
                if (msg.MessageType == NetIncomingMessageType.UnconnectedData)
                {
                    // Self
                    if (msg.ReadString() == "ARMY")
                    {
                        var army = msg.ReadArmyScheme();
                        army.Build();
                        Console.WriteLine("NORMAL::\n");
                        Console.WriteLine(PatapolisArmyScheme.ToString() + "\n");
                        Console.WriteLine("SERIALIZED::\n");
                        Console.WriteLine(army.ToString());
                    }
                }
                if (msg.MessageType == NetIncomingMessageType.ConnectionApproval)
                {
                    msg.SenderConnection.Approve();
                }

                if (msg.MessageType == NetIncomingMessageType.DebugMessage)
                {
                    try
                    {
                        Log(msg.ReadString());
                    }
                    catch { }
                }
                if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    Log("WA!!! : " + msg.ReadString());
                }
                if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    Console.WriteLine("New status " + msg.SenderConnection.Status);
                    if (msg.SenderConnection.GameUserExist())
                    {
                        if (msg.SenderConnection.Status == NetConnectionStatus.Disconnected)
                        {
                            CheckUserStatus();
                            usersInRoom.Remove(msg.SenderConnection.GetGameUser().login);
                        }
                    }

                    if (msg.SenderConnection.Peer == UsingPeer
                        && msg.SenderConnection.Status == NetConnectionStatus.Disconnected)
                    {
                        shutdownSent = true;
                    }
                }

                /*if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    try
                    {
                        if (msg.PeekString() == "REPLY")
                        {
                            msg.ReadString(); //< skip
                            var handle = msg.ReadInt64();
                            Message.TriggerHandler(handle, msg);
                        }
                    }
                    catch { }
                }*/

                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    var header = msg.PeekString();
                    Log("header:" + header);
                    if (header == "USERCONNECT")
                    {
                        msg.ReadString();
                        msg.SenderConnection.Tag = "USER";
                        string login = msg.ReadString();
                        Log("(Connection) Wanted connection + " + login);
                        if (!string.IsNullOrEmpty(login) && usersInRoom.TryGetValue(login, out var outputUser))
                        {
                            outputUser.AddNetConnection(msg.SenderConnection);
                        }
                        else if (!string.IsNullOrEmpty(login))
                        {
                            waitedUsers[login] = msg.SenderConnection;
                            Log("(Connection) Will wait for " + login);
                        }

                        await Task.Factory.StartNew(() => 
                        {
                            while (waitedUsers.ContainsKey(login)) ;
                        });

                        foreach (var user in usersInRoom)
                        {
                            if (user.Value == null)
                                continue;
                            if (user.Value.ownedPlayers[0].EntitiesInPossession != null)
                                foreach (var entity in user.Value.ownedPlayers[0].EntitiesInPossession)
                                    OutGameSystem.SendNewEntityTo(usersInRoom[login], user.Value.login, entity);
                        }
                    }

                    PataMessage<object> pataMsg = null;
                    if ((pataMsg = Message.Read<object>(msg)) != null)
                    {
                        Console.WriteLine("pataMsg received!" + pataMsg.contents.ContentType);
                        switch (pataMsg.contents.ContentType)
                        {
                            case Constants.MESSAGETYPE.SYSTEM:
                                {
                                    if (msg.PeekString() == Constants.SYSTEM.HANDLE)
                                    {
                                        msg.ReadString(); //< Skip
                                        var handle = msg.ReadInt32();

                                        var sendMsg = UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.SYSTEM);
                                        sendMsg.Write(Constants.SYSTEM.HANDLE_SUCCESS);
                                        sendMsg.Write(handle);

                                        UsingPeer.SendMessage(sendMsg, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                    }
                                    if (msg.PeekString() == Constants.SYSTEM.HANDLE_SUCCESS)
                                    {
                                        msg.ReadString(); //< Skip
                                        var handle = msg.ReadInt32();
                                        var hash = (Hashtable)msg.SenderConnection.Peer.Tag;
                                        hash["handle"] = handle;
                                    }
                                    break;
                                }
                            // Si le message est de Type OUTGAME
                            // Donc les connections des joueurs, hors gameplay quoi
                            case Constants.MESSAGETYPE.OUTGAME:
                                {
                                    OutGameSystem.Receive(msg, msg.ReadString());
                                    break;
                                }
                            case Constants.MESSAGETYPE.INGAME:
                                {
                                    InGameSystem.Receive(msg, msg.ReadString());
                                    break;
                                }
                            case Constants.MESSAGETYPE.CHAT:
                                {
                                    if (msg.ReadString() == Constants.CHAT.GO_SENDMESSAGE)
                                    {
                                        var messageFP = msg.ReadString();
                                        var messageType = msg.ReadByte();
                                        var sendMsg = UsingPeer.CreateMessage()
                                            .Start(Constants.MESSAGETYPE.CHAT);
                                        sendMsg.Write(Constants.CHAT.EVENT_ON_MESSAGERECEIVED);
                                        sendMsg.Write(msg.SenderConnection.GetGameUser()?.login);
                                        sendMsg.Write(messageFP);
                                        sendMsg.Write(messageType);
                                        var objSerialized = msg.SenderConnection.GetGameUser().Serialize();
                                        sendMsg.Write(objSerialized.Length);
                                        sendMsg.Write(objSerialized);

                                        Log("Replying message... to:");

                                        var userList = new List<NetConnection>();
                                        foreach (var user in usersInRoom)
                                        {
                                            if (user.Value.IfInitiliazed() != null)
                                            {
                                                userList.Add(user.Value.GetNetConnection());
                                                Log("<>" + user.Value.login);
                                            }
                                        }

                                        if (userList.Count > 0)
                                            UsingPeer.SendMessage(sendMsg, userList,
                                                NetDeliveryMethod.ReliableOrdered, 0);
                                    }
                                    break;
                                }

                            case Constants.SYSTEM.DEBUG:
                                {
                                    Console.WriteLine(msg.ReadString());
                                    break;
                                }
                        }
                    }
                }
            }
            MasterMessageLoop();
            ClientMessageLoop();
        }

        void UpdateTimeDelta()
        {
            Time.UpdateTimeDelta();
        }

		public static T Log<T>(T toLog)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(toLog);
			Console.ResetColor();

			return toLog;
		}

        public static int AvailablePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static List<NetConnection> GetUserConnections()
        {
            var userList = new List<NetConnection>();
            foreach (var user in gameServer.usersInRoom)
            {
                if (user.Value.IfInitiliazed() != null)
                {
                    userList.Add(user.Value.GetNetConnection());
                }
            }
            return userList;
        }

    }
}
