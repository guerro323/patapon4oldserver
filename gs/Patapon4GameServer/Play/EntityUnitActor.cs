using FarseerPhysics.Dynamics;
using PataponPhotonShared.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityActor;

namespace Patapon4GameServer.Play
{
    public class EntityUnitActor : EntityBaseActor
    {
        [ActorVariable]
        public Body PhysicBody { get; set; }
        [ActorVariable]
        public EntityPlus EntityPlus { get; set; }

        public override void Update()
        {
            base.Update();
            EntityPlus.Update();
        }
    }
}
