#! /bin/bash

# create db backup in tmp folder
pg_dump nellebot > /tmp/nellebot.bak

# create folder if it doesn't exist
mkdir -p "$HOME/nellebot-db-backups"

# move backup file to backups folder
mv /tmp/nellebot.bak "$HOME/nellebot-db-backups/nellebot-$(date +%Y-%m-%d-%H:%M).bak"