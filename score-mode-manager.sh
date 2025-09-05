#!/bin/bash

# Score Mode Manager - Backup and Delete scores by mode
# Supports: Standard, Relax, Autopilot, ScoreV2
# Author: GitHub Copilot
# Date: $(date)

set -e

# Database connection settings
DB_CONTAINER="osu-sunrise-mysql-sunrise-db-1"
DB_USER="root"
DB_PASS="root"
DB_NAME="sunrise"
BACKUP_DIR="/home/ubuntu/Observatory/backups/score-modes"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Ensure backup directory exists
mkdir -p "$BACKUP_DIR"

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to execute MySQL query
execute_query() {
    local query="$1"
    docker exec "$DB_CONTAINER" mysql -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "$query" 2>/dev/null
}

# Function to get mod condition for different modes
get_mod_condition() {
    local mode="$1"
    case "$mode" in
        "relax"|"rx")
            echo "Mods & 128 > 0"  # Relax mod
            ;;
        "autopilot"|"ap")
            echo "Mods & 8192 > 0"  # Autopilot mod
            ;;
        "scorev2"|"v2")
            echo "Mods & 536870912 > 0"  # ScoreV2 mod
            ;;
        "standard"|"std")
            echo "Mods & 128 = 0 AND Mods & 8192 = 0 AND Mods & 536870912 = 0"  # No special mods
            ;;
        *)
            print_error "Unknown mode: $mode"
            exit 1
            ;;
    esac
}

# Function to get gamemode condition for stats/grades
get_gamemode_condition() {
    local mode="$1"
    case "$mode" in
        "relax"|"rx")
            echo "GameMode IN (4, 5, 6, 7)"  # RX modes: 4=osu!rx, 5=taiko!rx, 6=catch!rx, 7=mania!rx
            ;;
        "autopilot"|"ap")
            echo "GameMode IN (8, 9, 10, 11)"  # AP modes (if exists)
            ;;
        "scorev2"|"v2")
            echo "GameMode IN (12, 13, 14, 15)"  # V2 modes (if exists)
            ;;
        "standard"|"std")
            echo "GameMode IN (0, 1, 2, 3)"  # Standard modes: 0=osu!, 1=taiko, 2=catch, 3=mania
            ;;
        *)
            print_error "Unknown mode: $mode"
            exit 1
            ;;
    esac
}

# Function to backup user data
backup_user_data() {
    local user_id="$1"
    local mode="$2"
    local timestamp=$(date +"%Y%m%d_%H%M%S")
    local backup_file="$BACKUP_DIR/user_${user_id}_${mode}_backup_${timestamp}.sql"
    
    print_info "Creating backup for user $user_id ($mode mode)..."
    
    local mod_condition=$(get_mod_condition "$mode")
    local gamemode_condition=$(get_gamemode_condition "$mode")
    
    # Create backup SQL file
    cat > "$backup_file" << EOF
-- Backup for User ID: $user_id, Mode: $mode
-- Created: $(date)
-- Mod Condition: $mod_condition
-- GameMode Condition: $gamemode_condition

-- Backup Scores
CREATE TABLE IF NOT EXISTS backup_scores_${user_id}_${mode}_${timestamp} AS 
SELECT * FROM score WHERE UserId = $user_id AND ($mod_condition);

-- Backup User Stats  
CREATE TABLE IF NOT EXISTS backup_user_stats_${user_id}_${mode}_${timestamp} AS
SELECT * FROM user_stats WHERE UserId = $user_id AND ($gamemode_condition);

-- Backup User Grades
CREATE TABLE IF NOT EXISTS backup_user_grades_${user_id}_${mode}_${timestamp} AS
SELECT * FROM user_grades WHERE UserId = $user_id AND ($gamemode_condition);
EOF
    
    # Execute backup
    execute_query "$(cat "$backup_file")"
    
    # Add data counts to backup file
    echo "" >> "$backup_file"
    echo "-- Data Counts:" >> "$backup_file"
    execute_query "SELECT 'Scores:', COUNT(*) FROM score WHERE UserId = $user_id AND ($mod_condition);" >> "$backup_file"
    execute_query "SELECT 'Stats:', COUNT(*) FROM user_stats WHERE UserId = $user_id AND ($gamemode_condition);" >> "$backup_file"
    execute_query "SELECT 'Grades:', COUNT(*) FROM user_grades WHERE UserId = $user_id AND ($gamemode_condition);" >> "$backup_file"
    
    print_success "Backup created: $backup_file"
}

# Function to show user data counts
show_user_counts() {
    local user_id="$1"
    local mode="$2"
    
    local mod_condition=$(get_mod_condition "$mode")
    local gamemode_condition=$(get_gamemode_condition "$mode")
    
    print_info "Data counts for User $user_id ($mode mode):"
    
    execute_query "
    SELECT 'Scores ($mode):' as Type, COUNT(*) as Count FROM score WHERE UserId = $user_id AND ($mod_condition)
    UNION ALL
    SELECT 'Stats ($mode):', COUNT(*) FROM user_stats WHERE UserId = $user_id AND ($gamemode_condition)  
    UNION ALL
    SELECT 'Grades ($mode):', COUNT(*) FROM user_grades WHERE UserId = $user_id AND ($gamemode_condition);
    "
}

# Function to delete user data
delete_user_data() {
    local user_id="$1"
    local mode="$2"
    local skip_backup="$3"
    
    # Get username for confirmation
    local username=$(execute_query "SELECT username FROM user WHERE id = $user_id;" | tail -n 1)
    
    if [ -z "$username" ]; then
        print_error "User ID $user_id not found!"
        exit 1
    fi
    
    print_warning "About to delete $mode data for user: $username (ID: $user_id)"
    
    # Show current counts
    show_user_counts "$user_id" "$mode"
    
    # Create backup unless skipped
    if [ "$skip_backup" != "true" ]; then
        backup_user_data "$user_id" "$mode"
    fi
    
    # Confirm deletion
    echo -n "Are you sure you want to delete this data? (yes/no): "
    read confirmation
    
    if [ "$confirmation" != "yes" ]; then
        print_info "Deletion cancelled."
        exit 0
    fi
    
    local mod_condition=$(get_mod_condition "$mode")
    local gamemode_condition=$(get_gamemode_condition "$mode")
    
    print_info "Deleting $mode data for user $user_id..."
    
    # Delete scores
    execute_query "DELETE FROM score WHERE UserId = $user_id AND ($mod_condition);"
    
    # Delete stats
    execute_query "DELETE FROM user_stats WHERE UserId = $user_id AND ($gamemode_condition);"
    
    # Delete grades  
    execute_query "DELETE FROM user_grades WHERE UserId = $user_id AND ($gamemode_condition);"
    
    print_success "Deletion completed!"
    
    # Show final counts
    print_info "Final counts after deletion:"
    show_user_counts "$user_id" "$mode"
}

# Function to list all modes for a user
list_user_modes() {
    local user_id="$1"
    
    local username=$(execute_query "SELECT username FROM user WHERE id = $user_id;" | tail -n 1)
    
    if [ -z "$username" ]; then
        print_error "User ID $user_id not found!"
        exit 1
    fi
    
    print_info "Mode summary for user: $username (ID: $user_id)"
    
    execute_query "
    SELECT 'Standard Scores:' as Type, COUNT(*) as Count FROM score WHERE UserId = $user_id AND (Mods & 128 = 0 AND Mods & 8192 = 0 AND Mods & 536870912 = 0)
    UNION ALL
    SELECT 'Relax Scores:', COUNT(*) FROM score WHERE UserId = $user_id AND (Mods & 128 > 0)
    UNION ALL  
    SELECT 'Autopilot Scores:', COUNT(*) FROM score WHERE UserId = $user_id AND (Mods & 8192 > 0)
    UNION ALL
    SELECT 'ScoreV2 Scores:', COUNT(*) FROM score WHERE UserId = $user_id AND (Mods & 536870912 > 0)
    UNION ALL
    SELECT 'Standard Stats:', COUNT(*) FROM user_stats WHERE UserId = $user_id AND GameMode IN (0, 1, 2, 3)
    UNION ALL
    SELECT 'Relax Stats:', COUNT(*) FROM user_stats WHERE UserId = $user_id AND GameMode IN (4, 5, 6, 7);
    "
}

# Function to show usage
show_usage() {
    echo "Usage: $0 <command> [options]"
    echo ""
    echo "Commands:"
    echo "  list <user_id>                    - List all modes for a user"
    echo "  count <user_id> <mode>            - Show data counts for specific mode"
    echo "  backup <user_id> <mode>           - Backup user data for specific mode"
    echo "  delete <user_id> <mode>           - Delete user data for specific mode (with backup)"
    echo "  delete <user_id> <mode> --no-backup - Delete without backup"
    echo ""
    echo "Modes:"
    echo "  standard, std    - Standard/Normal scores"
    echo "  relax, rx        - Relax mod scores"
    echo "  autopilot, ap    - Autopilot mod scores"
    echo "  scorev2, v2      - ScoreV2 mod scores"
    echo ""
    echo "Examples:"
    echo "  $0 list 1059"
    echo "  $0 count 1059 relax"
    echo "  $0 backup 1059 rx"
    echo "  $0 delete 1059 relax"
    echo "  $0 delete 1059 autopilot --no-backup"
}

# Main script logic
case "$1" in
    "list")
        if [ -z "$2" ]; then
            print_error "User ID required!"
            show_usage
            exit 1
        fi
        list_user_modes "$2"
        ;;
    "count")
        if [ -z "$2" ] || [ -z "$3" ]; then
            print_error "User ID and mode required!"
            show_usage
            exit 1
        fi
        show_user_counts "$2" "$3"
        ;;
    "backup")
        if [ -z "$2" ] || [ -z "$3" ]; then
            print_error "User ID and mode required!"
            show_usage
            exit 1
        fi
        backup_user_data "$2" "$3"
        ;;
    "delete")
        if [ -z "$2" ] || [ -z "$3" ]; then
            print_error "User ID and mode required!"
            show_usage
            exit 1
        fi
        skip_backup="false"
        if [ "$4" = "--no-backup" ]; then
            skip_backup="true"
        fi
        delete_user_data "$2" "$3" "$skip_backup"
        ;;
    *)
        show_usage
        exit 1
        ;;
esac
