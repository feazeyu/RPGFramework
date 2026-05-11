using System;
using System.Collections.Generic;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;

namespace Feazeyu.RPGSystems.EditorTools
{
    /// <summary>
    /// Extension point so other editor assemblies can contribute
    /// <see cref="BlackboardVariable"/> types to the blackboard
    /// picker without the blackboard panel needing to know about
    /// them directly.
    ///
    /// Usage (from any editor assembly, in an
    /// <c>[InitializeOnLoadMethod]</c> or static constructor):
    /// <code>
    /// BlackboardVariableTypeRegistry.Register(new BlackboardVariableTypeRegistry.Entry
    /// {
    ///     TypeName     = "Quest",
    ///     AccentColour = new Color(...),
    ///     Factory      = () => new BlackboardVariableQuest(),
    ///     Matcher      = v => v is BlackboardVariableQuest,
    /// });
    /// </code>
    ///
    /// The built-in dialogue types (Boolean, Integer, Float, etc.)
    /// are not registered here — they live directly in
    /// <see cref="BlackboardPanel"/>. The registry is checked after
    /// the built-in list so built-ins always take precedence on
    /// name/type collision.
    /// </summary>
    public static class BlackboardVariableTypeRegistry
    {
        public struct Entry
        {
            /// <summary>Short display name — shown in the picker and the type pill.</summary>
            public string                      TypeName;
            /// <summary>Colour used for the variable's type pill and colour dot.</summary>
            public Color                       AccentColour;
            /// <summary>Factory called when the user adds a new variable of this type.</summary>
            public Func<BlackboardVariable>    Factory;
            /// <summary>Returns true if the given variable instance is of this type.</summary>
            public Func<BlackboardVariable, bool> Matcher;
        }

        private static readonly List<Entry> s_Entries = new List<Entry>();

        /// <summary>
        /// Register a new type. Later registrations of the same
        /// <see cref="Entry.TypeName"/> overwrite earlier ones so
        /// a domain reload that re-runs static ctors is idempotent.
        /// </summary>
        public static void Register(Entry entry)
        {
            if (string.IsNullOrEmpty(entry.TypeName)) return;
            if (entry.Factory == null || entry.Matcher == null) return;

            for (int i = 0; i < s_Entries.Count; i++)
            {
                if (s_Entries[i].TypeName == entry.TypeName)
                {
                    s_Entries[i] = entry;
                    return;
                }
            }
            s_Entries.Add(entry);
        }

        public static IReadOnlyList<Entry> All => s_Entries;

        /// <summary>Look up an entry by type-name. Returns null if no match.</summary>
        public static Entry? Find(string typeName)
        {
            foreach (var e in s_Entries)
                if (e.TypeName == typeName) return e;
            return null;
        }

        /// <summary>
        /// Find the entry whose <see cref="Entry.Matcher"/> returns
        /// true for the given variable. Used by
        /// <see cref="BlackboardPanel"/> when the variable is an
        /// extension type and the built-in <c>GetShortTypeName</c>
        /// switch didn't match.
        /// </summary>
        public static Entry? FindFor(BlackboardVariable variable)
        {
            if (variable == null) return null;
            foreach (var e in s_Entries)
                if (e.Matcher(variable)) return e;
            return null;
        }
    }
}
