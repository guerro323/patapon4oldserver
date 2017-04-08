using Lidgren.Network;
using Patapon4GameServer;
using Patapon4GameServer.Extension;
using PataponPhotonShared;
using PataponPhotonShared.Entity;
using PataponPhotonShared.Helper;
using PataponPhotonShared.Schemes;
using PataponPhotonShared.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Global = Patapon4GlobalServer.SystemMessage;

namespace Patapon4GlobalServer
{
    public static class OutGameMessage
    {
        private static NetServer Server => Program.Server;

        public static readonly ArmyScheme StandardArmyScheme = new ArmyScheme("playermain")
        {
            subs = new ArmyScheme[]
            {
                new ArmyScheme("uberheroarmy")
                {
                    leaderEntities = new EntityTypeScheme?[] { new EntityTypeScheme("uberhero", ActorConstants.ACScheme.TreeAllUberClasses) }
                },
                new ArmyScheme(ActorConstants.ACScheme.TateArmy)
                {
                    leaderEntities = new EntityTypeScheme?[] { new EntityTypeScheme("chef", ActorConstants.ACScheme.TreeAllNormalUberClasses) },
                    armyEntities = new EntityTypeScheme?[6]
                    {
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                    },
                },
                new ArmyScheme(ActorConstants.ACScheme.YariArmy)
                {
                    leaderEntities = new EntityTypeScheme?[] { new EntityTypeScheme("chef", ActorConstants.ACScheme.TreeAllNormalUberClasses) },
                    armyEntities = new EntityTypeScheme?[6]
                    {
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                    },
                },
                new ArmyScheme(ActorConstants.ACScheme.YumiArmy)
                {
                    leaderEntities = new EntityTypeScheme?[] { new EntityTypeScheme("chef", ActorConstants.ACScheme.TreeAllNormalUberClasses) },
                    armyEntities = new EntityTypeScheme?[6]
                    {
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllTateClasses),
                    },
                },
			    // "R" On met les renforts
			    new ArmyScheme("renforts")
                {
                    armyEntities = new EntityTypeScheme?[2]
                    {
                        new EntityTypeScheme("renfort", "renfort"),
                        new EntityTypeScheme("renfort", "renfort")
                    }
                }
            }
        };

        public static List<ItemData> DefaultItems => ItemBank.BasicItems.ToList();
        public static ItemData LockedItem = new ItemData("locked", "locked", ItemData.EType.LevelLocked, ItemData.ERarity.Basic, 0, string.Empty);

        private static ItemData _DefaultHelm => DefaultItems.Where(i => i.Type == ItemData.EType.Helm).First();
        private static int _DefaultHelmIndex => DefaultItems.IndexOf(DefaultItems.Where(i => i.Type == ItemData.EType.Helm).First());
        private static ItemData _DefaultShield => DefaultItems.Where(i => i.Type == ItemData.EType.Shield).First();
        private static int _DefaultShieldIndex => DefaultItems.IndexOf(DefaultItems.Where(i => i.Type == ItemData.EType.Shield).First());
        private static ItemData _DefaultSword => DefaultItems.Where(i => i.Type == ItemData.EType.Sword).First();
        private static int _DefaultSwordIndex => DefaultItems.IndexOf(DefaultItems.Where(i => i.Type == ItemData.EType.Sword).First());
        private static ItemData _DefaultBow => DefaultItems.Where(i => i.Type == ItemData.EType.Bow).First();
        private static int _DefaultBowIndex => DefaultItems.IndexOf(DefaultItems.Where(i => i.Type == ItemData.EType.Bow).First());
        private static ItemData _DefaultSpear => DefaultItems.Where(i => i.Type == ItemData.EType.Spear).First();
        private static int _DefaultSpearIndex => DefaultItems.IndexOf(DefaultItems.Where(i => i.Type == ItemData.EType.Spear).First());
        private static ItemData _DefaultBigShuriken => DefaultItems.Where(i => i.Type == ItemData.EType.BigShuriken).First();
        private static int _DefaultBigShurikenIndex => DefaultItems.IndexOf(DefaultItems.Where(i => i.Type == ItemData.EType.BigShuriken).First());

        public static async void Receive(NetIncomingMessage message, string type)
        {

            switch (type)
            {
                // [MESSAGE STRUCTURE]
                // Index
                case Constants.OUTGAME.SET_PLAYERINDEX:
                    {
                        byte reason = 0;
                        if (Global.peerGameUsers.TryGetValue(message.SenderConnection, out var user))
                        {
                            int askedIndex = message.ReadByte();
                            if (user.ownedPlayers.ContainsKey(askedIndex))
                            {
                                user.currentPlayerIndex = askedIndex;

                                var netMsg = Server.CreateMessage("HEADER");
                                netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                netMsg.Write(Constants.OUTGAME.SUCCESS_SETTINGPLAYERINDEX);
                                Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                                return;
                            }
                            else
                            {
                                reason = 1;
                                goto error;
                            }
                        }
                        else
                            reason = 2;

                        error:
                        {
                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.ERROR_SETTINGPLAYERINDEX);
                            netMsg.Write(reason);
                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        }

                        break;
                    }
                // [MESSAGE STRUCTURE]
                // MightyName
                case Constants.OUTGAME.GO_PLAYERCREATE:
                    {
                        byte reason = 0;
                        if (Global.peerGameUsers.TryGetValue(message.SenderConnection, out var user))
                        {
                            int? firstAvailable = Enumerable.Range(0, int.MaxValue)
                                                            .Except(user.ownedPlayers.Keys)
                                                            .FirstOrDefault();
                            Console.WriteLine(firstAvailable);

                            if (firstAvailable == null)
                            {
                                reason = 1;
                                goto error;
                            }

                            user.currentPlayerIndex = firstAvailable.Value;

                            user.ownedPlayers[firstAvailable.Value] = new GamePlayer()
                            {
                                MightyName = message.ReadString(),
                                User = user,
                                wasCreated = false
                            };

                            if (user.ownedPlayers[user.currentPlayerIndex].MightyName != null
                                || user.ownedPlayers[user.currentPlayerIndex].MightyName.Length <= 0)
                            {
                                user.ownedPlayers[user.currentPlayerIndex].wasCreated = true;
                            }

                            GamePlayer player = user.ownedPlayers[user.currentPlayerIndex];
                            byte[] objSeralized = null;

                            player.EntitiesInPossession = new List<EntityData>();
                            player.Inventory = DefaultItems.ToArray();

                            player.EntitiesInPossession.Add(new EntityData()
                            {
                                SchemeID = "#uberheroarmy[uberhero,0]",
                                ServerID = 0,
                                CurrentClass = Constants.PlayerClass.Taterazay,
                                CurrentRarepon = Constants.PlayerRarepon.Default,
                                RareponsInPossession = new Dictionary<string, int>(),
                                Type = EntityData.EntityDataType.Chef,
                                ClassesInPossession = new Dictionary<string, ClassDataInfo>()
                                {
                                    { Constants.PlayerClass.Taterazay, new ClassDataInfo(1563, new [] { _DefaultHelmIndex, _DefaultSwordIndex, _DefaultShieldIndex }, player) },
                                    { Constants.PlayerClass.Yarida, new ClassDataInfo(0, new [] { _DefaultHelmIndex, _DefaultSpearIndex }, player) },
                                    { Constants.PlayerClass.Yumiyacha, new ClassDataInfo(0, new [] { _DefaultHelmIndex, _DefaultBowIndex }, player) },
                                    { Constants.PlayerClass.Shurika, new ClassDataInfo(0, new [] { _DefaultHelmIndex, _DefaultBigShurikenIndex }, player) },
                                }
                            });
                            player.EntitiesInPossession.Add(new EntityData()
                            {
                                SchemeID = ActorConstants.ACScheme.TateArmy + "[chef,0]",
                                ServerID = 1,
                                CurrentClass = Constants.PlayerClass.Taterazay,
                                CurrentRarepon = Constants.PlayerRarepon.Default,
                                Type = EntityData.EntityDataType.Chef,
                                ClassesInPossession = new Dictionary<string, ClassDataInfo>()
                                {
                                    { Constants.PlayerClass.Taterazay, new ClassDataInfo(0, new [] { _DefaultHelmIndex, _DefaultSwordIndex, _DefaultShieldIndex }, player) }
                                }
                            });
                            player.EntitiesInPossession.Add(new EntityData()
                            {
                                SchemeID = ActorConstants.ACScheme.TateArmy + "(unit,0)",
                                ServerID = 2,
                                CurrentClass = Constants.PlayerClass.Tatepon,
                                CurrentRarepon = Constants.PlayerRarepon.Default,
                                Type = EntityData.EntityDataType.Unit,
                                ArmyIndex = 0,
                                RareponsInPossession = new Dictionary<string, int>()
                                {
                                    { Constants.PlayerRarepon.Default, 0 }
                                },
                                ClassesInPossession = new Dictionary<string, ClassDataInfo>()
                                {
                                    { Constants.PlayerClass.Tatepon, new ClassDataInfo(0, new [] { _DefaultHelmIndex, _DefaultSwordIndex, _DefaultShieldIndex }, player) }
                                }
                            });

                            // [MESSAGE STRUCTURE]
                            // GAMEPLAYER
                            // GAMEPLAYER.SCHEME
                            // GAMEPLAYER.ARMYENTITIESINFO
                            // GAMEPLAYER.INVENTORY
                            // GAMEUSER.INDEX
                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.SUCCESS_CREATINGPLAYER);
                            netMsg.Write(player);

                            netMsg.Write(player.armyScheme = StandardArmyScheme);
                            objSeralized = player.EntitiesInPossession.ToArray().Serialize();
                            {
                                netMsg.Write(objSeralized.Length);
                                netMsg.Write(objSeralized);
                            }
                            objSeralized = player.Inventory.Serialize();
                            {
                                netMsg.Write(objSeralized.Length);
                                netMsg.Write(objSeralized);
                            }
                            netMsg.Write(user.currentPlayerIndex);
                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                            return;

                        }
                        else
                            reason = 2;

                        error:
                        {
                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.ERROR_CREATINGPLAYER);
                            netMsg.Write(reason);
                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        }

                        break;
                    }
                // [MESSAGE STRUCTURE]
                // Get Type (0: total)
                case Constants.OUTGAME.GET_PLAYERINFO:
                    {
                        byte reason = 0;
                        if (Global.peerGameUsers.TryGetValue(message.SenderConnection, out var user))
                        {
                            if (!user.ownedPlayers.ContainsKey(user.currentPlayerIndex))
                            {
                                reason = 1;
                                goto error;
                            }

                            var getType = message.ReadByte();
                            if (getType == 0)
                            {
                                GamePlayer player = null;
                                byte[] objSerialized = null;
                                // [MESSAGE STRUCTURE]
                                // GAMEPLAYER
                                // GAMEPLAYER.SCHEME
                                // GAMEPLAYER.ARMYENTITIESINFO
                                // GAMEPLAYER.INVENTORY
                                var netMsg = Server.CreateMessage("HEADER");
                                netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                netMsg.Write(Constants.OUTGAME.SUCCESS_GETTINGPLAYERINFO);
                                netMsg.Write(player = user.ownedPlayers[user.currentPlayerIndex]);

                                netMsg.Write(player.armyScheme = StandardArmyScheme);
                                netMsg.Write(player.EntitiesInPossession.Count);
                                foreach (var entity in player.EntitiesInPossession)
                                {
                                    objSerialized = entity.Serialize();
                                    netMsg.Write(objSerialized.Length);
                                    netMsg.Write(objSerialized);

                                    netMsg.Write(entity.ClassesInPossession.Count);
                                    foreach (var c in entity.ClassesInPossession)
                                    {
                                        netMsg.Write(c.Key);
                                        netMsg.Write(c.Value);
                                    }
                                }
                                objSerialized = player.Inventory.Serialize();
                                {
                                    netMsg.Write(objSerialized.Length);
                                    netMsg.Write(objSerialized);
                                }
                                Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                                return;
                            }
                            else
                            {
                                reason = 2;
                                goto error;
                            }
                        }
                        else
                            reason = 3;

                        error:
                        {
                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.ERROR_GETTINGPLAYERINFO);
                            netMsg.Write(reason);
                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        }

                        break;
                    }
                // [MESSAGE STRUCTURE]
                // EMPTY
                case Constants.OUTGAME.GET_SAVESDATA:
                    {
                        byte reason = 0;
                        if (Global.peerGameUsers.TryGetValue(message.SenderConnection, out var user))
                        {
                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.SUCCESS_GETTINGSAVESDATA);

                            var objSerialized = user.SavesData.Serialize();
                            {
                                netMsg.Write(objSerialized.Length);
                                netMsg.Write(objSerialized);
                            }

                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                            return;
                        }
                        else
                        {
                            reason = 1;
                            goto error;
                        }

                        error:
                        {
                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.ERROR_GETTINGSAVESDATA);
                            netMsg.Write(reason);
                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        }

                        break;
                    }
                case Constants.OUTGAME.UPDATE_SAVEDATA:
                    {
                        byte reason = 0;
                        if (Global.peerGameUsers.TryGetValue(message.SenderConnection, out var user))
                        {
                            var index = message.ReadInt32();
                            var newName = message.ReadString();
                            var currentMission = message.ReadString();
                            var lastTime = new DateTime(message.ReadInt64());

                            search:
                            if (user.SavesData.TryGetValue(index, out var save))
                            {
                                save.Index = index;
                                save.MightyName = newName;
                                save.LastMission = currentMission;
                                save.UserLogin = user.login;
                                save.LastTimePlayed = lastTime;

                                user.SavesData[index] = save;
                            }
                            else
                            {
                                save = new PataponPhotonShared.Structs.SaveData();
                                user.SavesData[index] = save;
                                goto search;
                            }

                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.SUCCESS_UPDATINGSAVEDATA);
                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                            return;
                        }
                        else
                        {
                            reason = 1;
                            goto error;
                        }

                        error:
                        {
                            var netMsg = Server.CreateMessage("HEADER");
                            netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                            netMsg.Write(Constants.OUTGAME.ERROR_UPDATINGSAVEDATA);
                            netMsg.Write(reason);
                            Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        }

                        break;
                    }
                case Constants.OUTGAME.UPDATE_ENTITYDATA:
                    {
                        if (Global.peerGameUsers.TryGetValue(message.SenderConnection, out var user))
                        {
                            int entityID = message.ReadInt32();
                            if (user.ownedPlayers.TryGetValue(user.currentPlayerIndex, out var player))
                            {
                                EntityData entity;
                                if ((entity = player.EntitiesInPossession.ElementAtOrDefault(entityID)) != null)
                                {
                                    var newData = message.ReadBytes(message.ReadInt32()).Deserialize<EntityData>();

                                    if (entity.ClassesInPossession.TryGetValue(newData.CurrentClass, out var entity_class))
                                    {
                                        entity.CurrentClass = newData.CurrentClass;
                                    }
                                    if (entity.RareponsInPossession.ContainsKey(newData.CurrentRarepon))
                                        entity.CurrentRarepon = newData.CurrentRarepon;

                                    byte[] objSerialized = new byte[0];

                                    // [MESSAGE STRUCTURE]
                                    // NewMade EntityData
                                    var netMsg = Program.Server.CreateMessage("HEADER");
                                    netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                    netMsg.Write(Constants.OUTGAME.EVENT_ON_RECEIVEENTITYDATA);
                                    objSerialized = entity.Serialize();
                                    {
                                        netMsg.Write(objSerialized.Length);
                                        netMsg.Write(objSerialized);
                                    }

                                    netMsg.Write(entity.ClassesInPossession.Count);
                                    foreach (var c in entity.ClassesInPossession)
                                    {
                                        netMsg.Write(c.Key);
                                        netMsg.Write(c.Value);
                                    }

                                    Program.Server.SendMessage(netMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                                    // Send to the gameserver'room' (if exist)
                                    // [MESSAGE STRUCTURE]
                                    // NewMade EntityData
                                    if (user.currentRoom != null)
                                    {
                                        netMsg = Program.Server.CreateMessage("HEADER");
                                        netMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                        netMsg.Write(Constants.OUTGAME.EVENT_ON_UPDATEONLINEENTITY);
                                        netMsg.Write(user.login);
                                        objSerialized = entity.Serialize();
                                        {
                                            netMsg.Write(objSerialized.Length);
                                            netMsg.Write(objSerialized);
                                        }

                                        // GameServer will not care about ArmyIndex

                                        Program.Server.SendMessage(netMsg, (NetConnection)user.currentRoom.netConnection, NetDeliveryMethod.ReliableOrdered);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case Constants.OUTGAME.GET_FINALLOADOUTOFALL:
                    {
                        if (!(message.SenderConnection.Tag is object[]))
                            return;
                        var requestType = (string)((object[])message.SenderConnection.Tag)[0];
                        if (requestType == "GAMEROOM")
                        {
                            foreach (var playerLogin in ((GameRoom)((object[])message.SenderConnection.Tag)[1]).playersIn)
                            {
                                if (string.IsNullOrEmpty(playerLogin))
                                    continue;
                                if (Global.users.TryGetValue(playerLogin, out var user))
                                {
                                    var sendMsg = Program.Server.CreateMessage("HEADER");
                                    sendMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                    sendMsg.Write(Constants.OUTGAME.EVENT_ON_RECEIVEFINALLOADOUT);

                                    var player = user.ownedPlayers[user.currentPlayerIndex];
                                    sendMsg.Write(user.login);
                                    var objSerialized = player.EntitiesInPossession.ToArray().Serialize();
                                    sendMsg.Write(objSerialized.Length);
                                    sendMsg.Write(objSerialized);

                                    Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                }
                            }
                        }

                        break;
                    }
                case Constants.OUTGAME.GO_CHANGEBASEMENT:
                    {
                        if (Global.peerGameUsers.TryGetValue(message.SenderConnection, out var user))
                        {
                            if (user.currentRoom != null)
                                return;
                            else
                            {
                                var sendMsg = Program.Server.CreateMessage("HEADER");
                                sendMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                sendMsg.Write(Constants.OUTGAME.EVENT_ON_CHANGEBASEMENT);
                                sendMsg.Write(message.ReadString());
                                Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                            }
                        }
                        break;
                    }
            }
        }
    }
}
