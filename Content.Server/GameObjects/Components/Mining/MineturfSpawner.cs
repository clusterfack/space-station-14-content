using Content.Shared.GameObjects.Components.Mining;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Mining
{
    public class MineturfSpawner : Component
    {
        public override string Name => "MineturfSpawner";

        private TurfProperties TurfFlags = TurfProperties.LowResource;

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            List<string> slots = new List<string>();

            serializer.DataField(ref slots, "turfs", new List<string>(0));

            foreach (var slotflagsloaded in slots)
            {
                TurfFlags |= (TurfProperties)Enum.Parse(typeof(TurfProperties), slotflagsloaded.ToUpper());
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var position = Owner.GetComponent<IServerTransformComponent>().LocalPosition;
            var aabb = Owner.GetComponent<BoundingBoxComponent>().AABB;
            var emanager = IoCManager.Resolve<IServerEntityManager>();

            var intersectingentities = emanager.GetEntitiesIntersecting(position);
            intersectingentities = intersectingentities.Where(x => x.HasComponent<Mineturfs>());

            if(intersectingentities.FirstOrDefault() != null)
            {
                Mineturfs lastmergedmineturf = null;
                foreach (var entity in intersectingentities)
                {
                    var mineturf = entity.GetComponent<Mineturfs>();

                    //We only want to act if we could be the join point for multiple mineturfs
                    if (mineturf.BordersOrIntersectsVoxel(position, aabb))
                    {
                        if(lastmergedmineturf == null)
                        {
                            mineturf.AddVoxels(position, aabb, TurfFlags);
                            lastmergedmineturf = mineturf;
                        }
                        else
                        {
                            //mineturf.Merge(lastmergedmineturf);
                            lastmergedmineturf = mineturf;
                        }
                    }

                    //TODO: merge mine turfs that connect???
                }
            }
            else
            {
                var mineturf = emanager.ForceSpawnEntityAt("mineturfentity", position);
                mineturf.GetComponent<Mineturfs>().AddVoxels(position, aabb, TurfFlags);
            }

            Owner.Delete();
        }
    }
}
