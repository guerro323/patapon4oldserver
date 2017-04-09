using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patapon4GameServer.Play
{
    public class EntityPlus
    {
        public PlayerManager plManager;
        public EntityUnitActor Actor;

        public virtual void Update()
        {
            // Pata Pata Pata Pon
            if (plManager.CurrentCommand == "1112")
            {
                Console.WriteLine(Actor.ServerPosition);
                Actor.ServerPosition = new UnityEngine.Vector3(Actor.ServerPosition.x + 0.025f, 2f, Actor.ServerPosition.z);
            }
        }
    }
}
