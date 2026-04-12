using RespawnGear.Misc;
using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RespawnGear.EntityBehaviors
{
    public class EntityBehaviorRespawnable(Entity entity) : EntityBehavior(entity)
    {
        public static readonly string BEHAVIOR_ID = "respawninfo";
        public override string PropertyName() => BEHAVIOR_ID;

        #region Properties
        ITreeAttribute Tree
        {
            get => entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID);
            set => entity.WatchedAttributes.SetAttribute(BEHAVIOR_ID, value);
        }
        public Vec3i Position
        {
            get => Tree.GetVec3i("position");
            set => InTree(t => t.SetVec3i("position", value));
        }
        public float Yaw
        {
            get => Tree.GetFloat("yaw");
            set => InTree(t => t.SetFloat("yaw", value));
        }
        public float Pitch
        {
            get => Tree.GetFloat("pitch");
            set => InTree(t => t.SetFloat("pitch", value));
        }
        public double Timestamp
        {
            get => Tree.GetDouble("timestamp");
            set => InTree(t => t.SetDouble("timestamp", value));
        }
        public int Charges
        {
            get => Tree.GetInt("charges");
            set => InTree(t => t.SetInt("charges", value));
        }
        #endregion

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            if (Tree == null)
            {
                Tree = new TreeAttribute();
                Timestamp = -1;
                Charges = RespawnGearModSystem.Config.InitCharges;
            }
        }

        /// <summary> Sets the modded spawn position and calculates the timestamp and charges </summary>
        /// <param name="pos"> A <see cref="Vec3i"/> where the spawn point will be placed at </param>
        /// <param name="yaw"> Horizontal rotation </param>
        /// <param name="pitch"> Vertical rotation </param>
        public void SetSpawnPosition(Vec3i pos, float yaw, float pitch)
        {
            Position = pos;
            Yaw = yaw;
            Pitch = pitch;
            CalculateTimestampAndCharges();
        }

        /// <summary> Updates the current timestamp and adds the corresponding charges </summary>
        public void CalculateTimestampAndCharges()
        {
            if (Timestamp == -1) Timestamp = entity.World.Calendar.TotalHours;
            else
            {
                double hoursPerCharge = RespawnGearModSystem.Config.HoursPerCharge;

                double elapsed = entity.World.Calendar.TotalHours - Timestamp;
                int chargesGained = (int) (elapsed / hoursPerCharge);

                Charges = Math.Clamp(Charges + chargesGained, 0, RespawnGearModSystem.Config.MaxCharges);
                Timestamp = entity.World.Calendar.TotalHours - (elapsed % hoursPerCharge);
            }
        }

        /// <summary> Outsources tree insertion by receiving a lambda that expects an <see cref="ITreeAttribute"/> </summary>
        /// <param name="set"> A lambda that inserts a propertie into the tree of watched attributes </param>
        private void InTree(Action<ITreeAttribute> set)
        {
            set(Tree);
            entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
        }
    }
}
