using Content.Shared.GameObjects.Components.Mining;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Mining
{
    public class Mineturfs : SharedMineturfs
    {
        private IServerTransformComponent transform;
        private BoundingBoxComponent boundingbox;

        public Dictionary<Tuple<int, int>, TurfProperties[,]> chunks = new Dictionary<Tuple<int, int>, TurfProperties[,]>();
        public HashSet<Tuple<int, int>> DirtyChunks = new HashSet<Tuple<int, int>>();

        public override void Initialize()
        {
            base.Initialize();

            transform = Owner.GetComponent<IServerTransformComponent>();
            boundingbox = Owner.GetComponent<BoundingBoxComponent>();

            var position = transform.LocalPosition;

            //Let's slide the position so that if we merge mine turf grids, they are on a universal grid already
            transform.LocalPosition = new GridLocalCoordinates(
                (float)(Math.Round(position.X / WorldSpacePerVoxel) * WorldSpacePerVoxel),
                (float)(Math.Round(position.Y / WorldSpacePerVoxel) * WorldSpacePerVoxel),
                position.GridID);
        }

        public void AddVoxels(GridLocalCoordinates coordinates, Box2 aabb, TurfProperties turf)
        {
            if (coordinates.GridID != transform.GridID)
            {
                return;
            }

            var positiondiff = coordinates.ToWorld().Position - transform.WorldPosition;
            aabb = aabb.Translated(positiondiff);

            //Go to every voxel point contained within the bounding box of the added aabb
            for (var left = aabb.Left; left <= aabb.Right; left += WorldSpacePerVoxel)
            {
                for (var top = aabb.Top; top <= aabb.Bottom; top += WorldSpacePerVoxel)
                {
                    SetVoxelPoint(left, top, turf);
                }
            }

            var mineturfaabb = boundingbox.AABB;
            var expandedaabb = new Box2(Math.Min((float)(Math.Round(aabb.Left / WorldSpacePerVoxel) * WorldSpacePerVoxel), mineturfaabb.Left),
                Math.Min((float)(Math.Round(aabb.Top / WorldSpacePerVoxel) * WorldSpacePerVoxel), mineturfaabb.Top),
                Math.Max(aabb.Right, mineturfaabb.Right),
                Math.Max(aabb.Bottom, mineturfaabb.Bottom));
            boundingbox.AABB = expandedaabb;

            SendDirtyChunks();
        }

        private void SetVoxelPoint(float x, float y, TurfProperties turf)
        {
            var chunkx = (int)Math.Floor(x / Chunksize);
            var chunky = (int)Math.Floor(y / Chunksize);

            TurfProperties[,] chunk;
            if (!chunks.TryGetValue(new Tuple<int, int>(chunkx, chunky), out chunk))
            {
                chunk = new TurfProperties[VoxelsPerChunks, VoxelsPerChunks];
                chunks.Add(new Tuple<int, int>(chunkx, chunky), chunk);
            }

            DirtyChunks.Add(new Tuple<int, int>(chunkx, chunky));

            var chunkelementx = (int)(Math.Floor((x - chunkx * Chunksize) / WorldSpacePerVoxel));
            var chunkelementy = (int)(Math.Floor((y - chunky * Chunksize) / WorldSpacePerVoxel));

            chunk[chunkelementx, chunkelementy] = turf;
        }

        public void RemoveVoxelPointUpdate(float x, float y)
        {
            if(RemoveVoxelPoint(x, y))
            {
                SendDirtyChunks();
            }
        }

        private bool RemoveVoxelPoint(float x, float y)
        {
            var chunkx = (int)Math.Floor(x / Chunksize);
            var chunky = (int)Math.Floor(y / Chunksize);

            if (chunks.TryGetValue(new Tuple<int, int>(chunkx, chunky), out TurfProperties[,] chunk))
            {
                var chunkelementx = (int)(Math.Floor((x - chunkx * Chunksize) / WorldSpacePerVoxel));
                var chunkelementy = (int)(Math.Floor((y - chunky * Chunksize) / WorldSpacePerVoxel));

                chunk[chunkelementx, chunkelementy] = TurfProperties.None;
                DirtyChunks.Add(new Tuple<int, int>(chunkx, chunky));
                return true;
            }
            return false;
        }

        private TurfProperties GetVoxelPoint(float x, float y)
        {
            var chunkx = (int)Math.Floor(x / Chunksize);
            var chunky = (int)Math.Floor(y / Chunksize);

            if (chunks.TryGetValue(new Tuple<int, int>(chunkx, chunky), out TurfProperties[,] chunk))
            {
                var chunkelementx = (int)(Math.Floor((x - chunkx * Chunksize) / WorldSpacePerVoxel));
                var chunkelementy = (int)(Math.Floor((y - chunky * Chunksize) / WorldSpacePerVoxel));

                return chunk[chunkelementx, chunkelementy];
            }
            return TurfProperties.None;
        }

        public bool AreVoxelsIntersecting(GridLocalCoordinates coordinates, Box2 aabb)
        {
            if (coordinates.GridID != transform.GridID)
            {
                return false;
            }

            var positiondiff = coordinates.ToWorld().Position - transform.WorldPosition;
            aabb = aabb.Translated(positiondiff);

            //Go to every voxel point contained within the bounding box of the aabb
            for (var left = aabb.Left; left <= aabb.Right; left += WorldSpacePerVoxel)
            {
                for (var top = aabb.Top; top <= aabb.Bottom; top += WorldSpacePerVoxel)
                {
                    if(GetVoxelPoint(left, top) != TurfProperties.None)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool BordersOrIntersectsVoxel(GridLocalCoordinates coordinates, Box2 aabb)
        {
            aabb = new Box2(aabb.TopLeft - new Vector2(WorldSpacePerVoxel, WorldSpacePerVoxel), aabb.BottomRight + new Vector2(WorldSpacePerVoxel, WorldSpacePerVoxel));
            return AreVoxelsIntersecting(coordinates, aabb);
        }

        private void SendDirtyChunks()
        {
            foreach(var chunktuple in DirtyChunks)
            {
                var message = new UpdateMiningChunk(chunks[chunktuple], chunktuple);
                SendNetworkMessage(message);
            }
        }

        private void SendAllChunks()
        {
            foreach(var chunktosend in chunks)
            {
                var message = new UpdateMiningChunk(chunktosend.Value, chunktosend.Key);
                SendNetworkMessage(message);
            }
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case ChunkRequest msg:
                    SendAllChunks();
                    break;
            }
        }
    }
}
