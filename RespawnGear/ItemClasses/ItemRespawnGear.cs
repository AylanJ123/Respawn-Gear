using RespawnGear.EntityBehaviors;
using RespawnGear.Misc;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RespawnGear.ItemClasses
{
    public class ItemRespawnGear : Item
    {
        public static readonly string ITEM_ID = "respawngear";

        private SimpleParticleProperties? particles;
        private float lastProcedTimestamp;
        SimpleParticleProperties Particles => particles ?? throw new Exception("Item particles not defined");

        /// <inheritdoc cref="CollectibleObject.OnLoaded(ICoreAPI)"/>
        /// <remarks> Loads particles when item is being held </remarks>
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            particles = new(
                1, 3,
                ColorUtil.ToRgba(50, 220, 220, 220),
                new Vec3d(),
                new Vec3d(),
                new Vec3f(-0.5f, -0.5f, -0.5f),
                new Vec3f(0.5f, 0.5f, 0.5f),
                1.5f,
                0,
                0.5f,
                0.75f,
                EnumParticleModel.Cube
            )
            {
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f)
            };
            Particles.AddPos.Set(0.1f, 0.1f, 0.1f);
            Particles.addLifeLength = 0.5f;
            Particles.RandomVelocityChange = true;
        }

        /// <inheritdoc cref="CollectibleObject.InGuiIdle(IWorldAccessor, ItemStack)"/>
        /// <remarks> Gives the item the typical temp gear rotation when its on the GUI </remarks>
        public override void InGuiIdle(IWorldAccessor world, ItemStack stack)
        {
            if (world is IClientWorldAccessor)
            {
                GuiTransform.Rotation.Y = GameMath.Mod(world.ElapsedMilliseconds / 25f, 360f);
            }
        }

        /// <inheritdoc cref="CollectibleObject.OnGroundIdle(EntityItem)"/>
        /// <remarks> Gives the item the typical temp gear rotation on the ground </remarks>
        public override void OnGroundIdle(EntityItem entityItem)
        {
            if (entityItem.World is IClientWorldAccessor)
            {
                GroundTransform.Rotation.Y = GameMath.Mod(entityItem.World.ElapsedMilliseconds / 25f, 360f);

                Particles.MinQuantity = 1f;

                SpawnParticles(entityItem.World, entityItem.Pos.XYZ, final: false);
            }
        }

        /// <inheritdoc cref="CollectibleObject.OnHeldIdle(ItemSlot, EntityAgent)"/>
        /// <remarks> Gives the item the typical temp gear rotation BUT this time won't affect translation </remarks>
        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity.World is IClientWorldAccessor)
            {
                FpHandTransform.Rotation.Y = GameMath.Mod((-byEntity.World.ElapsedMilliseconds) / 25f, 360f);
                TpHandTransform.Rotation.Y = GameMath.Mod((-byEntity.World.ElapsedMilliseconds) / 25f, 360f);
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (byEntity.World.Calendar.ElapsedSeconds - lastProcedTimestamp < 60) return;
            if (byEntity.World is IClientWorldAccessor clientWorldAccessor)
            {
                ILoadedSound sound;
                byEntity.World.Api.ObjectCache["temporalGearSound"] = sound = clientWorldAccessor.LoadSound(new SoundParams
                {
                    Location = new AssetLocation("sounds/effect/gears.ogg"),
                    ShouldLoop = true,
                    //Fixes the crash occuring if you don't have a block selected when starting to roll the gear
                    Position = byEntity.Pos.AsBlockPos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1f,
                    Pitch = 0.9f
                });
                sound?.Start();
                void StopSound(float ms)
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.None)
                    {
                        sound?.Stop();
                        sound?.Dispose();
                    }
                }
                byEntity.World.RegisterCallback(StopSound, 20);
                byEntity.World.RegisterCallback(StopSound, 360);
            }
            handHandling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity is not EntityPlayer) return false;
            if (byEntity.World.Calendar.ElapsedSeconds - lastProcedTimestamp < 60) return false;

            bool proc = secondsUsed > 3.25f;

            if (byEntity.World is IClientWorldAccessor accessor)
            {
                FpHandTransform.Rotation.Y = GameMath.Mod(byEntity.World.ElapsedMilliseconds / (25f - secondsUsed * 20f), 360f);
                TpHandTransform.Rotation.Y = GameMath.Mod(byEntity.World.ElapsedMilliseconds / (25f - secondsUsed * 20f), 360f);

                if (proc)
                {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/portal.ogg"), byEntity, null, randomizePitch: true);
                }

                accessor.AddCameraShake(0.035f);
                SpawnParticles(byEntity.World, byEntity.Pos.XYZ, final: false);

                ILoadedSound sound = ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound");
                sound?.SetPitch(0.8f + secondsUsed / 2);
                sound?.SetPosition(byEntity.Pos.XYZFloat); //We want the sound to follow us
            }
            if (proc)
            {
                EntityPlayer player = (EntityPlayer) byEntity;
                if (byEntity.World.Side == EnumAppSide.Server)
                {
                    SpawnParticles(byEntity.World, byEntity.Pos.XYZ, final: true);
                    EntityBehaviorRespawnable? behavior = byEntity.GetBehavior<EntityBehaviorRespawnable>();
                    behavior?.SetSpawnPosition(byEntity.Pos.AsBlockPos.ToVec3i(), byEntity.Pos.Yaw, byEntity.Pos.Pitch);
                    ModHelper.ServerHelper.Message(player.Player, $"You have {behavior?.Charges} charges left, they will continue to recharge over time.");
                    return false;
                }
                else
                {
                    lastProcedTimestamp = player.World.Calendar.ElapsedSeconds;
                }
            }
            
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                ILoadedSound loadedSound = ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound");
                loadedSound?.Stop();
                loadedSound?.Dispose();
            }
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                ILoadedSound? loadedSound = ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound");
                loadedSound?.Stop();
                loadedSound?.Dispose();
            }
            return true;
        }

        private void SpawnParticles(IWorldAccessor world, Vec3d pos, bool final)
        {
            if ((final || world.Rand.NextDouble() > 0.5) && Particles != null)
            {
                int h = 342 + world.Rand.Next(15);
                int v = 76 + world.Rand.Next(50);
                Particles.MinQuantity = final ? 120 : 3;
                Particles.MinPos = pos;
                Particles.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v));
                Particles.MinSize = 0.8f;
                Particles.ParticleModel = EnumParticleModel.Cube;
                Particles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
                Particles.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v, 150));
                world.SpawnParticles(Particles);
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new()
                {
                    ActionLangCode = "heldhelp-useonground",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}