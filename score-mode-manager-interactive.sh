#!/bin/bash

# Score Mode Manager - Interactive Menu Version
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
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
WHITE='\033[1;37m'
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

print_header() {
    echo -e "${CYAN}================================${NC}"
    echo -e "${WHITE}$1${NC}"
    echo -e "${CYAN}================================${NC}"
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
        "1"|"relax"|"rx")
            echo "Mods & 128 > 0"  # Relax mod
            ;;
        "2"|"autopilot"|"ap")
            echo "Mods & 8192 > 0"  # Autopilot mod
            ;;
        "3"|"scorev2"|"v2")
            echo "Mods & 536870912 > 0"  # ScoreV2 mod
            ;;
        "4"|"standard"|"std")
            echo "Mods & 128 = 0 AND Mods & 8192 = 0 AND Mods & 536870912 = 0"  # No special mods
            ;;
        *)
            print_error "Unknown mode: $mode"
            return 1
            ;;
    esac
}

# Function to get gamemode condition for stats/grades
get_gamemode_condition() {
    local mode="$1"
    case "$mode" in
        "1"|"relax"|"rx")
            echo "GameMode IN (4, 5, 6, 7)"  # RX modes
            ;;
        "2"|"autopilot"|"ap")
            echo "GameMode IN (8, 9, 10, 11)"  # AP modes (if exists)
            ;;
        "3"|"scorev2"|"v2")
            echo "GameMode IN (12, 13, 14, 15)"  # V2 modes (if exists)
            ;;
        "4"|"standard"|"std")
            echo "GameMode IN (0, 1, 2, 3)"  # Standard modes
            ;;
        *)
            print_error "Unknown mode: $mode"
            return 1
            ;;
    esac
}

# Function to get mode name
get_mode_name() {
    local mode="$1"
    case "$mode" in
        "1"|"relax"|"rx") echo "Relax" ;;
        "2"|"autopilot"|"ap") echo "Autopilot" ;;
        "3"|"scorev2"|"v2") echo "ScoreV2" ;;
        "4"|"standard"|"std") echo "Standard" ;;
        *) echo "Unknown" ;;
    esac
}

# Function to validate user exists
validate_user() {
    local user_id="$1"
    local username=$(execute_query "SELECT username FROM user WHERE id = $user_id;" | tail -n 1)
    
    if [ -z "$username" ]; then
        print_error "User ID $user_id not found!"
        return 1
    fi
    
    echo "$username"
}

# Function to show user data counts
show_user_counts() {
    local user_id="$1"
    local mode="$2"
    
    local mod_condition=$(get_mod_condition "$mode")
    local gamemode_condition=$(get_gamemode_condition "$mode")
    local mode_name=$(get_mode_name "$mode")
    
    local username=$(validate_user "$user_id")
    if [ $? -ne 0 ]; then return 1; fi
    
    print_header "$mode_name Data for $username (ID: $user_id)"
    echo ""
    
    # Get individual counts
    local scores=$(execute_query "SELECT COUNT(*) FROM score WHERE UserId = $user_id AND ($mod_condition);" | tail -n 1)
    local stats=$(execute_query "SELECT COUNT(*) FROM user_stats WHERE UserId = $user_id AND ($gamemode_condition);" | tail -n 1)
    local grades=$(execute_query "SELECT COUNT(*) FROM user_grades WHERE UserId = $user_id AND ($gamemode_condition);" | tail -n 1)
    
    # Pretty formatted display
    printf "${CYAN}%-15s${NC} ${GREEN}%-10s${NC}\n" "DATA TYPE" "COUNT"
    printf "${CYAN}%-15s${NC} ${GREEN}%-10s${NC}\n" "---------------" "----------"
    printf "${YELLOW}%-15s${NC} ${WHITE}%-10s${NC}\n" "Scores:" "$scores"
    printf "${YELLOW}%-15s${NC} ${WHITE}%-10s${NC}\n" "Stats:" "$stats"  
    printf "${YELLOW}%-15s${NC} ${WHITE}%-10s${NC}\n" "Grades:" "$grades"
    
    echo ""
    
    # Show inconsistency warning if needed
    if [ "$scores" = "0" ] && [ "$stats" != "0" ]; then
        print_warning "⚠️  Inconsistency: $stats stats records but 0 scores (orphaned data)"
    fi
    if [ "$scores" = "0" ] && [ "$grades" != "0" ]; then
        print_warning "⚠️  Inconsistency: $grades grades records but 0 scores (orphaned data)"
    fi
}

# Function to list all modes for a user
list_user_modes() {
    local user_id="$1"
    
    local username=$(validate_user "$user_id")
    if [ $? -ne 0 ]; then return 1; fi
    
    print_header "Mode Summary for $username (ID: $user_id)"
    echo ""
    
    # Get counts
    local std_scores=$(execute_query "SELECT COUNT(*) FROM score WHERE UserId = $user_id AND (Mods & 128 = 0 AND Mods & 8192 = 0 AND Mods & 536870912 = 0);" | tail -n 1)
    local rx_scores=$(execute_query "SELECT COUNT(*) FROM score WHERE UserId = $user_id AND (Mods & 128 > 0);" | tail -n 1)
    local ap_scores=$(execute_query "SELECT COUNT(*) FROM score WHERE UserId = $user_id AND (Mods & 8192 > 0);" | tail -n 1)
    local v2_scores=$(execute_query "SELECT COUNT(*) FROM score WHERE UserId = $user_id AND (Mods & 536870912 > 0);" | tail -n 1)
    local std_stats=$(execute_query "SELECT COUNT(*) FROM user_stats WHERE UserId = $user_id AND GameMode IN (0, 1, 2, 3);" | tail -n 1)
    local rx_stats=$(execute_query "SELECT COUNT(*) FROM user_stats WHERE UserId = $user_id AND GameMode IN (4, 5, 6, 7);" | tail -n 1)
    local ap_stats=$(execute_query "SELECT COUNT(*) FROM user_stats WHERE UserId = $user_id AND GameMode IN (8, 9, 10, 11);" | tail -n 1)
    local v2_stats=$(execute_query "SELECT COUNT(*) FROM user_stats WHERE UserId = $user_id AND GameMode IN (12, 13, 14, 15);" | tail -n 1)
    
    # Pretty formatted table
    printf "${CYAN}%-20s${NC} ${GREEN}%-10s${NC} ${CYAN}%-20s${NC} ${GREEN}%-10s${NC}\n" "📊 SCORES" "COUNT" "📈 STATS" "COUNT"
    printf "${CYAN}%-20s${NC} ${GREEN}%-10s${NC} ${CYAN}%-20s${NC} ${GREEN}%-10s${NC}\n" "--------------------" "----------" "--------------------" "----------"
    printf "${YELLOW}%-20s${NC} ${WHITE}%-10s${NC} ${YELLOW}%-20s${NC} ${WHITE}%-10s${NC}\n" "Standard:" "$std_scores" "Standard:" "$std_stats"
    printf "${MAGENTA}%-20s${NC} ${WHITE}%-10s${NC} ${MAGENTA}%-20s${NC} ${WHITE}%-10s${NC}\n" "Relax:" "$rx_scores" "Relax:" "$rx_stats"
    printf "${BLUE}%-20s${NC} ${WHITE}%-10s${NC} ${BLUE}%-20s${NC} ${WHITE}%-10s${NC}\n" "Autopilot:" "$ap_scores" "Autopilot:" "$ap_stats"
    printf "${GREEN}%-20s${NC} ${WHITE}%-10s${NC} ${GREEN}%-20s${NC} ${WHITE}%-10s${NC}\n" "ScoreV2:" "$v2_scores" "ScoreV2:" "$v2_stats"
    
    echo ""
    
    # Check for inconsistencies
    if [ "$rx_scores" = "0" ] && [ "$rx_stats" != "0" ]; then
        print_warning "⚠️  Inconsistency detected: $rx_stats Relax stats but 0 Relax scores (orphaned data)"
    fi
    if [ "$ap_scores" = "0" ] && [ "$ap_stats" != "0" ]; then
        print_warning "⚠️  Inconsistency detected: $ap_stats Autopilot stats but 0 Autopilot scores (orphaned data)"
    fi
    if [ "$v2_scores" = "0" ] && [ "$v2_stats" != "0" ]; then
        print_warning "⚠️  Inconsistency detected: $v2_stats ScoreV2 stats but 0 ScoreV2 scores (orphaned data)"
    fi
}

# Function to backup user data
backup_user_data() {
    local user_id="$1"
    local mode="$2"
    local timestamp=$(date +"%Y%m%d_%H%M%S")
    local mode_name=$(get_mode_name "$mode")
    local backup_file="$BACKUP_DIR/user_${user_id}_${mode_name}_backup_${timestamp}.sql"
    
    print_info "Creating backup for user $user_id ($mode_name mode)..."
    
    local mod_condition=$(get_mod_condition "$mode")
    local gamemode_condition=$(get_gamemode_condition "$mode")
    
    # Create backup SQL file
    cat > "$backup_file" << EOF
-- Backup for User ID: $user_id, Mode: $mode_name
-- Created: $(date)

-- Backup Scores
CREATE TABLE IF NOT EXISTS backup_scores_${user_id}_${mode_name}_${timestamp} AS 
SELECT * FROM score WHERE UserId = $user_id AND ($mod_condition);

-- Backup User Stats  
CREATE TABLE IF NOT EXISTS backup_user_stats_${user_id}_${mode_name}_${timestamp} AS
SELECT * FROM user_stats WHERE UserId = $user_id AND ($gamemode_condition);

-- Backup User Grades
CREATE TABLE IF NOT EXISTS backup_user_grades_${user_id}_${mode_name}_${timestamp} AS
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

# Function to delete user data
delete_user_data() {
    local user_id="$1"
    local mode="$2"
    local skip_backup="$3"
    
    local username=$(validate_user "$user_id")
    if [ $? -ne 0 ]; then return 1; fi
    
    local mode_name=$(get_mode_name "$mode")
    
    print_header "DELETE $mode_name DATA"
    print_warning "User: $username (ID: $user_id)"
    
    # Show current counts
    show_user_counts "$user_id" "$mode"
    
    # Create backup unless skipped
    if [ "$skip_backup" != "true" ]; then
        echo ""
        backup_user_data "$user_id" "$mode"
    fi
    
    echo ""
    print_warning "⚠️  This will PERMANENTLY delete all $mode_name data for this user!"
    echo -n "Type 'DELETE' to confirm: "
    read -r confirmation
    
    if [ "$confirmation" != "DELETE" ]; then
        print_info "Deletion cancelled."
        return 0
    fi
    
    local mod_condition=$(get_mod_condition "$mode")
    local gamemode_condition=$(get_gamemode_condition "$mode")
    
    print_info "Deleting $mode_name data for user $user_id..."
    
    # Delete scores
    execute_query "DELETE FROM score WHERE UserId = $user_id AND ($mod_condition);"
    
    # Delete stats
    execute_query "DELETE FROM user_stats WHERE UserId = $user_id AND ($gamemode_condition);"
    
    # Delete grades  
    execute_query "DELETE FROM user_grades WHERE UserId = $user_id AND ($gamemode_condition);"
    
    # ⭐ FIX: Clear Redis leaderboard to prevent ranking gaps
    print_info "Clearing Redis leaderboard cache to prevent ranking inconsistencies..."
    docker exec osu-sunrise-redis-1 redis-cli DEL leaderboard:global:0 >/dev/null 2>&1 || true
    docker exec osu-sunrise-redis-1 redis-cli DEL "leaderboard:country:0:*" >/dev/null 2>&1 || true
    
    print_success "Deletion completed!"
    
    # Show final counts
    echo ""
    print_info "Final counts after deletion:"
    show_user_counts "$user_id" "$mode"
}

# Function to show mode selection menu
show_mode_menu() {
    echo ""
    echo -e "${CYAN}Select Mode:${NC}"
    echo -e "${YELLOW}1)${NC} Relax (RX)"
    echo -e "${YELLOW}2)${NC} Autopilot (AP)" 
    echo -e "${YELLOW}3)${NC} ScoreV2 (V2)"
    echo -e "${YELLOW}4)${NC} Standard (STD)"
    echo -e "${YELLOW}0)${NC} Back to main menu"
    echo ""
}

# Function to get user input
get_user_input() {
    local prompt="$1"
    local input
    echo -n "$prompt"
    read -r input
    echo "$input"
}

# Function to pause and wait for user
pause() {
    echo ""
    echo -n "Press Enter to continue..."
    read -r
}

# Main menu function
show_main_menu() {
    clear
    print_header "🎵 OSU! SCORE MODE MANAGER 🎵"
    echo ""
    echo -e "${GREEN}What would you like to do?${NC}"
    echo ""
    echo -e "${CYAN}1)${NC} 📊 List all modes for a user"
    echo -e "${CYAN}2)${NC} 🔍 Check data counts for specific mode"
    echo -e "${CYAN}3)${NC} 💾 Backup user data for specific mode"
    echo -e "${CYAN}4)${NC} 🗑️  Delete user data for specific mode"
    echo -e "${CYAN}5)${NC} 🗑️  Delete user data (without backup)"
    echo -e "${CYAN}6)${NC} ❓ Help"
    echo -e "${CYAN}0)${NC} 🚪 Exit"
    echo ""
}

# Help function
show_help() {
    clear
    print_header "📖 HELP & INFORMATION"
    echo ""
    echo -e "${GREEN}Mode Types:${NC}"
    echo -e "${YELLOW}• Standard (STD):${NC} Normal osu! scores without special mods"
    echo -e "${YELLOW}• Relax (RX):${NC} Scores played with Relax mod (auto-clicking)"
    echo -e "${YELLOW}• Autopilot (AP):${NC} Scores played with Autopilot mod (auto-aim)"
    echo -e "${YELLOW}• ScoreV2 (V2):${NC} Scores played with ScoreV2 mod"
    echo ""
    echo -e "${GREEN}Data Types:${NC}"
    echo -e "${YELLOW}• Scores:${NC} Individual play records"
    echo -e "${YELLOW}• Stats:${NC} User statistics (pp, accuracy, rank, etc.)"
    echo -e "${YELLOW}• Grades:${NC} Grade counts (SS, S, A, B, C, D)"
    echo ""
    echo -e "${GREEN}Backup Location:${NC}"
    echo -e "${YELLOW}• Path:${NC} $BACKUP_DIR"
    echo -e "${YELLOW}• Format:${NC} SQL files with timestamp"
    echo ""
    echo -e "${RED}⚠️  WARNING:${NC}"
    echo -e "Deletion operations are ${RED}PERMANENT${NC}!"
    echo -e "Always create backups before deleting data!"
    echo ""
    pause
}

# Main interactive loop
main_loop() {
    while true; do
        show_main_menu
        
        echo -n "Enter your choice (0-6): "
        read -r choice
        
        case "$choice" in
            "1")
                clear
                print_header "📊 LIST USER MODES"
                echo -n "Enter User ID: "
                read -r user_id
                echo ""
                list_user_modes "$user_id"
                pause
                ;;
            "2")
                clear
                print_header "🔍 CHECK DATA COUNTS"
                echo -n "Enter User ID: "
                read -r user_id
                show_mode_menu
                echo -n "Enter mode (1-4, 0=back): "
                read -r mode
                if [ "$mode" != "0" ]; then
                    echo ""
                    show_user_counts "$user_id" "$mode"
                    pause
                fi
                ;;
            "3")
                clear
                print_header "💾 BACKUP USER DATA"
                echo -n "Enter User ID: "
                read -r user_id
                show_mode_menu
                echo -n "Enter mode (1-4, 0=back): "
                read -r mode
                if [ "$mode" != "0" ]; then
                    echo ""
                    backup_user_data "$user_id" "$mode"
                    pause
                fi
                ;;
            "4")
                clear
                print_header "🗑️ DELETE USER DATA (WITH BACKUP)"
                echo -n "Enter User ID: "
                read -r user_id
                show_mode_menu
                echo -n "Enter mode (1-4, 0=back): "
                read -r mode
                if [ "$mode" != "0" ]; then
                    echo ""
                    delete_user_data "$user_id" "$mode" "false"
                    pause
                fi
                ;;
            "5")
                clear
                print_header "🗑️ DELETE USER DATA (NO BACKUP)"
                print_warning "⚠️  This will delete data WITHOUT creating a backup!"
                echo -n "Enter User ID: "
                read -r user_id
                show_mode_menu
                echo -n "Enter mode (1-4, 0=back): "
                read -r mode
                if [ "$mode" != "0" ]; then
                    echo ""
                    delete_user_data "$user_id" "$mode" "true"
                    pause
                fi
                ;;
            "6")
                show_help
                ;;
            "0")
                clear
                print_success "Thanks for using Score Mode Manager! 👋"
                exit 0
                ;;
            *)
                print_error "Invalid choice! Please select 0-6."
                sleep 2
                ;;
        esac
    done
}

# Check if running in interactive mode or with arguments
if [ $# -eq 0 ]; then
    # No arguments - run interactive menu
    main_loop
else
    # Arguments provided - run in command line mode (for backward compatibility)
    case "$1" in
        "list")
            if [ -z "$2" ]; then
                print_error "User ID required!"
                exit 1
            fi
            list_user_modes "$2"
            ;;
        "count")
            if [ -z "$2" ] || [ -z "$3" ]; then
                print_error "User ID and mode required!"
                exit 1
            fi
            show_user_counts "$2" "$3"
            ;;
        "backup")
            if [ -z "$2" ] || [ -z "$3" ]; then
                print_error "User ID and mode required!"
                exit 1
            fi
            backup_user_data "$2" "$3"
            ;;
        "delete")
            if [ -z "$2" ] || [ -z "$3" ]; then
                print_error "User ID and mode required!"
                exit 1
            fi
            skip_backup="false"
            if [ "$4" = "--no-backup" ]; then
                skip_backup="true"
            fi
            delete_user_data "$2" "$3" "$skip_backup"
            ;;
        *)
            print_error "Unknown command: $1"
            echo ""
            echo "Available commands: list, count, backup, delete"
            echo "Or run without arguments for interactive menu."
            exit 1
            ;;
    esac
fi
