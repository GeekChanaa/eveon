#!/bin/bash

# Color codes for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'
BLUE='\033[0;34m'
# Project and Docker configuration
PROJECT_NAME="voltax-api"
DOCKER_REGISTRY="jaberfeka"
AWS_MACHINE_IP="51.20.70.166"
AWS_USER="ec2-user"

# Function to handle errors
error_exit() {
    echo -e "${RED}ERROR: $1${NC}" >&2
    exit 1
}

# Function to modify Program.cs for Kestrel configuration
modify_kestrel_config() {
    local file="Program.cs"
    
    # Check if the modification already exists
    if grep -q "options.ListenAnyIP(5000)" "$file"; then
        echo -e "${YELLOW}Kestrel configuration already exists.${NC}"
        return
    fi

    # Backup the original file
    cp "$file" "$file.bak"

    # Use sed to insert the Kestrel configuration
    sed -i '/var builder = WebApplication.CreateBuilder(args);/a\
builder.WebHost.ConfigureKestrel(options =>\
{\
    options.ListenAnyIP(5000);\
});' "$file" || error_exit "Failed to modify Kestrel configuration"

    echo -e "${GREEN}Kestrel configuration added successfully.${NC}"
}

# Function to update database context configuration
update_db_context() {
    local file="Program.cs"
    
    # Ensure the file exists
    if [[ ! -f "$file" ]]; then
        echo -e "${RED}Error: $file not found!${NC}"
        return 1
    fi

    # Backup the original file
    cp "$file" "$file.bak" || { echo -e "${RED}Failed to create backup.${NC}"; return 1; }

    # Define the replacement text
    replacement='builder.Services.AddDbContext<VoltaXApiDbContext>((serviceProvider, options) => {\
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");\
    var serverVersion = new MariaDbServerVersion("10.6.15");\
    options.UseMySql(connectionString, serverVersion);\
});'

    # Use sed to replace the old context definition
    sed -i.bak "s|// builder\.Services\.AddDbContext<VoltaXApiDbContext>(options =>\
//         options\.UseSqlServer(builder\.Configuration\.GetConnectionString(\"DefaultConnection\")));|$replacement|" "$file"

    # Check if replacement was successful
    if grep -q "UseMySql" "$file"; then
        echo -e "${GREEN}Database context configuration updated successfully.${NC}"
    else
        echo -e "${RED}Error: Failed to update database context configuration.${NC}"
        mv "$file.bak" "$file"  # Restore the backup if the replacement failed
        return 1
    fi
}


# Function to handle database migrations
handle_database_migrations() {
    read -p "Do you want to reset database migrations? (y/n): " reset_migrations

    if [[ $reset_migrations == "y" || $reset_migrations == "Y" ]]; then
        # Remove migrations folder
        rm -rf Migrations || error_exit "Failed to remove Migrations folder"
        
        # Drop database
        dotnet ef database drop -f || error_exit "Failed to drop database"
        
        # Create new migrations
        dotnet ef migrations add InitialCreate || error_exit "Failed to create migrations"
        
        # Update database
        dotnet ef database update || error_exit "Failed to update database"

        echo -e "${GREEN}Database migrations reset successfully.${NC}"
    else
        echo -e "${YELLOW}Skipping database migrations reset.${NC}"
    fi
}

# Function to build Docker image
build_docker_image() {
    # Build Docker image
    docker build -t "$PROJECT_NAME:latest" . || error_exit "Docker image build failed"
    
    # Tag Docker image
    docker tag "$PROJECT_NAME:latest" "$DOCKER_REGISTRY/$PROJECT_NAME:latest" || error_exit "Docker image tagging failed"
    
    # Push Docker image
    docker push "$DOCKER_REGISTRY/$PROJECT_NAME:latest" || error_exit "Docker image push failed"
    
    echo -e "${GREEN}Docker image built, tagged, and pushed successfully.${NC}"
}

# Function to deploy to AWS machine
deploy_to_aws() {
    # SSH into AWS machine and perform deployment
    ssh -i "/mnt/c/Users/bcg-d/OneDrive/Bureau/voltax.pem" "$AWS_USER@$AWS_MACHINE_IP" << EOSSH
        # Stop running container
        docker stop "$PROJECT_NAME" || true
        
        # Remove existing container
        docker rm "$PROJECT_NAME" || true
        
        # Pull latest image
        docker pull "$DOCKER_REGISTRY/$PROJECT_NAME:latest"
        #docker pull "jaberfeka/voltax-api:latest"
        
        # Run new container
        docker run -d --name "$PROJECT_NAME" -p 80:5000 "$DOCKER_REGISTRY/$PROJECT_NAME:latest"
        # docker run -d --name "voltax-api" -p 80:5000 "jaberfeka/voltax-api:latest"
EOSSH

    echo -e "${GREEN}Deployment to AWS machine completed successfully.${NC}"
}

# Main script execution
main() {
    
    # Modify Kestrel configuration
    echo -e "${BLUE}modifying the kestrel config${NC}"
    modify_kestrel_config

    # # Update database context configuration
    echo -e "${BLUE}modifying the db context${NC}"
    update_db_context

    # # Handle database migrations
    echo -e "${BLUE}updating remote database${NC}"
    handle_database_migrations

    # # Build and push Docker image
    build_docker_image

    # Deploy to AWS
    deploy_to_aws

    echo -e "${GREEN}CI/CD process completed successfully!${NC}"
}

# Run the main script
main