#!/bin/bash

# Variables
LOCAL_BUILD_DIR="./dist/volta-xdashboard-spa"
DEP_FILES_DIR="./deployment-files"
REMOTE_HOST="u216915831@191.96.63.101" ##  host
# REMOTE_DIR="/home/u216915831/domains/voltaxcharging.com/public_html/dashboard" ## this is for voltaxcharging.
REMOTE_DIR="/home/u216915831/domains/eveon.ma/public_html/dashboard" ## this is for eveon
SSH_PORT="65002"  # Replace with your Hostinger SSH port, if not default (22)

# Ensure the script stops if an error occurs
set -e

# Step 1: Build the Angular Project
echo "Building Angular project..."
#ng build --configuration production

# Step 2: Remove Existing Files from Hostinger's public_html
echo "Removing old files from Hostinger..."
ssh -p $SSH_PORT $REMOTE_HOST "find $REMOTE_DIR -maxdepth 1 -type f -exec rm -f {} \;"
echo "here"
ssh -p 65002 u216915831@191.96.63.101 "find /home/u216915831/domains/voltaxcharging.com/public_html/dashboard -maxdepth 1 -type f -exec rm -f {} \;"
echo "here 2"
# # Step 3: Upload New Build to Hostinger
echo "Uploading new build to Hostinger..."
rsync -avz -e "ssh -p $SSH_PORT" $LOCAL_BUILD_DIR/ $REMOTE_HOST:$REMOTE_DIR
rsync -avz -e "ssh -p $SSH_PORT" $DEP_FILES_DIR/ $REMOTE_HOST:$REMOTE_DIR

echo "Deployment completed successfully!"