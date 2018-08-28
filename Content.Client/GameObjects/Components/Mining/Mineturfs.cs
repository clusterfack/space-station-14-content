using Content.Shared.GameObjects.Components.Mining;
using SS14.Client.GameObjects;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace Content.Client.GameObjects.Components.Mining
{
    public class Mineturfs : SharedMineturfs
    {
        private TileMap tilemap;

        public override void Initialize()
        {
            base.Initialize();

            tilemap = new TileMap(new Vector2(Voxelsize, Voxelsize), 10, 4);
            tilemap.AddTileType("asteroid", "asteroid");
            Owner.GetComponent<GodotTransformComponent>().GodotNode.AddChild(tilemap);

            SendNetworkMessage(new ChunkRequest());
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case UpdateMiningChunk msg:
                    UpdateChunk(msg);
                    break;
            }
        }

        private void UpdateChunk(UpdateMiningChunk msg)
        {
            var cellstartx = msg.chunkindex.Item1 * VoxelsPerChunks;
            var cellstarty = msg.chunkindex.Item2 * VoxelsPerChunks;

            var cellendx = cellstartx + VoxelsPerChunks;
            var cellendy = cellstarty + VoxelsPerChunks;

            for(var x = 0; x < VoxelsPerChunks; x++)
            {
                for(var y = 0; y < VoxelsPerChunks; y++)
                {
                    var turf = msg.turfs[x][y];
                    if(turf != TurfProperties.None)
                    {
                        tilemap.SetCell(cellstartx + x, cellstarty + y, "asteroid");
                    }
                    else
                    {
                        tilemap.ClearCell(cellstartx + x, cellstarty + y);
                    }
                }
            }
        }
    }
}
