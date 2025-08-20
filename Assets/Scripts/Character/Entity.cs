using UnityEngine;
using Game.Core.Utilities;
namespace Game.Character
{
    public class Entity : MonoBehaviour
    {
        public SerializableDictionary<ResourceTypes, Resource> resources;

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
            }
        }

        public GameObject? Cast(SpellInfo spell) { 
            foreach (var resourceCost in spell.resourceCosts)
            {
                if (!resources.TryGetValue(resourceCost.Key, out var resource) || resource.Points < resourceCost.Value)
                {
                    Debug.LogWarning($"Not enough {resourceCost.Key} to cast {spell.name}");
                    return null; // Not enough resources
                }
            }
            //Has enough, deduct and cast.
            foreach (var resourceCost in spell.resourceCosts)
            {
                if (resources.TryGetValue(resourceCost.Key, out var resource))
                {
                    resource.Points -= resourceCost.Value;
                }
            }
            var SpellObject = Instantiate(spell.prefab);
            return SpellObject;
        }
    }
}
