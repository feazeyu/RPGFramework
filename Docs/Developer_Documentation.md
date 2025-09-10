# Developer Documentation

## Project Overview

The project is meant as a starting point, or a helpful bunch of boilerplate code for your own project,
don't be afraid to root around and modify the provided files as it's impossible to truly encompass all
possible ways an RPG could be made and could work.
Suggestions and constructive criticism is appreciated!

## Installation

In the future, these files will be available on the unity asset store for free, as of now, simply download the project and put it into your unity project folder.
Keep in mind, that some folder names cannot be changed due to the way unity handles Resources and Editor files, don't rename those.

## Contents

The project is composed of modules, which are largely independent on each other, some dependencies of course exist.

### List of current modules

- Core
- Character
- Inventory
- Item

## Character module

This module aims to help with the implementation of weapons and their abilities, and provides the resource class to handle Health, mana, stamina and whatever else you might want.

### Resources

---

#### Resource class

To use this class, you'll need to inherit it in your own resource class, and add an additional resource type to the ResourceTypes enum in ResourceTypes.cs

Provided example classes are Health, Mana and Stamina.

#### Resource amount handling

To change the amount of a resource a GameObject has modify one of its properties - either "Points" or "Percent"

Percent are a value between 0 and 1, where 0 is 0 and 1 is maxAmount.

Points are a value between 0 and maxAmount.

#### Provided Events

By default the resource class provides event that are called when changing the resources' amounts.

The events are as follows:

- onResourceReachesZero: Invoked upon the resource being modified to 0
- onResourceLost: Invoked when the resources' new value is lower than the old one. Argument is the amount lost. This is a positive number.
- onResourceGained: Invoked when the resources' new value is higher than the old one.
Argument is the amount gained. This is a positive number
- onResourceChanged: Invoked when the resources' value changes.
Argument is the change. This can be either negative or positive.

### Entity

---
Entities are gameObjects that have Resources attached to them, and can potentially cast spells with them.

There is a provided method - GetResourceComponents() that gets all components castable to Resource and puts them in a dictionary, the keys are either the Type name, if it is the same as a ResourceType enum value, or the resourceType.

#### Spell casting

After providing a spell to cast, calling the Cast(SpellInfo spell) method checks the cooldown, the resources and then casts the spell by instantiating the prefab in your provided SpellInfo.

### Player Controller

PlayerController.cs is a sample player controller that assumes you're going to be using the new input system for Unity in Event mode, calling the OnMove, OnLook and OnAttack events correspondingly.

### Abilities

Weapons should all be derived from the class Weapon, and I have implemented a sample class, ComboWeapon in ComboWeapon.cs

#### WeaponCollisionHandler

Things that physically hit enemies should have the WeaponCollisionHandler component, which ensures each enemy is hit only once and Invokes the OnHit(GameObject collisionTarget) event.

#### Projectile

Simple implementation of a projectile that goes forward, and upon colliding with something, calls a WeaponCollisionHandler to handle the hit.

There are two more handy events:

- OnExpired: Called when the projectiles travels too far without hitting anything.
- OnDestroyed: Called when the projectile is destroyed in any way.

By default, expiration destroys the projectile.

A sample projectile prefab is included called TestProjectile.

#### SpellInfo

SpellInfo.cs

Is a scriptable object to define values of projectile spells. Modify to suit your needs.

#### ComboWeapon

---

This class handles purely animation based weapons, if you want to add projectiles etc. you'll have to modify it somehow.

Out of the box, this class works closely with the following scripts:

- WeaponOffsetController.cs
- ComboStep.cs
- Combo.cs

**Combo**

Is a scriptable object containing a number of ComboSteps

**ComboStep**

Is a scriptable object containing a motion and a time-out delay.

**WeaponOffsetController**

Resets the GameObjects' position to (0,0,0) after BeginRecovery() is called, invokes the onRecovered event after the object is returned.

Combo weapons themselves have an attached combo, and essentially play it's animations in sequence.When attack() is not called for too long or the end of the combo is reached, they tell their WeaponoffsetController to return the weapon to (0,0,0)

That means you need an animator attached to the same GameObject as the ComboWeapon.

That's why, the ComboWeaponEditor class in the same file has a GenerateAnimator method, which creates an animator from the assigned combo. You can later modify the animator however you want.

## Inventory module

This module implements two main types of inventories - Grid and list inventories

All inventories have two parts - a UI part and data part

Data wise, inventories are just collections of inventory slots, inventory slots in turn remember the id of the item they have in them.

In turn, you need a way to convert ids into item prefabs

### Inventory manager

This is a singleton class that does exactly what was mentioned above - converts id's into prefabs.

It automatically assigns id's when the editor button is pressed.

All of its functionality is in the method ReloadItems() where it goes through prefabs in the Resources folder that have the Item component and puts them into the items dictionary.

### Inventory Slot

Position - Position in the parent container (for example grid)

Anchor position - In case you're using multi-slot items, this is the position of the "main" item. All raycasts are redirected to the anchor.

IsEnabled - In case you want to disable some slots.

BaseColor - Color in the unity editor

#### Put item/Remove item/Editor only variants

In case you want to put an item somewhere it shouldn't go - there's an editor only function to force it in.

PutItem checks if the item goes into the slot by calling AcceptsItem, all logic determining whether or not can the item be put in should be here.

#### Custom inventory slots

If you want to add behavior to your inventory slots, you should inherit the main InventorySlot class and override the methods you need, or add your own. Just make sure that it is in the same assembly so it can be found by InventoryHelper's GetSlotTypeNames method.

### Drag handling

Drag handling is not done by the unity's input system, but instead by raycasting and then using InventoryItemUIHandler.

### InventoryItemUIHandler

#### Inventory slot drag handling

This component presumes the following is true, if it is not, it may not work correctly:

The gameobject:

- is within a canvas
- has a rect transform
- is a child to another gameobject, that implements the ISingleItemContainer interface

and a DragLayer exists.

A DragLayer is any gameobject called DragLayer, under which the dragged gameobject will be parented while dragging

This class implements Unity's drag handling interfaces IBeginDragHandler, IDragHandler and IEndDragHandler and their respective methods.

On beginning drag, we remove the item from the slot and create a dragged instance by using CreateDraggedItem(int itemId) to get a corresponding prefab from the InventoryManager.
We also remember the original parent.

This instance is a child of this gameobject, which gets moved to draglayer.

On continuing drag we simply reposition ourselves.

On ending drag, GetDragTarget(PointerEventData eventData) is used to find the slot we've ended our drag above using RaycastAll and filtering the results.

If no target is found, we return to the originalParent.

#### Tooltips

Furthermore, this class implements "tooltips" - if you want to display some information about items in the slot, like its stats.

These are implemented using Unity's IPointerEnterHandler and IPointerExitHandler interfaces.

On pointer enter, we start the coroutine to display the tooltip. It is cancelled on pointer exit.

The coroutine DisplayTooltip(PointerEventData eventData) itself is simple.

It waits for an amount of time, and then calls the item's DisplayTooltip(Transform parent) method, which just instantiates the object and returns it.

To ensure the tooltip is within screen bounds, there's some calculation to calculate the relative position to your mouse cursor - I just check positions one by one and choose the first one that is not outside the screen.

#### InventoryItemUIRedirectingHandler

Is a helper class, used primarily for multi slot items.

It has a targetPosition, and when asked who it's parent is, it returns the target cell's parent instead.
**The parent should only be retrieved using the provided GetOriginalParent() method, not using gameObject.transform.parent**

### PositionalUISlot

This component should be used in UI Slot elements that care about their position.

It expects to have a target IUIPositionalItemContainer (Both ListInventoryUI and GridInventory implement this)

When Putting/Removing items into this type of slot, it instead delegates the work to its target with a positional argument.

### InventoryGrid

This class is designed to generate and handle grid-based inventory operations. It is separated into a UI and a data part.

Just like inventory slots, item can be put into it or removed by using PutItem() and RemoveItem(). 
However, these require a Vector2Int position of where to be put or removed from, as the grid inventory is handled like a 2D array of inventorySlots.

In order to update the inventory's UI, you'll need to call InventoryGridGenerator's GenerateUI(), which is called from RedrawContents() aswell.

### InventoryGridGenerator

Handles the unity editor UI, and more importantly the generation of your UI elements. That is done by instantiating the selected slot prefabs into a target Canvas.

#### GenerateUI() Method

This is an expensive operation.

Destroys the last generated inventory by this component, and calls GenerateSlot() for each slot in the inventory

GenerateSlot() Instantiates the correct prefab and if there's an item in it, the item aswell.

### GridUISlot

Is a derived class of PositionalUISlots, this one supports "anchors"

Those are used to handle multislot items, as the above mentioned InventoryGridGenerator draws item in slots where they are, some slots are instead only anchor slot that act as if they had the item on the anchorposition in them for all intents and purposes.

### InventoryList

Is the data class for list inventories. List inventories are lists of StackableInventorySlots.

Just like InventoryGrid, it has a generator class for handling UI.

### InventoryListGenerator

Generates the UI part of inventory grid by calling DrawContents() which deletes the last inventorylist object and instantiates a new one. This is an expensive operation.

### InventoryListUI

Instantiates slots for each item that is inside of it, based on the slot prefab. When an item ends its drag inside the rect transform of this, PutItem is called on this.

CreateOriginPooint() is used to create a transform that is the parent of all the inventory slots in this inventory. OnScroll moves this transform.

RedrawContents() is the method for deleting and instantiating all the slots again. This is an expensive operation.

### TextCountItemRenderer

In case you don't want your items to always look like their prefab, you can do something similiar to this class.

Instead of drawing the sprite of the item in the list inventory, thanks to this class only its amount and name will be shown.

## Core

Contains mainly utility functions and classes.

I do not recommend editing existing files here, as it may have unforseen consequences

### Utilities

#### Array2D

A Unity serializable generic 2d array. Reading elements and enumeration should work the same as a regular 2D array, internally stored as a 1D array which remembers the number of elements in a row.

#### EditorHelper

A bunch of useful functions for manipulating Unity editor UI

#### EventRedirector

This component redirects a bunch of Unity events to a target gameObject. Probably doesn't have them all, adding additional ones wont break anything.

#### RectBoundCheck

Check if one RectTransform is completely within another one using IsElementWithinAnother()

#### RotateTowards

Rotate an object towards a point.

#### SerializableDictionary

A classic dictionary, but it is serializable by unity. Works just like a dictionary, but implements serialization. Serialization splits the dictionary into two collections, one of keys and one of values.