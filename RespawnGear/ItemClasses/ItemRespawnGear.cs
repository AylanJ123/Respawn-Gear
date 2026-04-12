using RespawnGear.EntityBehaviors;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace RespawnGear.ItemClasses
{
    public class ItemRespawnGear : Item
    {
        #region Fields

        public static readonly string ITEM_ID = "respawngear";
        private static readonly string SoundCacheKey = "temporalGearSound";

        private float secondsSince;
        private SimpleParticleProperties? particlesHeld;

        #endregion

        #region Initialization

        /// <inheritdoc cref="CollectibleObject.OnLoaded(ICoreAPI)"/>
        /// <remarks> Loads particles when the item finally loads </remarks>
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side == EnumAppSide.Client) secondsSince = api.World.Calendar.ElapsedSeconds;

            particlesHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.1f, -0.1f, -0.1f), new Vec3f(0.1f, 0.1f, 0.1f), 1.5f, 0f, 0.5f, 0.75f)
            {
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f)
            };
            particlesHeld.AddPos.Set(0.10000000149011612, 0.10000000149011612, 0.10000000149011612);
            particlesHeld.addLifeLength = 0.5f;
            particlesHeld.RandomVelocityChange = true;
        }

        #endregion

        #region Idle Transforms

        /// <inheritdoc cref="CollectibleObject.InGuiIdle(IWorldAccessor, ItemStack)"/>
        /// <remarks> Gives the item the typical temp gear rotation when its on the GUI </remarks>
        public override void InGuiIdle(IWorldAccessor world, ItemStack stack)
        {
            if (world is IClientWorldAccessor)
            {
                GuiTransform.Rotation.Y = GameMath.Mod(world.ElapsedMilliseconds / 25f, 360f);
                GuiTransform.Translation.Y = (float)Math.Sin(world.ElapsedMilliseconds / 500f);
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
                SpawnParticles(entityItem.World, entityItem.SidedPos.XYZ, final: false);
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

        #endregion

        #region Interaction

        /// <inheritdoc cref="CollectibleObject.OnHeldInteractStart"/>
        /// <remarks>
        /// Starts the interaction by loading and playing the gear sound, then registering
        /// safety callbacks to stop it if the player releases early.
        /// Bails early on the client if the item is still on cooldown.
        /// </remarks>
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity is not EntityPlayer) return;
            if (byEntity.World.Side == EnumAppSide.Client &&
                byEntity.World.Calendar.ElapsedSeconds - secondsSince < 60)
            {
                return;
            }
            StartSounds(byEntity, blockSel);
            handHandling = EnumHandHandling.PreventDefault;
        }

        /// <inheritdoc cref="CollectibleObject.OnHeldInteractStep"/>
        /// <remarks>
        /// Each tick of the interaction: spins the gear faster as <paramref name="secondsUsed"/> increases,
        /// spawns particles at the targeted block, shakes the camera, and raises the sound pitch.
        /// Returns false (ending the interaction) once the full charge time has elapsed.
        /// </remarks>
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
                    SpawnParticles(byEntity.World, blockSel.Position.ToVec3d().Add(blockSel.HitPosition), final: false);
                }
                accessor.AddCameraShake(0.045f);
                ObjectCacheUtil.TryGet<ILoadedSound>(api, SoundCacheKey)?.SetPitch(0.8f + secondsUsed / 4f);
            }

            return secondsUsed < 2.25f;
        }

        /// <inheritdoc cref="CollectibleObject.OnHeldInteractStop"/>
        /// <remarks>
        /// Stops the gear sound, then, if the interaction was held long enough, delegates
        /// to <see cref="HandleClientInteractStop"/> or <see cref="HandleServerInteractStop"/>
        /// depending on the current side.
        /// </remarks>
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World.Side == EnumAppSide.Client) TryStopSound();

            if (blockSel == null || secondsUsed < 2f) return;

            if (byEntity.World.Side == EnumAppSide.Client)
                HandleClientInteractStop(byEntity, blockSel);
            else if (byEntity.World.Side == EnumAppSide.Server && byEntity is EntityPlayer player)
                HandleServerInteractStop(player, blockSel);
        }

        /// <inheritdoc cref="CollectibleObject.OnHeldInteractCancel"/>
        /// <remarks> Stops the gear sound if the player cancels the interaction mid-charge. </remarks>
        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            if (byEntity.World.Side == EnumAppSide.Client) TryStopSound();
            return true;
        }

        #endregion

        #region Interaction Helpers

        /// <summary>
        /// Handles the client side of a successful interact stop: plays the portal sound,
        /// bursts a large quantity of particles at the target, and resets the cooldown timer.
        /// </summary>
        private void HandleClientInteractStop(EntityAgent byEntity, BlockSelection blockSel)
        {
            byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/portal.ogg"), byEntity, null, randomizePitch: true);
            if (particlesHeld != null)
            {
                particlesHeld.MinSize = 0.25f;
                particlesHeld.MaxSize = 0.5f;
                particlesHeld.MinQuantity = 300f;
                SpawnParticles(byEntity.World, blockSel.Position.ToVec3d().Add(blockSel.HitPosition), final: true);
            }
            secondsSince = byEntity.World.Calendar.ElapsedSeconds;
        }

        /// <summary>
        /// Handles the server side of a successful interact stop: sets the player's new spawn point
        /// via <see cref="EntityBehaviorRespawnable"/> and notifies them of their remaining charges.
        /// </summary>
        private static void HandleServerInteractStop(EntityPlayer player, BlockSelection blockSel)
        {
            EntityBehaviorRespawnable? respawnable = player.GetBehavior<EntityBehaviorRespawnable>();
            if (respawnable == null)
            {
                RespawnGearModSystem.LogError("The respawnable behavior is not present on the player");
                return;
            }
            respawnable.SetSpawnPosition(blockSel.Position.AsVec3i + new Vec3i(0, 1, 0), player.Pos.Yaw, player.Pos.Pitch);
            ICoreServerAPI serverAPI = (ICoreServerAPI)player.Api;
            serverAPI.SendMessage( // TODO: This message is hardcoded, put into lang
                player.Player, 0,
                $"You have {respawnable.Charges} charges left, they will continue to recharge over time.",
                EnumChatType.Notification
            );
        }

        /// <summary>
        /// Loads and starts the looping gear sound at the targeted block position, then registers
        /// two safety callbacks (at 20ms and 3600ms) that stop and dispose the sound if the
        /// player is no longer interacting.
        /// </summary>
        private void StartSounds(EntityAgent byEntity, BlockSelection blockSel)
        {
            if (byEntity.World is IClientWorldAccessor clientWorldAccessor)
            {
                ILoadedSound sound;
                byEntity.World.Api.ObjectCache[SoundCacheKey] = sound = clientWorldAccessor.LoadSound(new SoundParams
                {
                    Location = new AssetLocation("sounds/effect/gears.ogg"),
                    ShouldLoop = true,
                    Position = blockSel.Position.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1f,
                    Pitch = 0.9f
                });
                sound?.Start();

                void StopIfIdle(float _)
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.None)
                    {
                        sound?.Stop();
                        sound?.Dispose();
                    }
                }

                byEntity.World.RegisterCallback(StopIfIdle, 20);
                byEntity.World.RegisterCallback(StopIfIdle, 3600);
            }
        }

        /// <summary> Retrieves the cached gear sound and stops and disposes it if it exists. </summary>
        private void TryStopSound()
        {
            ILoadedSound loadedSound = ObjectCacheUtil.TryGet<ILoadedSound>(api, SoundCacheKey);
            loadedSound?.Stop();
            loadedSound?.Dispose();
        }

        /// <summary>
        /// Spawns a single particle at <paramref name="pos"/> with a randomized hue and brightness.
        /// When <paramref name="final"/> is true, always spawns. Otherwise fires at a 20% chance per tick.
        /// </summary>
        private void SpawnParticles(IWorldAccessor world, Vec3d pos, bool final)
        {
            if ((final || world.Rand.NextDouble() > 0.8) && particlesHeld != null)
            {
                int h = 342 + world.Rand.Next(15);
                int v = 76 + world.Rand.Next(50);
                particlesHeld.MinPos = pos;
                particlesHeld.MinQuantity = 1f;
                particlesHeld.MinSize = 0.3f;
                particlesHeld.ParticleModel = EnumParticleModel.Cube;
                particlesHeld.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f);
                particlesHeld.Color = ColorUtil.ReverseColorBytes(ColorUtil.HsvToRgba(h, 180, v, 150));
                world.SpawnParticles(particlesHeld);
            }
        }

        #endregion

        #region UI

        /// <inheritdoc cref="CollectibleObject.GetHeldInteractionHelp(ItemSlot)"/>
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[1] {
                new() {
                    ActionLangCode = "heldhelp-useonground",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }

        #endregion
    }
}