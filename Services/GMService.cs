using System;
using System.Collections.Generic;
using System.Linq;

namespace ShadowrunDiscordBot.Services
{
    public class GMService
    {
        private readonly DiceService _diceService;

        public GMService(DiceService diceService)
        {
            _diceService = diceService;
        }

        // Mission Generator
        public string GenerateMission(string missionType)
        {
            missionType = missionType?.ToLower() ?? "generic";

            var templates = GetMissionTemplates(missionType);
            var template = templates[Random.Shared.Next(templates.Count)];

            return template;
        }

        private Dictionary<string, List<string>> GetMissionTemplates(string type)
        {
            var templates = new Dictionary<string, List<string>>();

            templates["cyberdeck"] = new List<string>
            {
                $"The team needs to breach the mainframe of {{company_name}} Corp to steal the {{artifact}} project data. Priority target: {{hostile_system}}. Additional threat: {{ice_configuration}}.",
                $"The {{company_name}} server farm is hosting a dangerous {{project_name}} that {{villain_name}} wants to sabotage. The team must infiltrate, steal the data, and escape. Threat level: {{threat_level}}.",
                $"A corporate assassination has been ordered. The team must eliminate {{target}} before they can deliver {{project_name}}. Needs extraction vehicle: {{vehicle_type}}.",
                $"The {{company_name}} AR node is leaking data to the {{company_name}} competitors. The team must find the leak and close it. Complication: {{secret_informant}}.",
                $"A {{villain_name}} hacker is using {{company_name}}'s resources to {{villain_goal}}. The team must trace the connection and shut down {{target_system}}."
            };

            templates["assassination"] = new List<string>
            {
                $"Target: {{target_name}}. Location: {{location}}. Manner: {{manner}}. Motivation: {{motivation}}. Risk: {{risk_level}}.",
                $"The {{target_name}} is hosting a gala at {{location}}. Team must eliminate them without alerting {{villain_name}}.",
                $"Target is protected by {{security_level}}. Need to create {{cover_story}} to get close.",
                $"Mission: {{target_name}} at {{location}}. Approach: {{approach}}. Backup plan: {{backup_plan}}.",
                $"Contract issued by {{company_name}} to eliminate {{target_name}}. Payment: {{payment_amount}} nuyen. Deadline: {{deadline}}."
            };

            templates["extraction"] = new List<string>
            {
                $"Extract {{hostage_name}} from {{location}}. Extraction method: {{extraction_method}}. Transport: {{transport}}.",
                $"{{company_name}} has a {{target_name}} in custody at {{location}}. Break them out!",
                $"Evacuation needed from {{location}}. Extraction point: {{extraction_point}}. Prepare {{vehicle_type}}.",
                $"Target needs to get to {{location}}. Team must escort them with {{protection_type}}.",
                $"{{villain_name}} has a {{target_name}} at {{location}}. Team must infiltrate, free them, and get out."
            };

            templates["theft"] = new List<string>
            {
                $"Steal {{artifact_name}} from {{location}}. Target is guarded by {{security_level}}. Approach: {{approach}}.",
                $"{{company_name}} is liquidating {{project_name}}. Team must intercept and {{action}}.",
                $"The {{artifact_name}} is being transported from {{location}}. Team must intercept the {{transport_type}}.",
                $"Target: {{artifact_name}}. Location: {{location}}. Timeframe: {{timeframe}}. Risk: {{risk_level}}.",
                $"{{villain_name}} wants {{artifact_name}}. Team must steal it before {{company_name}} does."
            };

            templates["investigation"] = new List<string>
            {
                $"Investigate {{mystery}} at {{location}}. Team must find {{clue}} and determine {{conclusion}}.",
                $"Rumors of {{villain_name}} activities at {{location}}. Find the evidence.",
                $"{{company_name}} is {{incident_description}}. Team must uncover the truth.",
                $"A {{artifact_name}} was stolen from {{location}}. Trace the evidence.",
                $"Investigate {{location}} for {{mystery}}. Answers needed: {{questions}}."
            };

            return templates;
        }

        // NPC Generator
        public string GenerateNPC(string role)
        {
            var npc = new NPC
            {
                Name = GenerateName(),
                Role = role,
                Company = GenerateCompany(),
                Description = GenerateDescription(role),
                Stats = GenerateNPCStats(role),
                Motivation = GenerateMotivation(),
                Backstory = GenerateBackstory()
            };

            return GenerateNPCEmbed(npc);
        }

        private string GenerateName()
        {
            var surnames = new[] { "Chen", "Tanaka", "Webb", "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis" };
            var givenNames = new[] { "Kai", "Ren", "Alex", "Sarah", "Mike", "Jake", "Nova", "Echo", "Vega", "Jax" };

            return $"{givenNames[Random.Shared.Next(givenNames.Length)]} {surnames[Random.Shared.Next(surnames.Length)]}";
        }

        private string GenerateCompany()
        {
            var companies = new[]
            {
                "Arasaka", "Kangtaek Corporation", "Michell Technologies", "Saeder-Krupp", "Renraku",
                "Wuxing", "Shiawase", "Biotechnica", "Soviet Union", "Company ShadowOps"
            };
            return companies[Random.Shared.Next(companies.Length)];
        }

        private string GenerateDescription(string role)
        {
            var descriptions = new Dictionary<string, List<string>>
            {
                ["corporate exec"] = new List<string> {
                    "Suit and tie, expensive cyberware, briefcase with encryption keys",
                    "Well-dressed with corporate tattoos, holding a datapad",
                    "Bodyguards in the background, holding a glass of expensive wine"
                },
                ["fixer"] = new List<string> {
                    "Wearing expensive cyberware, holding a datapad",
                    "Looking nervous, scanning the room for threats",
                    "Relaxed, smoke in hand, obviously has connections"
                },
                ["street doc"] = new List<string> {
                    "Wearing stained medical scrubs, tool belt around waist",
                    "Blood on their hands, bandaged wounds visible",
                    "Surgical mask on, working on someone in the corner"
                },
                ["shadowrunner"] = new List<string> {
                    "Casual clothing, obvious cyberware",
                    "Tattoos visible, weapons concealed",
                    "Looking around nervously, checking for shadows"
                },
                ["corporate guard"] = new List<string> {
                    "Full body armor, holding a weapon",
                    "Formal uniform with security badge",
                    "Checking identification, scanning faces"
                },
                ["terrorist"] = new List<string> {
                    "Weapons visible, explosive vest clearly marked",
                    "Holding an explosive device, panicked expression",
                    "Deadpan face, clearly committed to violence"
                }
            };

            var roleList = descriptions.ContainsKey(role) ? descriptions[role] : descriptions["shadowrunner"];
            return roleList[Random.Shared.Next(roleList.Count)];
        }

        private string GenerateNPCStats(string role)
        {
            var attributes = new Dictionary<string, int[]>
            {
                ["corporate exec"] = new[] { 2, 1, 2, 1, 3, 1 },
                ["fixer"] = new[] { 2, 1, 3, 2, 3, 2 },
                ["street doc"] = new[] { 2, 3, 2, 3, 1, 1 },
                ["shadowrunner"] = new[] { 3, 3, 3, 3, 2, 3 },
                ["corporate guard"] = new[] { 2, 3, 2, 3, 1, 1 },
                ["terrorist"] = new[] { 3, 2, 3, 2, 1, 2 }
            };

            var roleList = attributes.ContainsKey(role) ? attributes[role] : attributes["shadowrunner"];
            return $"INT {roleList[0]} | BODY {roleList[1]} | REA {roleList[2]} | STR {roleList[3]} | CHR {roleList[4]} | EDGE {roleList[5]}";
        }

        private string GenerateMotivation()
        {
            var motivations = new[]
            {
                "Money - Wants to pay off debt",
                "Revenge - Someone wronged them",
                "Ideology - Believe in their cause",
                "Power - Seeking more influence",
                "Love - Protecting someone",
                "Survival - Simply trying to survive"
            };
            return motivations[Random.Shared.Next(motivations.Length)];
        }

        private string GenerateBackstory()
        {
            var backstories = new[]
            {
                "Used to work for {{company_name}}, got burned, now runs freelance",
                "{{villain_name}} saved their life once. Now owes them a debt.",
                "{{company_name}} destroyed their family. They want revenge.",
                "Went {{adventure_type}} and saw too much. Now they have to flee.",
                "Was {{past_role}} but {{corporate intrigue}} forced them out.",
                "Found something {{discovery}} and can't let {{enemy}} get to it."
            };
            return backstories[Random.Shared.Next(backstories.Length)];
        }

        private string GenerateNPCEmbed(NPC npc)
        {
            return $@"**NPC Profile**

**Name:** {npc.Name}
**Role:** {npc.Role}
**Company:** {npc.Company}
**Description:** {npc.Description}
**Attributes:** {npc.Stats}
**Motivation:** {npc.Motivation}
**Backstory:** {npc.Backstory}

---
🎲 Difficulty: {_diceService.ParseAndRoll(""1d6"").Total}";
        }

        public class NPC
        {
            public string Name { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Company { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Stats { get; set; } = string.Empty;
            public string Motivation { get; set; } = string.Empty;
            public string Backstory { get; set; } = string.Empty;
        }

        // Location Generator
        public string GenerateLocation(string locationType)
        {
            var locations = new Dictionary<string, List<string>>
            {
                ["corporate"] = new List<string>
                {
                    "Arasaka Corporate Tower - Executive Suite",
                    "Kangtaek Research Facility - Laboratory Level",
                    "Michell Technologies Server Farm - Underground",
                    "Renraku Red Brick Tower - Client Lounge",
                    "Wuxing Antiques Shop - Hidden Back Room"
                },
                ["seedy"] = new List<string>
                {
                    "The Rusty Rat - Dive Bar, Lower Night City",
                    "Noodle Shop - Street Level, Food Stalls",
                    "Shack on the Edge of Slums - Unregulated Zone",
                    "Abandoned Warehouse - Corrupted by ICE",
                    "Back Alley Shop - Unlicensed Cyberware Dealer"
                },
                ["safehouse"] = new List<string>
                {
                    "Rooftop Safehouse - Professional Security",
                    "Safe Apartment - Concealed Exit",
                    "Subterranean Bunker - Self-Sufficient",
                    "Safe House in Corporation District - Discreet",
                    "Remote Cabin - Isolated Location"
                },
                ["combat"] = new List<string>
                {
                    "Factory Floor - Automated Defense Systems",
                    "City Streets - Crowded, Urban Combat",
                    "Roof of Building - Elevated Combat",
                    "Warehouse - Narrow Passages",
                    "Nightclub - Crowd Control Problems"
                }
            };

            var typeList = locations.ContainsKey(locationType) ? locations[locationType] : locations["seedy"];
            return typeList[Random.Shared.Next(typeList.Count)];
        }

        // Plot Hook Generator
        public string GeneratePlotHook()
        {
            var hooks = new List<string>
            {
                "A mysterious package arrives with a single name inside. Who wants it?",
                "Your contact betrays you during a job. Can you survive the betrayal?",
                "You discover evidence of {{villain_name}}'s true plan. Now you're a target.",
                "{{company_name}} is hiring runners. The job looks simple... until you arrive.",
                "A {{target_name}} has information that could change everything. Find them first.",
                "You're offered {{reward_amount}} nuyen to {{mission_type}}. Too good to be true?",
                "{{villain_name}} is {{threat_description}}. The team must stop them before it's too late.",
                "A shadow war has broken out between {{faction_a}} and {{faction_b}}. Choose your side."
            };

            return hooks[Random.Shared.Next(hooks.Count)];
        }

        // Loot Generator
        public string GenerateLoot()
        {
            var items = new List<string>
            {
                "Data Shards - Corporate secrets",
                "Sumeragi Data Chip - Top-secret files",
                "Nuyen - 5,000 ¥",
                "Cyberware - {{cyberware_type}}",
                "Weapons - {{weapon_type}} with {{quality}}",
                "Medical Supplies - Life-saving materials",
                "Intel - Location of {{asset}}",
                "Contract - A new job for {{company_name}}"
            };

            return items[Random.Shared.Next(items.Count)];
        }

        // Random Event Generator
        public string GenerateRandomEvent()
        {
            var events = new List<string>
            {
                "A corporate security team is investigating the area. Time is running out!",
                "{{villain_name}}'s bounty hunter has found your trail. Prepare for a chase.",
                "An accident happens. The team must investigate or exploit the situation.",
                "A corrupt officer threatens to call it in. Bribe or fight?",
                "{{company_name}} increases security. Time to change your approach.",
                "A rival gang steps in. Negotiate or fight?",
                "A ghost from {{villain_name}}'s past appears. Hidden connection?",
                "The location is about to be swarmed. Evacuate or push through?"
            };

            return events[Random.Shared.Next(events.Count)];
        }

        // Equipment Generator
        public string GenerateEquipment(string type)
        {
            type = type?.ToLower() ?? "general";

            var equipment = new Dictionary<string, List<string>>
            {
                ["weapon"] = new List<string>
                {
                    "Ares Predator - 6EV", "Cobra Pistol - 6", "Desperado SMG - 6EV",
                    "H&K 226 - 6", "Katana - +2DV", "Monowhip - +1DV"
                },
                ["armor"] = new List<string>
                {
                    "Arasaka Combat Armor - 8EV", "BioTech Heavy Armor - 7EV",
                    "Ghost in Shell Armor - 6EV", "Ballistic Vest - 4EV"
                },
                ["cyberware"] = new List<string>
                {
                    "Enhanced Vision - 2 vision modes", "Muscle Augmentation - +2 STR",
                    "Synapse Accelerator - +2 REA", "Cyberimplant Arm - 1 damage",
                    "Stealth Chip - +3 Stealth"
                },
                ["general"] = new List<string>
                {
                    "Medical Kit - Basic", "Hack Tools - Common Programs",
                    "Money - 500 ¥", "Passport - Fake"
                }
            };

            var typeList = equipment.ContainsKey(type) ? equipment[type] : equipment["general"];
            return typeList[Random.Shared.Next(typeList.Count)];
        }
    }
}
