using RespawnGear.EntityBehaviors;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace RespawnGear.ItemClasses
{
    public class ItemRespawnGear : Item
    {
        public static readonly string ITEM_ID = "respawngear";
        public static float secondsSince;

        public SimpleParticleProperties? particlesHeld;

        /// <inheritdoc cref="CollectibleObject.OnLoaded(ICoreAPI)"/>
        /// <remarks> Loads particles when item is being held </remarks>
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            particlesHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.1f, -0.1f, -0.1f), new Vec3f(0.1f, 0.1f, 0.1f), 1.5f, 0f, 0.5f, 0.75f)
            {
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f)
            };
            particlesHeld.AddPos.Set(0.10000000149011612, 0.10000000149011612, 0.10000000149011612);
            particlesHeld.addLifeLength = 0.5f;
            particlesHeld.RandomVelocityChange = true;
        }

        /// <inheritdoc cref="CollectibleObject.InGuiIdle(IWorldAccessor, ItemStack)"/>
        /// <remarks> Gives the item the typical temp gear rotation when its on the GUI </remarks>
        public override void InGuiIdle(IWorldAccessor world, ItemStack stack)
        {
            if (world is IClientWorldAccessor)
            {
                GuiTransform.Rotation.Y = GameMath.Mod(world.ElapsedMilliseconds / 25f, 360f);
                GuiTransform.Translation.Y = (float) Math.Sin(world.ElapsedMilliseconds / 500f);
            }
        }

        /// <inheritdoc cref="CollectibleObject.OnGroundIdle(EntityItem)"/>
        /// <remarks> Gives the item the typical temp gear rotation on the ground </remarks>
        public override void OnGroundIdle(EntityItem entityItem)
        {
            if (entityItem.World is IClientWorldAccessor)
            {
                GroundTransform.Rotation.Y = 0f - GameMath.Mod(entityItem.World.ElapsedMilliseconds / 25f, 360f);
                GroundTransform.Translation.Y = MathF.Sin(entityItem.World.ElapsedMilliseconds / 500f) * 0.1f + 0.1f;
                if (particlesHeld == null) return;
                Vec3d xYZ = entityItem.SidedPos.XYZ;
                particlesHeld.MinQuantity = 1f;
                SpawnParticles(entityItem.World, xYZ, final: false);
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
                // No need to change the vertical translation here, the seraph is holding it in place.
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity is not EntityPlayer) return;
            if (byEntity.World.Side == EnumAppSide.Client &&
                byEntity.World.Calendar.ElapsedSeconds - secondsSince < 60)
            {
                return;
            }
            if (byEntity.World is IClientWorldAccessor clientWorldAccessor)
            {
                ILoadedSound sound;
                byEntity.World.Api.ObjectCache["temporalGearSound"] = sound = clientWorldAccessor.LoadSound(new SoundParams
                {
                    Location = new AssetLocation("sounds/effect/gears.ogg"),
                    ShouldLoop = true,
                    Position = blockSel.Position.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1f,
                    Pitch = 0.9f
                });
                sound?.Start();
                byEntity.World.RegisterCallback(delegate
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.None)
                    {
                        sound?.Stop();
                        sound?.Dispose();
                        
                    }
                }, 3600);
                byEntity.World.RegisterCallback(delegate
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.None)
                    {
                        sound?.Stop();
                        sound?.Dispose();
                    }
                }, 20);
            }
            handHandling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null || byEntity is not EntityPlayer)
            {
                return false;
            }

            if (byEntity.World is IClientWorldAccessor accessor)
            {
                FpHandTransform.Rotation.Y = GameMath.Mod(byEntity.World.ElapsedMilliseconds / (25f - secondsUsed * 20f), 360f);
                TpHandTransform.Rotation.Y = GameMath.Mod(byEntity.World.ElapsedMilliseconds / (25f - secondsUsed * 20f), 360f);
                if (particlesHeld != null)
                {
                    particlesHeld.MinQuantity = 1f;
                    Vec3d pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
                    SpawnParticles(byEntity.World, pos, final: false);
                }
                accessor.AddCameraShake(0.035f);
                ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound")?.SetPitch(0.8f + secondsUsed / 4f);
            }

            return (double) secondsUsed < 2.25f;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                ILoadedSound loadedSound = ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound");
                loadedSound?.Stop();
                loadedSound?.Dispose();
            }

            if (blockSel != null && secondsUsed > 2f)
            {
                if (byEntity.World.Side == EnumAppSide.Client)
                {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/portal.ogg"), byEntity, null, randomizePitch: true);
                    if (particlesHeld != null)
                    {
                        particlesHeld.MinSize = 0.25f;
                        particlesHeld.MaxSize = 0.5f;
                        particlesHeld.MinQuantity = 300f;
                        Vec3d pos = blockSel.Position.ToVec3d().Add(blockSel.HitPosition);
                        SpawnParticles(byEntity.World, pos, final: true);
                    }
                }

                if (byEntity.World.Side == EnumAppSide.Server && byEntity is EntityPlayer player)
                {
                    // Here is where the magic happens
                    EntityBehaviorRespawnable? respawnable = 
                        byEntity.GetBehavior<EntityBehaviorRespawnable>()
                        ?? throw new Exception("The respawnable behavior is not present on the player");
                    respawnable.PosX = (float) byEntity.ServerPos.X;
                    respawnable.PosY = (float) byEntity.ServerPos.Y;
                    respawnable.PosZ = (float) byEntity.ServerPos.Z;
                    respawnable.Yaw = byEntity.ServerPos.Yaw;
                    respawnable.Pitch = byEntity.ServerPos.Pitch;
                    respawnable.CalculateTimestampAndCharges();
                    ICoreServerAPI serverAPI = (ICoreServerAPI) byEntity.Api;
                    serverAPI.SendMessage( // TODO: This message is hardcoded, put into lang
                        player.Player, 0,
                        $"You have {respawnable.Charges} charges left, they will continue to recharge over time.",
                        EnumChatType.Notification
                    );
                } else if (byEntity.World.Side == EnumAppSide.Client)
                {
                    secondsSince = byEntity.World.Calendar.ElapsedSeconds;
                }
            }
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                ILoadedSound loadedSound = ObjectCacheUtil.TryGet<ILoadedSound>(api, "temporalGearSound");
                loadedSound?.Stop();
                loadedSound?.Dispose();
            }
            return true;
        }

        private void SpawnParticles(IWorldAccessor world, Vec3d pos, bool final)
        {
            if ((final || world.Rand.NextDouble() > 0.8) && particlesHeld != null)
            {
                int h = 342 + world.Rand.Next(15);
                int v = 76 + world.Rand.Next(50);
                particlesHeld.MinPos = pos;
                if (final)
                {
                    particlesHeld.MinPos.X += ((world.Rand.NextDouble() - 0.5) * 0.5);
                    particlesHeld.MinPos.Z += ((world.Rand.NextDouble() - 0.5) * 0.5);
                }
                particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v));
                particlesHeld.MinSize = 0.2f;
                particlesHeld.ParticleModel = EnumParticleModel.Cube;
                particlesHeld.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
                particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v, 150));
                world.SpawnParticles(particlesHeld);
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[1] {
                new() {
                    ActionLangCode = "heldhelp-useonground",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
