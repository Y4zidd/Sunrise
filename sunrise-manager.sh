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
    echo "MAIN MENU:"
    echo "--------------------------------------------------"
    echo "1.  Backup MySQL Database (Manual)"
    echo "2.  Docker Compose Down + Backup"
    echo "3.  Restore MySQL Database"
    echo "4.  List Available Backups"
    echo "5.  Container Status"
    echo "6.  Database Statistics"
    echo "7.  Start All Services"
    echo "8.  View Logs"
    echo "9.  Delete User Scores"
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

delete_user_scores() {
    echo "Delete User Scores"
    echo "--------------------------------------------------"
    
    if ! docker ps | grep -q "osu-sunrise-mysql-sunrise-db-1"; then
        echo "MySQL container is not running!"
        echo "Starting MySQL container..."
        cd /home/ubuntu/Sunrise
        docker compose up mysql-sunrise-db -d
        sleep 5
    fi
    
    echo "⚠️  WARNING: This will permanently delete all scores, stats, and grades for a user!"
    echo "⚠️  The user account will remain but will lose all gameplay data!"
    echo ""
    
    read -p "Enter user ID to delete scores for: " user_id
    
    if [[ ! "$user_id" =~ ^[0-9]+$ ]]; then
        echo "Invalid user ID! Please enter a number."
        read -p "Press Enter to return to menu..."
        return
    fi
    
    echo ""
    echo "Checking user data for ID: $user_id"
    
    # Check if user exists
    user_exists=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT Username FROM user WHERE Id = $user_id;" -s -N 2>/dev/null)
    
    if [ -z "$user_exists" ] || [ "$user_exists" = "NULL" ]; then
        echo "❌ User with ID $user_id not found!"
        read -p "Press Enter to return to menu..."
        return
    fi
    
    echo "✅ User found: $user_exists"
    
    # Count existing data
    score_count=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM score WHERE UserId = $user_id;" -s -N 2>/dev/null)
    stats_count=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user_stats WHERE UserId = $user_id;" -s -N 2>/dev/null)
    grades_count=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user_grades WHERE UserId = $user_id;" -s -N 2>/dev/null)
    
    echo ""
    echo "Data to be deleted:"
    echo "- Scores: $score_count"
    echo "- User Stats: $stats_count"
    echo "- User Grades: $grades_count"
    echo ""
    
    read -p "Are you sure you want to delete all data for user '$user_exists' (ID: $user_id)? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        echo "Operation cancelled."
        read -p "Press Enter to return to menu..."
        return
    fi
    
    echo ""
    echo "🗑️  Deleting user data..."
    
    # Delete scores
    if [ "$score_count" -gt 0 ]; then
        docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; DELETE FROM score WHERE UserId = $user_id;" 2>/dev/null
        echo "✅ Deleted $score_count scores"
    fi
    
    # Delete user stats
    if [ "$stats_count" -gt 0 ]; then
        docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; DELETE FROM user_stats WHERE UserId = $user_id;" 2>/dev/null
        echo "✅ Deleted $stats_count user stats"
    fi
    
    # Delete user grades
    if [ "$grades_count" -gt 0 ]; then
        docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; DELETE FROM user_grades WHERE UserId = $user_id;" 2>/dev/null
        echo "✅ Deleted $grades_count user grades"
    fi
    
    echo ""
    echo "🎯 User '$user_exists' (ID: $user_id) data has been cleared!"
    echo "📝 The user account remains and can login again."
    echo "🎮 All scores, PP, stats, and grades have been reset."
    echo ""
    
    read -p "Press Enter to return to menu..."
}

main() {
    while true; do
        clear
        show_header
        show_menu
        
        read -p "Select option (0-9): " choice
        
        case $choice in
            1)
                backup_mysql
                ;;
            2)
                docker_down_with_backup
                ;;
            3)
                restore_mysql
                ;;
            4)
                show_backups
                ;;
            5)
                show_status
                ;;
            6)
                show_database_stats
                ;;
            7)
                start_services
                ;;
            8)
                show_logs
                ;;
            9)
                delete_user_scores
                ;;
            0)
                echo "Thank you for using Sunrise Manager!"
                exit 0
                ;;
            *)
                echo "Invalid option! Please select 0-9"
                sleep 2
                ;;
        esac
    done
}

main 