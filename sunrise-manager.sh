#!/bin/bash

# Sunrise Manager - Interactive Menu for Backup & Restore
# All-in-one script for MySQL database management

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKUP_SCRIPT="$SCRIPT_DIR/Data/scripts/auto_backup_mysql.sh"
DOWN_SCRIPT="$SCRIPT_DIR/Data/scripts/docker-compose-down-with-backup.sh"
RESTORE_SCRIPT="$SCRIPT_DIR/Data/scripts/restore_mysql_backup.sh"
BACKUP_DIR="/home/ubuntu/Sunrise/Data/Backups/mysql"

show_header() {
    echo "=================================================="
    echo "              SUNRISE MANAGER"
    echo "=================================================="
    echo ""
}

show_menu() {
    echo "MAIN MENU (Topic-based):"
    echo "--------------------------------------------------"
    echo "1.  Backup & Restore"
    echo "2.  Services"
    echo "3.  Database"
    echo "4.  Logs"
    echo "5.  User Score Tools"
    echo "0.  Exit"
    echo "--------------------------------------------------"
    echo ""
}

backup_mysql() {
    echo "Creating MySQL backup..."
    if [ -f "$BACKUP_SCRIPT" ]; then
        bash "$BACKUP_SCRIPT"
        if [ $? -eq 0 ]; then
            echo "Backup completed successfully!"
        else
            echo "Backup failed!"
        fi
    else
        echo "Backup script not found!"
    fi
    echo ""
    read -p "Press Enter to return to menu..."
}

docker_down_with_backup() {
    echo "Running docker compose down with backup..."
    if [ -f "$DOWN_SCRIPT" ]; then
        bash "$DOWN_SCRIPT"
    else
        echo "Docker down script not found!"
        echo "Running docker compose down without backup..."
        docker compose down
    fi
    echo ""
    read -p "Press Enter to return to menu..."
}

show_backups() {
    echo "Available MySQL Backups:"
    echo "--------------------------------------------------"
    
    if [ -d "$BACKUP_DIR" ] && [ "$(ls -A $BACKUP_DIR 2>/dev/null)" ]; then
        ls -la "$BACKUP_DIR"/*.sql 2>/dev/null | awk '{print $9}' | sed 's|.*/||' | nl
    else
        echo "No backups available"
    fi
    
    echo "--------------------------------------------------"
    echo ""
    read -p "Press Enter to return to menu..."
}

restore_mysql() {
    echo "Restore MySQL Database"
    echo "--------------------------------------------------"
    
    if [ -d "$BACKUP_DIR" ] && [ "$(ls -A $BACKUP_DIR 2>/dev/null)" ]; then
        echo "Available backups:"
        ls -la "$BACKUP_DIR"/*.sql 2>/dev/null | awk '{print $9}' | sed 's|.*/||' | nl
        echo ""
        
        read -p "Enter backup number to restore (or 0 to cancel): " choice
        
        if [ "$choice" = "0" ]; then
            echo "Restore cancelled"
            return
        fi
        
        backup_file=$(ls -1 "$BACKUP_DIR"/*.sql 2>/dev/null | sed -n "${choice}p" | sed 's|.*/||')
        
        if [ -n "$backup_file" ]; then
            echo "Starting restore from: $backup_file"
            if [ -f "$RESTORE_SCRIPT" ]; then
                bash "$RESTORE_SCRIPT" "$backup_file"
            else
                echo "Restore script not found!"
            fi
        else
            echo "Invalid backup number!"
        fi
    else
        echo "No backups available"
    fi
    
    echo ""
    read -p "Press Enter to return to menu..."
}

show_database_stats() {
    echo "Database Statistics"
    echo "--------------------------------------------------"
    
    if ! docker ps | grep -q "osu-sunrise-mysql-sunrise-db-1"; then
        echo "MySQL container is not running!"
        echo "Starting MySQL container..."
        cd /home/ubuntu/Sunrise
        docker compose up mysql-sunrise-db -d
        sleep 5
    fi
    
    echo "Fetching database statistics..."
    echo ""
    
    # Total users
    total_users=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user;" -s -N 2>/dev/null)
    if [ -n "$total_users" ] && [ "$total_users" != "NULL" ]; then
        echo "Total Users: $total_users"
    else
        echo "Total Users: Not accessible"
    fi
    
    # Active users (non-bot)
    active_users=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user WHERE Privilege != 32;" -s -N 2>/dev/null)
    if [ -n "$active_users" ] && [ "$active_users" != "NULL" ]; then
        echo "Active Users (non-bot): $active_users"
    else
        echo "Active Users: Not accessible"
    fi
    
    # Total scores
    total_scores=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM score;" -s -N 2>/dev/null)
    if [ -n "$total_scores" ] && [ "$total_scores" != "NULL" ]; then
        echo "Total Scores: $total_scores"
    else
        echo "Total Scores: Not accessible"
    fi
    
    # Total beatmaps
    total_beatmaps=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM beatmap;" -s -N 2>/dev/null)
    if [ -n "$total_beatmaps" ] && [ "$total_beatmaps" != "NULL" ]; then
        echo "Total Beatmaps: $total_beatmaps"
    else
        echo "Total Beatmaps: Not accessible"
    fi
    
    # Latest user
    latest_user=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT Username, RegisterDate FROM user ORDER BY RegisterDate DESC LIMIT 1;" -s -N 2>/dev/null)
    if [ -n "$latest_user" ] && [ "$latest_user" != "NULL" ]; then
        echo "Latest User: $latest_user"
    else
        echo "Latest User: Not accessible"
    fi
    
    # Latest score
    latest_score=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT s.Score, u.Username, s.Date FROM score s JOIN user u ON s.UserId = u.Id ORDER BY s.Date DESC LIMIT 1;" -s -N 2>/dev/null)
    if [ -n "$latest_score" ] && [ "$latest_score" != "NULL" ]; then
        echo "Latest Score: $latest_score"
    else
        echo "Latest Score: Not accessible"
    fi
    
    echo ""
    echo "Recent Users (10 latest):"
    echo "--------------------------------------------------"
    
    docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "
    USE sunrise; 
    SELECT 
        Id,
        Username,
        Email,
        DATE_FORMAT(RegisterDate, '%Y-%m-%d %H:%i') as RegisterDate,
        Privilege
    FROM user 
    ORDER BY RegisterDate DESC 
    LIMIT 10;" 2>/dev/null | column -t -s $'\t'
    
    echo ""
    echo "Top 5 Users by Score Count:"
    echo "--------------------------------------------------"
    
    docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "
    USE sunrise; 
    SELECT 
        u.Username,
        COUNT(s.Id) as ScoreCount,
        SUM(s.Score) as TotalScore
    FROM user u 
    LEFT JOIN score s ON u.Id = s.UserId 
    WHERE u.Privilege != 32
    GROUP BY u.Id, u.Username 
    ORDER BY ScoreCount DESC 
    LIMIT 5;" 2>/dev/null | column -t -s $'\t'
    
    echo ""
    read -p "Press Enter to return to menu..."
}

show_status() {
    echo "Container Status:"
    echo "--------------------------------------------------"
    
    cd /home/ubuntu/Sunrise
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -E "(sunrise|observatory)"
    
    echo ""
    echo "Resource Usage:"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}" | grep -E "(sunrise|observatory)"
    
    echo ""
    read -p "Press Enter to return to menu..."
}

# =============================
# Topic: Backup & Restore
# =============================
menu_backup_restore() {
    echo "Backup & Restore"
    echo "--------------------------------------------------"
    echo "1. Backup MySQL Database (Manual)"
    echo "2. Docker Compose Down + Backup"
    echo "3. Restore MySQL Database"
    echo "4. List Available Backups"
    echo "0. Back to main menu"
    read -p "Select option: " br_choice
    case $br_choice in
        1) backup_mysql ;;
        2) docker_down_with_backup ;;
        3) restore_mysql ;;
        4) show_backups ;;
        0) return ;;
        *) echo "Invalid option!"; sleep 1 ;;
    esac
}

# =============================
# Topic: Services
# =============================
menu_services() {
    echo "Services"
    echo "--------------------------------------------------"
    echo "1. Start All Services"
    echo "2. Container Status & Resource Usage"
    echo "0. Back to main menu"
    read -p "Select option: " svc_choice
    case $svc_choice in
        1) start_services ;;
        2) show_status ;;
        0) return ;;
        *) echo "Invalid option!"; sleep 1 ;;
    esac
}

# =============================
# Topic: Database
# =============================
menu_database() {
    echo "Database"
    echo "--------------------------------------------------"
    echo "1. Database Statistics"
    echo "0. Back to main menu"
    read -p "Select option: " db_choice
    case $db_choice in
        1) show_database_stats ;;
        0) return ;;
        *) echo "Invalid option!"; sleep 1 ;;
    esac
}

# =============================
# Topic: Logs (reuses existing sub-menu)
# =============================
menu_logs() {
    show_logs
}

start_services() {
    echo "Starting all services..."
    cd /home/ubuntu/Sunrise
    docker compose up -d
    echo "All services started!"
    echo ""
    read -p "Press Enter to return to menu..."
}

show_logs() {
    echo "Select container to view logs:"
    echo "--------------------------------------------------"
    echo "1. Sunrise Server"
    echo "2. MySQL Database"
    echo "3. Redis"
    echo "4. Grafana"
    echo "5. Prometheus"
    echo "0. Back to main menu"
    echo "--------------------------------------------------"
    
    read -p "Select option: " log_choice
    
    case $log_choice in
        1)
            echo "Sunrise Server logs (Ctrl+C to exit):"
            docker logs -f osu-sunrise-sunrise-1
            ;;
        2)
            echo "MySQL Database logs (Ctrl+C to exit):"
            docker logs -f osu-sunrise-mysql-sunrise-db-1
            ;;
        3)
            echo "Redis logs (Ctrl+C to exit):"
            docker logs -f osu-sunrise-redis-1
            ;;
        4)
            echo "Grafana logs (Ctrl+C to exit):"
            docker logs -f osu-sunrise-grafana-1
            ;;
        5)
            echo "Prometheus logs (Ctrl+C to exit):"
            docker logs -f osu-sunrise-prometheus-1
            ;;
        0)
            return
            ;;
        *)
            echo "Invalid option!"
            ;;
    esac
}

# =============================
# User Score Tools (Backup/Delete per variant)
# =============================

ensure_mysql_running() {
    if ! docker ps | grep -q "osu-sunrise-mysql-sunrise-db-1"; then
        echo "MySQL container is not running!"
        echo "Starting MySQL container..."
        cd /home/ubuntu/Sunrise || return 1
        docker compose up mysql-sunrise-db -d
        sleep 5
    fi
}

prompt_user_id() {
    read -p "Enter user ID: " user_id
    if [[ ! "$user_id" =~ ^[0-9]+$ ]]; then
        echo "Invalid user ID! Must be a number."
        return 1
    fi
    return 0
}

verify_user_exists() {
    local uid="$1"
    local uname
    uname=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT Username FROM user WHERE Id = $uid;" -s -N 2>/dev/null)
    if [ -z "$uname" ] || [ "$uname" = "NULL" ]; then
        echo "❌ User with ID $uid not found!"
        return 1
    fi
    echo "✅ User found: $uname"
    return 0
}

select_variant() {
    echo "Select variant:"
    echo "1) Standard"
    echo "2) ScoreV2"
    echo "3) Relax"
    echo "4) Autopilot"
    echo "0) Cancel"
    read -p "Choose (0-4): " v
    case "$v" in
        1) variant="standard" ;;
        2) variant="scorev2" ;;
        3) variant="relax" ;;
        4) variant="autopilot" ;;
        0) return 1 ;;
        *) echo "Invalid option"; return 1 ;;
    esac
    echo "$variant"
    return 0
}

_build_filters_for_variant() {
    # Outputs 3 values: gm_where|score_where|suffix
    local variant="$1"
    local gm_where score_where suffix
    case "$variant" in
        standard)
            gm_where="GameMode BETWEEN 0 AND 3"
            score_where="$gm_where"
            suffix="std"
            ;;
        scorev2)
            gm_where="GameMode BETWEEN 12 AND 15"
            score_where="$gm_where"
            suffix="v2"
            ;;
        relax)
            gm_where="GameMode BETWEEN 4 AND 6"
            score_where="$gm_where"
            suffix="relax"
            ;;
        autopilot)
            gm_where="GameMode = 8"
            score_where="$gm_where"
            suffix="ap"
            ;;
        *)
            echo ""; return 1 ;;
    esac
    echo "$gm_where|$score_where|$suffix"
}

backup_user_scores_by_variant() {
    ensure_mysql_running || return 1
    prompt_user_id || { read -p "Press Enter to return to menu..."; return 1; }
    local uid="$user_id"

    local variant
    variant=$(select_variant) || { read -p "Press Enter to return to menu..."; return 1; }

    local filters gm_where score_where suffix
    filters=$(_build_filters_for_variant "$variant") || { read -p "Press Enter to return to menu..."; return 1; }
    gm_where=$(echo "$filters" | cut -d '|' -f1)
    score_where=$(echo "$filters" | cut -d '|' -f2)
    suffix=$(echo "$filters" | cut -d '|' -f3)

    mkdir -p "$BACKUP_DIR"
    local ts prefix
    ts=$(date +%Y%m%d_%H%M%S)
    prefix="user${uid}_${suffix}_${ts}"

    echo "Creating backup files for user $uid ($variant)..."
    docker exec osu-sunrise-mysql-sunrise-db-1 mysqldump -u root -proot --no-create-info --skip-triggers --single-transaction --quick sunrise score \
        --where="UserId=${uid} AND (${score_where})" > "$BACKUP_DIR/${prefix}_score.sql" 2>/dev/null

    docker exec osu-sunrise-mysql-sunrise-db-1 mysqldump -u root -proot --no-create-info --skip-triggers --single-transaction --quick sunrise user_stats \
        --where="UserId=${uid} AND (${gm_where})" > "$BACKUP_DIR/${prefix}_user_stats.sql" 2>/dev/null

    docker exec osu-sunrise-mysql-sunrise-db-1 mysqldump -u root -proot --no-create-info --skip-triggers --single-transaction --quick sunrise user_grades \
        --where="UserId=${uid} AND (${gm_where})" > "$BACKUP_DIR/${prefix}_user_grades.sql" 2>/dev/null

    echo "✅ Backup saved to:"
    echo "- $BACKUP_DIR/${prefix}_score.sql"
    echo "- $BACKUP_DIR/${prefix}_user_stats.sql"
    echo "- $BACKUP_DIR/${prefix}_user_grades.sql"
    echo ""
    read -p "Press Enter to return to menu..."
}

delete_user_scores_by_variant() {
    ensure_mysql_running || return 1
    prompt_user_id || { read -p "Press Enter to return to menu..."; return 1; }
    local uid="$user_id"

    echo "Checking user data for ID: $uid"
    verify_user_exists "$uid" || { read -p "Press Enter to return to menu..."; return 1; }

    local variant
    variant=$(select_variant) || { read -p "Press Enter to return to menu..."; return 1; }

    local filters gm_where score_where suffix
    filters=$(_build_filters_for_variant "$variant") || { read -p "Press Enter to return to menu..."; return 1; }
    gm_where=$(echo "$filters" | cut -d '|' -f1)
    score_where=$(echo "$filters" | cut -d '|' -f2)
    suffix=$(echo "$filters" | cut -d '|' -f3)

    echo ""
    echo "Counting existing data to be deleted (variant: $variant)..."
    local score_cnt stats_cnt grades_cnt
    score_cnt=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM score WHERE UserId=$uid AND ($score_where);" -s -N 2>/dev/null)
    stats_cnt=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user_stats WHERE UserId=$uid AND ($gm_where);" -s -N 2>/dev/null)
    grades_cnt=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user_grades WHERE UserId=$uid AND ($gm_where);" -s -N 2>/dev/null)

    echo "Data to be deleted:"
    echo "- Scores: $score_cnt"
    echo "- User Stats: $stats_cnt"
    echo "- User Grades: $grades_cnt"

    echo ""
    echo "Creating safety backup before deletion..."
    mkdir -p "$BACKUP_DIR"
    local ts prefix
    ts=$(date +%Y%m%d_%H%M%S)
    prefix="user${uid}_${suffix}_${ts}"
    docker exec osu-sunrise-mysql-sunrise-db-1 mysqldump -u root -proot --no-create-info --skip-triggers --single-transaction --quick sunrise score \
        --where="UserId=${uid} AND (${score_where})" > "$BACKUP_DIR/${prefix}_score.sql" 2>/dev/null
    docker exec osu-sunrise-mysql-sunrise-db-1 mysqldump -u root -proot --no-create-info --skip-triggers --single-transaction --quick sunrise user_stats \
        --where="UserId=${uid} AND (${gm_where})" > "$BACKUP_DIR/${prefix}_user_stats.sql" 2>/dev/null
    docker exec osu-sunrise-mysql-sunrise-db-1 mysqldump -u root -proot --no-create-info --skip-triggers --single-transaction --quick sunrise user_grades \
        --where="UserId=${uid} AND (${gm_where})" > "$BACKUP_DIR/${prefix}_user_grades.sql" 2>/dev/null
    echo "✅ Backup saved to $BACKUP_DIR with prefix ${prefix}_*.sql"

    echo ""
    read -p "Are you sure you want to delete ONLY $variant data for user ID $uid? Type 'yes' to confirm: " confirm
    if [ "$confirm" != "yes" ]; then
        echo "Operation cancelled."
        read -p "Press Enter to return to menu..."
        return
    fi

    echo "Deleting user $variant data in a transaction..."
    docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; \
        START TRANSACTION; \
        DELETE FROM score WHERE UserId=$uid AND ($score_where); \
        DELETE FROM user_stats WHERE UserId=$uid AND ($gm_where); \
        DELETE FROM user_grades WHERE UserId=$uid AND ($gm_where); \
        COMMIT;" 2>/dev/null

    echo "Verifying..."
    local score_after stats_after grades_after
    score_after=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM score WHERE UserId=$uid AND ($score_where);" -s -N 2>/dev/null)
    stats_after=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user_stats WHERE UserId=$uid AND ($gm_where);" -s -N 2>/dev/null)
    grades_after=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user_grades WHERE UserId=$uid AND ($gm_where);" -s -N 2>/dev/null)

    echo "Post-delete counts:"
    echo "- Scores: $score_after"
    echo "- User Stats: $stats_after"
    echo "- User Grades: $grades_after"
    echo ""
    read -p "Press Enter to return to menu..."
}

user_score_tools_menu() {
    echo "User Score Tools"
    echo "--------------------------------------------------"
    ensure_mysql_running || { read -p "Press Enter to return to menu..."; return; }

    echo "1) Backup user data by variant (Standard / ScoreV2 / Relax / Autopilot)"
    echo "2) Delete user data by variant (auto-backup first)"
    echo "0) Back to main menu"
    read -p "Choose (0-2): " c
    case "$c" in
        1) backup_user_scores_by_variant ;;
        2) delete_user_scores_by_variant ;;
        0) return ;;
        *) echo "Invalid option"; sleep 1 ;;
    esac
}

main() {
    while true; do
        clear
        show_header
        show_menu
        
        read -p "Select option (0-5): " choice
        
        case $choice in
            1) menu_backup_restore ;;
            2) menu_services ;;
            3) menu_database ;;
            4) menu_logs ;;
            5) user_score_tools_menu ;;
            0)
                echo "Thank you for using Sunrise Manager!"
                exit 0
                ;;
            *)
                echo "Invalid option! Please select 0-5"
                sleep 2
                ;;
        esac 
    done
}

main 