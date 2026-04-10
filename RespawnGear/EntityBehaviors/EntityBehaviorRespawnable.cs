using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace RespawnGear.EntityBehaviors
{
    public class EntityBehaviorRespawnable(Entity entity) : EntityBehavior(entity)
    {
        public static readonly string BEHAVIOR_ID = "respawninfo";

        public float PosX
        {
            get
            {
                return entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).GetFloat("x");
            }
            set
            {
                entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).SetFloat("x", value);
                entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
            }
        }
        public float PosY
        {
            get
            {
                return entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).GetFloat("y");
            }
            set
            {
                entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).SetFloat("y", value);
                entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
            }
        }
        public float PosZ
        {
            get
            {
                return entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).GetFloat("z");
            }
            set
            {
                entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).SetFloat("z", value);
                entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
            }
        }
        public float Yaw
        {
            get
            {
                return entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).GetFloat("yaw");
            }
            set
            {
                entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).SetFloat("yaw", value);
                entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
            }
        }
        public float Pitch
        {
            get
            {
                return entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).GetFloat("pitch");
            }
            set
            {
                entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).SetFloat("charges", value);
                entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
            }
        }

        public double Timestamp
        {
            get
            {
                return entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).GetDouble("timestamp");
            }
            set
            {
                entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).SetDouble("timestamp", value);
                entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
            }
        }
        public int Charges
        {
            get
            {
                return entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).GetInt("charges");
            }
            set
            {
                entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID).SetInt("charges", value);
                entity.WatchedAttributes.MarkPathDirty(BEHAVIOR_ID);
            }
        }

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            ITreeAttribute treeAttribute = entity.WatchedAttributes.GetTreeAttribute(BEHAVIOR_ID);
            if (treeAttribute == null)
            {
                treeAttribute = new TreeAttribute();
                entity.WatchedAttributes.SetAttribute(BEHAVIOR_ID, treeAttribute);

                PosX = typeAttributes["x"].AsFloat();
                PosY = typeAttributes["y"].AsFloat();
                PosZ = typeAttributes["z"].AsFloat();
                Yaw = typeAttributes["yaw"].AsFloat();
                Pitch = typeAttributes["pitch"].AsFloat();

                Timestamp = typeAttributes["timestamp"].AsDouble(defaultValue: -1);
                Charges = typeAttributes["charges"].AsInt();

            }
        }

        /// <summary>
        /// Updates the current timestamp and adds the corresponding charges
        /// </summary>
        public void CalculateTimestampAndCharges()
        {
            if (Timestamp == -1)
            {
                // This is the first time the player uses the gear
                Charges = 0;
                Timestamp = entity.World.Calendar.TotalHours;
            }
            else
            {
                double hoursPerCharge = 1; // TODO, 1 day hardcoded, abstract to a config

                double elapsed = entity.World.Calendar.TotalHours - Timestamp;
                int chargesGained = (int) (elapsed / hoursPerCharge);

                Charges = Math.Clamp(Charges + chargesGained, 0, 3); // TODO, 3 charges are hardcoded, to the config it goes
                Timestamp = entity.World.Calendar.TotalHours - (elapsed % hoursPerCharge);
            }
        }

        public override string PropertyName()
        {
            return BEHAVIOR_ID;
        }
    }
}
