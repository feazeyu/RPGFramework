#nullable enable

using UnityEngine;
using UnityEngine.Events;
using Feazeyu.RPGSystems.Core.Utilities;
using System;
namespace Feazeyu.RPGSystems.Character
{
    /// <summary>
    /// Represents an entity in the game that can cast spells and manage resources.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        /// <summary>
        /// The resources available to this entity, mapped by resource type.
        /// </summary>
        public SerializableDictionary<ResourceTypes, Resource>? resources;

        /// <summary>
        /// The transform where spells are spawned when cast.
        /// </summary>
        [Tooltip("Spells spawn here")]
        public Transform? castingPosition;

        /// <summary>
        /// The transform whose rotation is inherited by spawned spells.
        /// </summary>
        [Tooltip("Spells inherit the rotation of this")]
        public Transform? castingRotationReference;

        [Header("Events")]
        public UnityEvent OnDeath;

        /// <summary>
        /// Initializes the entity's resources and spell cooldowns.
        /// </summary>
        protected virtual void Awake()
        {
            resources = new SerializableDictionary<ResourceTypes, Resource>();
            GetResourceComponents();
            if (resources != null && resources.TryGetValue(ResourceTypes.Health, out var healthRes))
                healthRes.onResourceReachesZero += () => OnDeath?.Invoke();
        }

        /// <summary>
        /// Retrieves all <see cref="Resource"/> components attached to this entity and populates the <see cref="resources"/> dictionary.
        /// </summary>
        public void GetResourceComponents()
        {
            if (resources == null)
            {
                return;
            }
            resources.Clear();
            var resourceComponents = GetComponents<Resource>();

            foreach (var resource in resourceComponents)
            {
                // Try to parse the ResourceTypes enum from the type name
                if (System.Enum.TryParse<ResourceTypes>(resource.GetType().Name, out var resourceType))
                {
                    resources.Add(resourceType, resource);
                }
                else if (resource.resourceType != ResourceTypes.None)
                {
                    resources.Add(resource.resourceType, resource);
                }
            }
        }
    }
}
