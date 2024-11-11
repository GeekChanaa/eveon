import os
import requests
import json
import re
# Function to load JSON data from a file
def load_json_from_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        return json.load(file)

def sanitize_filename(filename):
    return re.sub(r'[\\/*?:"<>|]', '_', filename)

# Set up directories and download images
def download_images(json_data):
  data = json_data['data']
  for brand in data:
    brand_name = sanitize_filename(brand['name'])
    brand_folder = os.path.join(os.getcwd(), brand_name)

    # Create the brand folder if it doesn't exist
    if not os.path.exists(brand_folder):
        os.makedirs(brand_folder)

    # Loop through each model and download the image
    for model in brand['models']:
        model_name = sanitize_filename(model['name'].replace(" ", "_"))  # Replace spaces with underscores for filenames
        image_url = model['imageUrl']

        if image_url:
            image_path = os.path.join(brand_folder, f"{model_name}.jpg")
            
            # Download the image
            response = requests.get(image_url)
            if response.status_code == 200:
                with open(image_path, 'wb') as img_file:
                    img_file.write(response.content)
                print(f"Downloaded: {image_path}")
            else:
                print(f"Failed to download image for {model_name}. Status code: {response.status_code}")

# Load the JSON data from the specified file path
file_path = './charger-brands.json' 
json_data = load_json_from_file(file_path)
print(json_data)
# Run the download function
download_images(json_data)
