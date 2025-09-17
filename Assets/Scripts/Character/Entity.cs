using UnityEngine;
using Game.Core.Utilities;
using System;
namespace Game.Character
{
    /// <summary>
    /// Represents an entity in the game that can cast spells and manage resources.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        /// <summary>
        /// The resources available to this entity, mapped by resource type.
        /// </summary>
        public SerializableDictionary<ResourceTypes, Resource> resources;

        /// <summary>
        /// The cooldowns for each spell, mapped by spell info.
        /// </summary>
        public SerializableDictionary<SpellInfo, DateTime> spellCooldowns;

        /// <summary>
        /// The transform where spells are spawned when cast.
        /// </summary>
        [Tooltip("Spells spawn here")]
        public Transform castingPosition;

        /// <summary>
        /// The transform whose rotation is inherited by spawned spells.
        /// </summary>
        [Tooltip("Spells inherit the rotation of this")]
        public Transform castingRotationReference;

        /// <summary>
        /// Initializes the entity's resources and spell cooldowns.
        /// </summary>
        private void Awake()
        {
            resources = new SerializableDictionary<ResourceTypes, Resource>();
            spellCooldowns = new SerializableDictionary<SpellInfo, DateTime>();
            GetResourceComponents();
        }

        /// <summary>
        /// Retrieves all <see cref="Resource"/> components attached to this entity and populates the <see cref="resources"/> dictionary.
        /// </summary>
        public void GetResourceComponents()
        {
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

        /// <summary>
        /// Attempts to cast the specified spell, deducting resources and handling cooldowns.
        /// </summary>
        /// <param name="spell">The spell to cast.</param>
        /// <returns>
        /// The instantiated spell <see cref="GameObject"/> if the cast is successful; otherwise, <c>null</c>.
        /// </returns>
        public GameObject? Cast(SpellInfo spell)
        {
            if (!spellCooldowns.TryGetValue(spell, out var cooldown))
            {
                spellCooldowns[spell] = DateTime.Now;
            }
            if (cooldown > DateTime.Now)
            {
                return null; // Spell is on cooldown
            }

            foreach (var resourceCost in spell.resourceCosts)
            {
                if (!resources.TryGetValue(resourceCost.Key, out var resource) || resource.Points < resourceCost.Value)
                {
                    Debug.LogWarning($"Not enough {resourceCost.Key} to cast {spell.name}");
                    return null; // Not enough resources
                }
            }
            // Has enough, deduct and cast.
            foreach (var resourceCost in spell.resourceCosts)
            {
                if (resources.TryGetValue(resourceCost.Key, out var resource))
                {
                    resource.Points -= resourceCost.Value;
                }
            }
            var SpellObject = Instantiate(spell.prefab);
            spellCooldowns[spell] = DateTime.Now.AddMilliseconds(spell.cooldown);
            SpellObject.transform.position = castingPosition.position;
            SpellObject.transform.rotation = castingRotationReference.rotation;
            return SpellObject;
        }
    }
}
