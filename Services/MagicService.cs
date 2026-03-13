using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services
{
    /// <summary>
    /// Service for handling magic system operations
    /// </summary>
    public class MagicService
    {
        private readonly MagicSystem _magicSystem;
        private readonly DiceService _diceService;

        public MagicService(MagicSystem magicSystem, DiceService diceService)
        {
            _magicSystem = magicSystem;
            _diceService = diceService;
        }

        /// <summary>
        /// Get the current magic status display
        /// </summary>
        public string GetMagicStatus()
        {
            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**Magic Status**");
            sb.AppendLine($"Magic: {_magicSystem.Magic}");
            sb.AppendLine($"Magician: {_magicSystem.Magician}");
            sb.AppendLine($"Awakened: {_magicSystem.Awakened}");
            sb.AppendLine($"Sorcerer: {_magicSystem.Sorcerer}");
            sb.AppendLine($"Adept: {_magicSystem.Adept}");
            sb.AppendLine($"Criticality: {_magicSystem.Criticality}");
            sb.AppendLine($"Instinct: {_magicSystem.Instinct}");
            sb.AppendLine($"Initiative: {_magicSystem.Initiative}");
            sb.AppendLine($"Wounds: {_magicSystem.Wounds}");
            sb.AppendLine($"Wound Mod: {_magicSystem.WoundMod}");
            sb.AppendLine($"Recovery: {_magicSystem.Recovery}");
            sb.AppendLine($"Magical Resistance: {_magicSystem.MagicalResistance}");
            sb.AppendLine($"Initiative Pool: {_magicSystem.InitiativePool}");
            sb.AppendLine($"Complex Form Pool: {_magicSystem.ComplexFormPool}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Get the list of active foci
        /// </summary>
        public string GetFocusList()
        {
            if (_magicSystem.Foci.Count == 0)
                return "No foci currently active.";
            
            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**Active Foci:**");
            foreach (var focus in _magicSystem.Foci)
            {
                sb.AppendLine($"- {focus.Name} ({focus.Type}) - {focus.Count}x, Essence: {focus.EssenceCost}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Get the list of known spells
        /// </summary>
        public string GetSpellList()
        {
            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**Known Spells:**");
            foreach (var spell in SpellDatabase.Spells)
            {
                sb.AppendLine($"- {spell.Name} ({spell.Category}) - Force {spell.Force}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Cast a spell and return the result
        /// </summary>
        public string CastSpell(string spellName)
        {
            var spell = SpellDatabase.Spells
                .FirstOrDefault(s => s.Name.ToLower() == spellName.ToLower());
            
            if (spell == null)
                return $"Spell '{spellName}' not found.";
            
            // Roll casting test using Shadowrun dice rules
            var pool = _magicSystem.Magic;
            if (pool <= 0)
                return "You don't have any Magic rating to cast spells.";
            
            var result = _diceService.RollShadowrun(pool, 5);
            
            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine($"**Spell Cast: {spell.Name}**");
            sb.AppendLine($"Force: {spell.Force}");
            sb.AppendLine($"Damage: {spell.Damage} {spell.DamageType}");
            sb.AppendLine($"Defense Target: {spell.DefenseTarget} {spell.DefenseType}");
            sb.AppendLine($"Duration: {spell.Duration}");
            sb.AppendLine($"Complex Form: {spell.ComplexForm}");
            sb.AppendLine($"Service: {spell.Service}");
            sb.AppendLine($"Pool: {pool}");
            sb.AppendLine($"Result: {result.Successes} successes");
            sb.AppendLine($"Rolls: {result.Details}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Add a focus to the character's active foci
        /// </summary>
        public string AddFocus(Focus focus)
        {
            _magicSystem.Foci.Add(focus);
            return $"Added focus: {focus.Name} ({focus.Type})";
        }

        /// <summary>
        /// Remove a focus from the character's active foci
        /// </summary>
        public string RemoveFocus(string focusName)
        {
            var focus = _magicSystem.Foci.FirstOrDefault(f => f.Name.ToLower() == focusName.ToLower());
            if (focus == null)
                return $"Focus '{focusName}' not found.";
            
            _magicSystem.Foci.Remove(focus);
            return $"Removed focus: {focus.Name}";
        }
    }
}
