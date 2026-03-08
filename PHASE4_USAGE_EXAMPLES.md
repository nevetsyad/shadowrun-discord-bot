# Phase 4: Session Management - Usage Examples

## 🎮 Practical Examples for Every Command

### Example 1: Basic Session Workflow

```bash
# Start a new session
/session start name:"Shadows of Seattle - Episode 1"

# Add organization tags
/session tags action:add tag:"Seattle Arc" category:"Campaign"
/session tags action:add tag:"Main Story" category:"Theme"
/session tags action:add tag:"Session 1" category:"Number"

# Set category
/session category name:"Main Campaign"

# Players join
/session join character:"Street Sam"
/session join character:"Decker"

# During play - add notes
/session notes text:"Players met Johnson at The Big Rhino diner" type:"Story"
/session notes text:"Negotiated 15,000¥ for extraction job" type:"Player" pinned:true
/session notes text:"Target is Dr. Elena Chen, researcher at Renraku" type:"Story"

# Take a break
/session break reason:"Pizza break - back in 30 minutes"

# Resume after break
/session resume

# More notes during play
/session notes text:"Infiltration successful, no alarms triggered" type:"Story"
/session notes text:"Decker found paydata worth extra 5,000¥" type:"Player Choice" pinned:true

# Complete the session
/session complete outcome:"Mission successful! Extracted Dr. Chen, earned 20,000¥ total, 5 karma each"
```

### Example 2: Session History and Search

```bash
# View all recent sessions
/session history

# Search for Seattle sessions
/session search query:"Seattle"

# Filter by campaign tag
/session history tag:"Seattle Arc"

# View specific session details
/session summary id:15

# Get time statistics
/session stats id:15

# View notes from a session
/session notes id:15
```

### Example 3: Time Tracking

```bash
# During an active session - view current time stats
/session time

# View guild-wide statistics
/session time guild:true

# Example output:
# 📊 Guild Time Statistics
# Total Sessions: 25
# Total Duration: 150.5 hours
# Total Active Time: 142.3 hours
# Total Breaks: 45
# Average Session: 360 minutes
# Longest Session: 480 minutes
```

### Example 4: Campaign Management

```bash
# Start campaign session
/session start name:"Shadows of Seattle - Episode 1"
/session category name:"Seattle Campaign"
/session tags action:add tag:"Seattle Campaign" category:"Campaign"

# Later sessions in same campaign
/session start name:"Shadows of Seattle - Episode 2"
/session category name:"Seattle Campaign"
/session tags action:add tag:"Seattle Campaign" category:"Campaign"

# View all campaign sessions
/session history tag:"Seattle Campaign"

# Each session will show:
# 📦 Shadows of Seattle - Episode 1
# 📦 Shadows of Seattle - Episode 2
# etc.
```

### Example 5: Break Management

```bash
# Manual break
/session break reason:"Dinner break"

# Bot responds:
# ☕ Session Break Started
# Reason: Dinner break
# Use /session resume to end the break

# After dinner, resume
/session resume

# Bot responds:
# ▶️ Game Session Resumed
# [Session Name] continues!

# View break statistics
/session stats

# Shows:
# Total Breaks: 3
# Break Time: 45 minutes
# Active Time: 3.5 hours
```

### Example 6: Note Management

```bash
# Add different types of notes

# General story note
/session notes text:"The team discovered a hidden lab in the basement" type:"Story"

# Important player choice
/session notes text:"Players chose to negotiate instead of fight" type:"Player Choice" pinned:true

# Reminder for next session
/session notes text:"Remember: Johnson will contact them tomorrow" type:"Reminder" pinned:true

# Player-specific note
/session notes text:"Street Sam took 3 boxes of damage" type:"Player"

# View all notes
/session notes

# Output shows:
# 📝 Session Notes
# 📌 **Player Choice**: Players chose to negotiate instead of fight
#    *2026-03-08 18:30*
# 
# 📌 **Reminder**: Remember: Johnson will contact them tomorrow
#    *2026-03-08 18:35*
# 
# **Story**: The team discovered a hidden lab in the basement
#    *2026-03-08 18:25*
```

### Example 7: Session Completion and Archiving

```bash
# End current session and auto-archive
/session complete outcome:"Successfully extracted Dr. Chen. Team earned 20,000¥ and 5 karma each. New contact: Dr. Chen (Loyalty 1)."

# Bot responds:
# ✅ Session Completed & Archived
# **[Session Name]** has been completed and archived.
# Duration: 240 minutes
# Participants: 4 players
# Total Breaks: 2

# Later, search for this session
/session search query:"Dr. Chen"

# View archived session
/session summary id:20
```

### Example 8: Tag Management

```bash
# Add tags
/session tags action:add tag:"Combat Heavy" category:"Theme"
/session tags action:add tag:"Aztlan" category:"Location"
/session tags action:add tag:"Extraction" category:"Mission Type"

# List all tags
/session tags action:list

# Output:
# 🏷️ Session Tags (ID: 15)
# • **Combat Heavy** (Theme)
# • **Aztlan** (Location)
# • **Extraction** (Mission Type)

# Remove a tag
/session tags action:remove tag:"Combat Heavy"

# Search by tag
/session history tag:"Extraction"
```

### Example 9: Session Statistics Deep Dive

```bash
# Get comprehensive session summary
/session summary

# Output:
# 📋 Session Summary: Shadows of Seattle - Episode 5
# Status: Active
# Duration: 4.25 hours (3.75h active)
# Location: Renraku Arcology
# Participants: 5 players
# Progress: 📖 12 events | 🎯 8 choices
# Missions: ⚔️ 1 active | ✅ 2 completed
# Breaks: 2 breaks (30 min)
# Tags: Seattle Campaign, Extraction, Main Story
```

### Example 10: One-Shot Session

```bash
# Quick one-shot game
/session start name:"Halloween One-Shot: Ghosts of the Ork Underground"

# Tag as one-shot
/session tags action:add tag:"One-Shot" category:"Format"
/session tags action:add tag:"Halloween Special" category:"Event"
/session category name:"One-Shot"

# Play through...
/session notes text:"Characters: Spooky, Wraith, and Banshee" type:"Player"
/session break reason:"Quick bio break"
/session resume
/session notes text:"Encountered the ghost of a murdered child" type:"Story" pinned:true

# Complete in one session
/session complete outcome:"Laid the ghost to rest. Characters earned 3 karma and 2,000¥ each. Spooky got a spirit formula."
```

### Example 11: Multi-Session Arc

```bash
# Session 1 of arc
/session start name:"Bug City - Part 1: The Discovery"
/session category name:"Bug City Arc"
/session tags action:add tag:"Bug City Arc" category:"Campaign"
/session tags action:add tag:"Insect Spirits" category:"Theme"

# Play through...
/session complete outcome:"Discovered insect spirit hive. Team barely escaped."

# Session 2 of arc
/session start name:"Bug City - Part 2: The Nest"
/session category name:"Bug City Arc"
/session tags action:add tag:"Bug City Arc" category:"Campaign"

# Continue story...
/session notes text:"Continuing from last session - team planning assault on hive"
/session complete outcome:"Destroyed the hive queen. Major victory!"

# View entire arc
/session history tag:"Bug City Arc"

# See all sessions in the arc with their outcomes
```

### Example 12: Session with Lots of Breaks

```bash
# Long session with multiple breaks
/session start name:"Convention Game - 8 Hour Marathon"

# Hour 2
/session break reason:"Lunch break"
# ... 1 hour later
/session resume

# Hour 4
/session break reason:"Stretch break"
# ... 15 min later
/session resume

# Hour 6
/session break reason:"Dinner break"
# ... 1 hour later
/session resume

# End of session
/session complete outcome:"Epic 8-hour game completed!"

# Check statistics
/session stats

# Shows:
# Total Breaks: 3
# Total Break Time: 135 minutes
# Active Time: 5.75 hours
# Total Duration: 8 hours
```

## 💡 Pro Tips

### 1. Consistent Tagging
```bash
# Use consistent tag names across sessions
/session tags action:add tag:"Seattle Campaign" category:"Campaign"
# Not: "Seattle" one time, "Seattle Arc" another, "Seattle Campaign" a third
```

### 2. Pin Important Notes
```bash
# Pin critical information so it's easy to find
/session notes text:"Important: Johnson's real name is Mr. Tanaka" type:"Story" pinned:true
```

### 3. Regular Summaries
```bash
# Check summary regularly during long sessions
/session summary
# Helps keep track of progress and time
```

### 4. Meaningful Break Reasons
```bash
# Use descriptive break reasons for better records
/session break reason:"Pizza arrived - 30 min break"
# Not just: /session break
```

### 5. Complete Session Outcomes
```bash
# Write comprehensive outcomes for future reference
/session complete outcome:"Mission complete. Earned 15,000¥ and 5 karma. New contacts: Dr. Chen (L3), Officer Martinez (L1). Loose end: Ares still hunting the team."

# Not just: /session complete outcome:"Done"
```

## 🎯 Common Workflows

### Quick Session
```bash
/session start name:"Quick Run"
/session complete outcome:"Success"
```

### Detailed Campaign Session
```bash
/session start name:"Campaign - Ep 10"
/session category name:"Main Campaign"
/session tags action:add tag:"Campaign" category:"Format"
/session notes text:"Recap: Last time..." type:"Story"
# ... play ...
/session break reason:"Dinner"
/session resume
# ... more play ...
/session notes text:"Cliffhanger for next time" type:"Story" pinned:true
/session complete outcome:"Detailed outcome here"
```

### Session Review
```bash
/session history
/session summary id:15
/session stats id:15
/session notes id:15
```

---

These examples cover all Phase 4 features. Mix and match commands based on your needs!
