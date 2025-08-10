# Observatory Data Backup

## Overview
This folder contains backup data from the Observatory project, specifically focused on beatmap status information.

## Contents

### custom-beatmap-status/
- **README.md**: Documentation of custom beatmap backup process
- **custom_beatmap_ids_restore.sql**: SQL script to restore custom beatmap IDs and their status

### osu-files/
- Sample `.osu` files from Observatory for reference
- These files contain beatmap metadata and status information

## Backup Purpose
The main goal is to preserve beatmap status information (ranked, loved, etc.) from Observatory to ensure data continuity and recovery capabilities.

## Date
Backup created: $(date)

## Notes
- This backup is maintained in a separate git branch to avoid cluttering the main repository
- The main Sunrise repository ignores backup folders by default
- This branch specifically allows tracking of backup data
