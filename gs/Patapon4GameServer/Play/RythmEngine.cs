using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Patapon4GameServer.Play
{
    public class RythmEngine
    {
        public event Action OnNewBeat;
        public int CurrentBeat { get; internal set; }

        internal Timer Timer;

        public void Start()
        {
            Timer = new Timer();
            Timer.Elapsed += Timer_Elapsed;
            Timer.Interval = 500;
            Timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CurrentBeat++;
            foreach (var playerManager in GameServer.gameServer.playersInRoom.Select(p => p.Value.GetManager()))
            {
                if (string.IsNullOrEmpty(playerManager.CurrentCommand))
                    continue; ;

                if (playerManager.CommandStartBeat + 3 < CurrentBeat
                    && !playerManager.CurrentCommand.StartsWith("_"))
                    playerManager.CurrentCommand = string.Empty;
                else if (playerManager.CurrentCommand.StartsWith("_"))
                {
                    playerManager.CurrentCommand = playerManager.CurrentCommand.Remove(0, 1);
                    playerManager.CommandStartBeat = CurrentBeat;
                }
            }
            OnNewBeat?.Invoke();
        }
    }
}
