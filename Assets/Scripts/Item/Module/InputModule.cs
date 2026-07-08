using System;
using Core.ItemSystem.Module;
using UnityEngine;

namespace Item.Module
{
    public enum GameplayInputAction
    {
        Attack,
        Secondary,
        Interact,
        UseSlot1,
        UseSlot2,
        Dodge,
    }

    public enum GameplayInputTrigger
    {
        Started,
        Performed,
        Canceled,
        Held,
    }

    public readonly struct GameplayInputEvent
    {
        public readonly GameplayInputAction Action;
        public readonly GameplayInputTrigger Trigger;
        public readonly float Time;

        public GameplayInputEvent(GameplayInputAction action, GameplayInputTrigger trigger, float time)
        {
            Action = action;
            Trigger = trigger;
            Time = time;
        }
    }

    [Serializable]
    public struct ItemInputBinding
    {
        public GameplayInputAction Action;
        public GameplayInputTrigger Trigger;
        public bool ConsumeInput;
        public float BufferTime;
    }

    [Serializable]
    public sealed class InputModule : StatelessModule
    {
        [SerializeField] private ItemInputBinding[] bindings;
    }

    public enum ItemInputAction
    {
        None,
        Attack,
        SecondaryAction,
        Interact,
        UseSlot1,
        UseSlot2,
        UseSlot3,
        UseSlot4,
        UseSlot5,
        UseSlot6
    }

    public enum ItemInputTrigger
    {
        Started,
        Performed,
        Canceled
    }
}
