using CSScriptLibrary;
using Patapon4GameServer.Core.Assets;
using Patapon4GameServer.Play;
using PataponPhotonShared;
using PataponPhotonShared.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static PataponPhotonShared.GameMap;

namespace Patapon4GameServer.Core
{
	public class MissionManager
	{

        public static Dictionary<string, GameMapInfo> DefaultMapsInfo;
        public static Dictionary<string, EntityAssetInfo> DefaultEntitiyAssets;

        public static GameMapInfo MissionToPlay;

        public static GameMap CurrentMission;
        public static MapMission MapMission;

        public static async void ForceLoad()
        {
            GameServer.Log("Loading: " + MissionToPlay.Dependencies.Count);

            CurrentMission = new GameMap()
            {
                Events = new List<MissionEvent>(),
                Info = MissionToPlay,
                Structure = new MissionStructure(),
                Visual = new MissionVisual()
            };

            // Build dependencies
            List<string> files = new List<string>();
            foreach (var dependency in CurrentMission.Info.Dependencies)
            {
                if (dependency.Value.type == "map_server")
                {
                    files.Add(dependency.Value.path);
                }
            }

            if (File.Exists(Environment.CurrentDirectory + "/" + CurrentMission.Name + ".p4map"))
            {
                File.SetAttributes(Environment.CurrentDirectory + "/" + CurrentMission.Name + ".p4map", FileAttributes.Normal);
            }

            var assembly = Assembly.LoadFile(CSScript.CompileFiles(files.ToArray(), Environment.CurrentDirectory + "/" + CurrentMission.Name + ".p4map", false));
            MapMission = (MapMission)assembly.CreateInstance(assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MapMission))).First().FullName);


            foreach (var player in GameServer.gameServer.playersInRoom.Values)
            {
                player.GetManager().Net_CurrentSyncState = EGameState.GameMissionSyncing;
            }

            GameServer.gameServer.currentGameState = EGameState.LoadingMission;
            await PlayerManager.GetFinalLoadoutOfAll();
            await MapMission.InitialStart();
            GameServer.gameServer.currentGameState = EGameState.GameMissionSyncing;
            await Task.Factory.StartNew(() =>
            {
                while (GameServer.gameServer.playersInRoom.Select(p => p.Value.GetManager()).Where(m => m.Net_CurrentSyncState != EGameState.Syncing).Count() > 0)
                {
                    foreach (var p in GameServer.gameServer.playersInRoom)
                    {
                        Console.WriteLine(p.Value.User.login + " : " + p.Value.GetManager().Net_CurrentSyncState);
                    }
                }
            });
            await MapMission.Preparation();
            await MapMission.Arena.SyncPlayers();
            await Task.Factory.StartNew(() =>
            {
                while (GameServer.gameServer.playersInRoom.Select(p => p.Value.GetManager()).Where(m => m.Net_CurrentSyncState != EGameState.Mission).Count() > 0) ;
            });
            MapMission.Arena.StartEvent(Arena.StartEventType.SyncFinished);
        }
	}
}
