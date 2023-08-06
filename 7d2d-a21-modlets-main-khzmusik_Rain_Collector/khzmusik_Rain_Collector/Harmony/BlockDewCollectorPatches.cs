using HarmonyLib;

namespace RainCollector.Harmony
{
    /// <summary>
    /// Harmony patches for <see cref="BlockDewCollector"/>.
    /// </summary>
    public class BlockDewCollectorPatches
    {
        /// <summary>
        /// Property name to determine if the dew collector should be a chunk observer.
        /// </summary>
        public const string PropIsChunkObserver = "IsChunkObserver";

        private static bool enabled = false;
        private static bool initialized = false;

        /// <summary>
        /// Patch to
        /// <see cref="BlockDewCollector.addTileEntity(WorldBase, Chunk, Vector3i, BlockValue)"/>
        /// to add a chunk observer as the tile entity is being added.
        /// </summary>
        [HarmonyPatch(typeof(BlockDewCollector), "addTileEntity")]
        public class AddTileEntity
        {
            /// <summary>
            /// Harmony postfix for
            /// <see cref="BlockDewCollector.addTileEntity(WorldBase, Chunk, Vector3i, BlockValue)"/>.
            /// </summary>
            /// <param name="world"></param>
            /// <param name="_blockPos"></param>
            /// <param name="__instance"></param>
            public static void Postfix(
                WorldBase world,
                Vector3i _blockPos,
                BlockDewCollector __instance)
            {
                if (!initialized)
                {
                    Initialize(__instance);
                }

                if (!enabled)
                {
                    return;
                }

                AddChunkObserver(world, _blockPos);
            }
        }

        /// <summary>
        /// Patch to
        /// <see cref="BlockDewCollector.removeTileEntity(WorldBase, Chunk, Vector3i, BlockValue)"/>
        /// to remove the chunk observer added when the tile entity was added.
        /// </summary>
        [HarmonyPatch(typeof(BlockDewCollector), "removeTileEntity")]
        public class RemoveTileEntity
        {
            /// <summary>
            /// Harmony postfix for
            /// <see cref="BlockDewCollector.removeTileEntity(WorldBase, Chunk, Vector3i, BlockValue)"/>.
            /// </summary>
            /// <param name="world"></param>
            /// <param name="_blockPos"></param>
            /// <param name="__instance"></param>
            public static void Postfix(
                WorldBase world,
                Vector3i _blockPos,
                BlockDewCollector __instance)
            {
                if (!initialized)
                {
                    Initialize(__instance);
                }

                if (!enabled)
                {
                    return;
                }

                RemoveChunkObserver(world, _blockPos);
            }
        }

        /// <summary>
        /// <para>
        /// Patch to
        /// <see cref="Block.OnBlockLoaded(WorldBase, int, Vector3i, BlockValue)"/>
        /// to ensure a chunk observer exists when the block is loaded.
        /// </para>
        /// 
        /// <para>
        /// This would patch <see cref="BlockDewCollector"/> but it does not override this method.
        /// </para>
        /// </summary>
        [HarmonyPatch(typeof(Block), "OnBlockLoaded")]
        public class OnBlockLoaded
        {
            /// <summary>
            /// Harmony postfix for
            /// <see cref="Block.OnBlockLoaded(WorldBase, int, Vector3i, BlockValue)"/>.
            /// </summary>
            /// <param name="_world"></param>
            /// <param name="_clrIdx"></param>
            /// <param name="_blockPos"></param>
            /// <param name="_blockValue"></param>
            /// <param name="__instance"></param>
            public static void Postfix(
                WorldBase _world,
                int _clrIdx,
                Vector3i _blockPos,
                BlockValue _blockValue,
                Block __instance)
            {
                // We don't want to check child blocks at all - the dew collector is 3x3x3, so this
                // method will be called 27 times, 26 of which will be on child blocks.
                if (_blockValue.ischild)
                {
                    return;
                }

                if (!(__instance is BlockDewCollector dewCollector))
                {
                    return;
                }

                if (!initialized)
                {
                    Initialize(dewCollector);
                }

                if (!enabled)
                {
                    return;
                }

                EnsureChunkObserverExists(_world, _clrIdx, _blockPos);
            }
        }

        private static void AddChunkObserver(WorldBase world, Vector3i blockPos)
        {
            var observer = world.GetGameManager().AddChunkObserver(
                blockPos.ToVector3(),
                false,
                3,
                -1);

            RainCollector.DebugLog($"Added observer {observer} at {blockPos.ToVector3()}");
        }

        private static void EnsureChunkObserverExists(
            WorldBase world,
            int clrIdx,
            Vector3i blockPos)
        {
            // Double-check that the tile entity wasn't removed
            if (!(world.GetTileEntity(clrIdx, blockPos) is TileEntityDewCollector))
            {
                return;
            }

            var observer = FindChunkObserver(world, blockPos);

            if (observer == null)
            {
                AddChunkObserver(world, blockPos);
            }
        }

        private static ChunkManager.ChunkObserver FindChunkObserver(
            WorldBase world,
            Vector3i blockPos)
        {
            if (!(world is World w))
            {
                return null;
            }

            var observedEntities = w.m_ChunkManager.m_ObservedEntities;

            var position = blockPos.ToVector3();

            for (int i = 0; i < observedEntities.Count; i++)
            {
                if (observedEntities[i].position == position)
                {
                    return observedEntities[i];
                }
            }

            return null;
        }

        private static void Initialize(BlockDewCollector block)
        {
            enabled = TileEntityDewCollectorPatches.HandleUpdate.Initialize(block);

            if (enabled)
            {
                // These patches are only enabled if the dew collector should be a chunk observer
                if (block.Properties.Contains(PropIsChunkObserver))
                {
                    block.Properties.ParseBool(PropIsChunkObserver, ref enabled);
                }
                else
                {
                    enabled = false;
                }
                // We want to print this message even if we're not debugging
                Log.Out($"RainCollector: Chunk observers are {(enabled ? "" : "NOT ")}enabled");
            }

            initialized = true;
        }

        private static void RemoveChunkObserver(WorldBase world, Vector3i blockPos)
        {
            var observer = FindChunkObserver(world, blockPos);

            if (observer != null)
            {
                world.GetGameManager().RemoveChunkObserver(observer);
                RainCollector.DebugLog($"Removed observer {observer} at {blockPos.ToVector3()}");
            }
        }
    }
}
