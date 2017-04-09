using Patapon4GameServer.Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Patapon4GameServer.Play.GUIInterface;
using UnityActor;
using PataponPhotonShared.Entity;
using PataponPhotonShared.Helper;
using Patapon4GameServer.MSGSerializer;
using Patapon4GameServer;
using Patapon4GameServer.Extension;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Factories;
using Patapon4GameServer.Core.Network;

namespace Patapon4DefaultPack.Scripts.Arenas
{
    public class ArenaRace_Server : Arena
    {
        protected override ArenaMode s__Mode => ArenaMode.NormalRace;

        public ArenaRace_Server() : base()
        {
            Arena.Interface.Type = GUIType.Normal;
            Arena.Interface.Mode = GUIMode.NormalRace;

            World = new World(new Microsoft.Xna.Framework.Vector2(0, -9));

            var edgeBody = new Body(World);
            FixtureFactory.AttachEdge(new Microsoft.Xna.Framework.Vector2(-50, 0), new Microsoft.Xna.Framework.Vector2(50, 0), edgeBody);
        }

        public override void StartEvent(StartEventType e)
        {
            if (e == StartEventType.Loaded)
            {
                GameServer.gameServer.WantedScheme.Build();

                // Load player entities configuration
                foreach (var pm in GameServer.gameServer.playersInRoom.Select(p => p.Value.GetManager()))
                {
                    EntityData entityData = null;

                    pm.Army = tributeSystem.CreateActor<TributeArmyActor>(ActorConstants.ArmyOf + "|" + pm.Player.User.login, true);
                    if (GameServer.gameServer.WantedScheme.GetEntity(ActorConstants.ACScheme.UberHero) != null)
                    {
                        pm.UberHeroActor = entitySystem.CreateActor<EntityUnitActor>(ActorConstants.ArmyOf + "|" + pm.Player.User.login, true);
                        entityData = PlayerManager.LoadoutOfAll[pm.Player].Where(ed => ed.SchemeID == "#uberheroarmy[uberhero,0]").First();
                    }

                    pm.UberHeroActor.Guid = ArenaExt.FindFreeGUID(entitySystem);
                    pm.UberHeroActor.Data = entityData;
                    pm.UberHeroActor.Tribute = pm.Army;
                    pm.UberHeroActor.EntityPlus = new EntityPlus();
                    pm.UberHeroActor.EntityPlus.Actor = pm.UberHeroActor;
                    pm.UberHeroActor.EntityPlus.plManager = pm;
                    pm.UberHeroActor.PhysicBody = new Body(World, new Microsoft.Xna.Framework.Vector2(0, 0.4377073f), 0, pm.UberHeroActor);
                    var fix = FixtureFactory.AttachRectangle(1.024947f, 1.70071f, 1, new Microsoft.Xna.Framework.Vector2(0, 0.4377073f), pm.UberHeroActor.PhysicBody, pm.UberHeroActor);

                    var sendMsg = CommunicateActorToClients(pm.UberHeroActor, pm.Player, false, Constants.EventOperation.Arena_SpawnAndSetEquipment);
                    // Send data
                    {
                        sendMsg.Write(entityData.SchemeID);
                        sendMsg.Write((byte)entityData.Type);
                        sendMsg.Write(entityData.ArmyIndex);
                        sendMsg.Write(entityData.CurrentClass);
                        sendMsg.Write(entityData.CurrentRarepon);
                        sendMsg.Write(entityData.ClassesInPossession[entityData.CurrentClass]);
                        if (entityData.CurrentRarepon != "none")
                        {
                            if (entityData.CurrentRarepon != "")
                            {
                                sendMsg.Write(entityData.RareponsInPossession[entityData.CurrentRarepon]);
                            }
                        }
                    }
                    GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), Lidgren.Network.NetDeliveryMethod.ReliableOrdered, 0);

                }
            }
            if (e == StartEventType.SyncFinished)
            {
                //ArenaExt.UISendMessage(null, "Mission start!", PataponPhotonShared.Enums.UIMessageAlignement.VSBottom, 10);
                GameServer.RythmEngine.Start();
            }
        }

        public override async Task SyncPlayers()
        {
            var engineStartTime = DateTime.UtcNow.AddSeconds(6);

            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.BeatSync);
            sendMsg.Write(DateTime.UtcNow.Ticks);
            sendMsg.Write(engineStartTime.Ticks);

            GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), Lidgren.Network.NetDeliveryMethod.ReliableOrdered, 0);
        }

        public override void Loop()
        {
            World.Step(1 / 20f);
            foreach (EntityUnitActor actor in entitySystem.Actors.values.Values)
            {
                if (actor.Position != actor.ServerPosition)
                {
                    actor.Position = actor.ServerPosition;

                    var sendMsg = CreateMessage(Constants.EventOperation.Arena_SendEntityPositionUpdate);
                    sendMsg.Write(actor.Guid);
                    sendMsg.Write(actor.Position.x);
                    sendMsg.Write(actor.Position.y);
                    sendMsg.Write(actor.Position.z);
                    SendMessageToAll(sendMsg, Lidgren.Network.NetDeliveryMethod.Unreliable);

                    Console.WriteLine("Sending new pos!");
                }
            }
        }
    }
}
