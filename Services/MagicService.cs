using System;
using System.Collections.Generic;
using System.Linq;
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
            var status = $"**Magic Status**\n";
            status += $"Magic: {_magicSystem.Magic}\n";
            status += $"Magician: {_magicSystem.Magician}\n";
            status += $"Awakened: {_magicSystem.Awakened}\n";
            status += $"Sorcerer: {_magicSystem.Sorcerer}\n";
            status += $"Adept: {_magicSystem.Adept}\n";
            status += $"Criticality: {_magicSystem.Criticality}\n";
            status += $"Instinct: {_magicSystem.Instinct}\n";
            status += $"Initiative: {_magicSystem.Initiative}\n";
            status += $"Wounds: {_magicSystem.Wounds}\n";
            status += $"Wound Mod: {_magicSystem.WoundMod}\n";
            status += $"Recovery: {_magicSystem.Recovery}\n";
            status += $"Magical Resistance: {_magicSystem.MagicalResistance}\n";
            status += $"Initiative Pool: {_magicSystem.InitiativePool}\n";
            status += $"Complex Form Pool: {_magicSystem.ComplexFormPool}\n";
            
            return status;
        }

        /// <summary>
        /// Get the list of active foci
        /// </summary>
        public string GetFocusList()
        {
            if (_magicSystem.Foci.Count == 0)
                return "No foci currently active.";
            
            var focusList = "**Active Foci:**\n";
            foreach (var focus in _magicSystem.Foci)
            {
                focusList += $"- {focus.Name} ({focus.Type}) - {focus.Count}x, Essence: {focus.EssenceCost}\n";
            }
            
            return focusList;
        }

        /// <summary>
        /// Get the list of known spells
        /// </summary>
        public string GetSpellList()
        {
            var spellList = "**Known Spells:**\n";
            foreach (var spell in SpellDatabase.Spells)
            {
                spellList += $"- {spell.Name} ({spell.Category}) - Force {spell.Force}\n";
            }
            
            return spellList;
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
            
            var embed = $"**Spell Cast: {spell.Name}**\n";
            embed += $"Force: {spell.Force}\n";
            embed += $"Damage: {spell.Damage} {spell.DamageType}\n";
            embed += $"Defense Target: {spell.DefenseTarget} {spell.DefenseType}\n";
            embed += $"Duration: {spell.Duration}\n";
            embed += $"Complex Form: {spell.ComplexForm}\n";
            embed += $"Service: {spell.Service}\n";
            embed += $"Pool: {pool}\n";
            embed += $"Result: {result.Successes} successes\n";
            embed += $"Rolls: {result.Details}\n";
            
            return embed;
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

        /// <summary>
        /// Check if the character can cast spells
        /// </summary>
        public bool CanCastSpells()
        {
            return _magicSystem.Magic > 0 && (_magicSystem.Magician || _magicSystem.Sorcerer || _magicSystem.Awakened);
        }

        /// <summary>
        /// Get drain resistance pool
        /// </summary>
        public int GetDrainPool()
        {
            // Typically Willpower + something, simplified here
            return _magicSystem.Magic + _magicSystem.Recovery;
        }
    }
}
