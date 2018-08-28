using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.GameObjects.Components.Mining
{
    public class SharedMineturfs : Component
    {
        public override string Name => "Mineturfs";

        public override uint? NetID => ContentNetIDs.MINE_TURFS;

        protected float Chunksize = 8; //worldspace
        protected float Voxelsize = 8; //pixels
        protected float Tilesize = 32; //pixels per worldspace

        public int VoxelsPerChunks => (int)(Chunksize * Tilesize / Voxelsize);
        public float WorldSpacePerVoxel => Voxelsize / Tilesize;
    }

    [Flags]
    public enum TurfProperties
    {
        None,
        LowResource,
        MediumResource,
        HighResource,
        Soft,
        Weak,
        Tough,
        Hard,
        Unbreakable
    }

    /// <summary>
    /// Updates the client chunk of mine turfs
    /// </summary>
    [Serializable, NetSerializable]
    public class UpdateMiningChunk : ComponentMessage
    {
        public TurfProperties[][] turfs;
        public Tuple<int, int> chunkindex;

        public UpdateMiningChunk(TurfProperties[,] chunkTurfs, Tuple<int, int> chunkIndex)
        {
            Directed = true;

            //kill me
            turfs = new TurfProperties[chunkTurfs.GetLength(0)][];
            for (var x = 0; x < chunkTurfs.GetLength(0); x++)
            {
                turfs[x] = new TurfProperties[chunkTurfs.GetLength(1)];
                for (var y = 0; y < chunkTurfs.GetLength(1); y++)
                {
                    turfs[x][y] = chunkTurfs[x, y];
                }
            }

            chunkindex = chunkIndex;
        }
    }
    
    [Serializable, NetSerializable]
    public class ChunkRequest : ComponentMessage
    {
        public ChunkRequest()
        {
            Directed = true;
        }
    }
}
