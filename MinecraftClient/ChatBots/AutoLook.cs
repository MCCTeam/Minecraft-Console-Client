using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    public class AutoLook : ChatBot
    {
        private Entity _entityToLookAt;
        public override void Initialize()
        {
            if (GetEntityHandlingEnabled()) return;
            LogToConsole("Entity Handling is not enabled in the config file!");
            LogToConsole("This bot will be unloaded.");
            UnloadBot();
        }

        public override void Update()
        {
        }
        public override void OnEntityDespawn(Entity entity)
        {
            if (entity == _entityToLookAt)
            {
                _entityToLookAt = null;
            }
        }
        public override void OnEntitySpawn(Entity entity)
        {
            HandleEntity(entity);
        }
        public override void OnEntityMove(Entity entity)
        {
            bool tempBool = HandleEntity(entity);
            if (!tempBool) return;
            LookAtLocation(entity.Location);
        }
        /// <summary>
        /// Handles an entity, and tracks it if it is closer then the one we are currently tracking
        /// </summary>
        /// <returns>True if found</returns>
        private bool HandleEntity(Entity entity)
        {
            if (entity.Type != EntityType.Player)
            {
                return false;
            }
            if (_entityToLookAt == null)
            {
                _entityToLookAt = entity;
                return true;
            }
            if (GetCurrentLocation().Distance(entity.Location) < GetCurrentLocation().Distance(_entityToLookAt.Location))
            {
                _entityToLookAt = entity;
                return true;
            }

            if (entity.ID != _entityToLookAt.ID) return false;
            _entityToLookAt = entity; //Handle looking at the same entity
            return true;

        }

    }
}