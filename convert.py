"""
This script converts data in Unity Perception Package format into YOLO format.
"""

import json
import os
import sys
import shutil
import random
import re
import cv2


pool_test_dir = "C:\\Users\\sterl\\reu\\real-pool-data\\test"
lake_test_dir = "C:\\Users\\sterl\\reu\\real-lake-data\\test"
base_dir = "C:\\Users\\sterl\\AppData\\LocalLow\\DefaultCompany\\ROBOSUB"


def convert_percept_to_yolo(test_path, solo_number, train_split=0.8):
    # Create and define directories
    input_folder = os.path.join(
        base_dir, "solo" if solo_number == "0" else f"solo_{solo_number}"
    )
    if not os.path.exists(input_folder):
        print(f"Error: Input folder '{input_folder}' does not exist.")
        sys.exit(1)
    images_folder = os.path.join(input_folder, "sequence.0")
    output_folder = f"{input_folder}_converted"

    train_img_folder = os.path.join(output_folder, "train", "images")
    train_lbl_folder = os.path.join(output_folder, "train", "labels")
    val_img_folder = os.path.join(output_folder, "valid", "images")
    val_lbl_folder = os.path.join(output_folder, "valid", "labels")

    for folder in [train_img_folder, train_lbl_folder, val_img_folder, val_lbl_folder]:
        os.makedirs(folder, exist_ok=True)

    # List JSON files
    def alphanum_key(s):
        return [
            int(text) if text.isdigit() else text.lower()
            for text in re.split("([0-9]+)", s)
        ]

    json_files = sorted(
        [f for f in os.listdir(images_folder) if f.endswith(".json")],
        key=alphanum_key,
    )
    json_files = json_files[1:]
    train_cutoff = int(len(json_files) * train_split)

    for idx, filename in enumerate(json_files):
        input_path = os.path.join(images_folder, filename)
        with open(input_path, "r") as file:
            data = json.load(file)

        # Extract image dimensions
        image_width, image_height = 1, 1
        for capture in data.get("captures", []):
            if "dimension" in capture:
                image_width, image_height = capture["dimension"]

        ### Extract bounding boxes ###
        # Note: Coordinates are normalized (0-1), so they remain valid after resizing
        bounding_boxes = []
        for capture in data.get("captures", []):
            for annotation in capture.get("annotations", []):
                if (
                    annotation["@type"]
                    == "type.unity.com/unity.solo.BoundingBox2DAnnotation"
                ):
                    for value in annotation.get("values", []):
                        try:
                            label_id = value["labelId"] - 1
                            origin_x, origin_y = value["origin"]
                            width, height = value["dimension"]
                            center_x = (origin_x + width / 2) / image_width
                            center_y = (origin_y + height / 2) / image_height
                            bounding_boxes.append(
                                f"{label_id} {center_x} {center_y} {width / image_width} {height / image_height}"
                            )
                        except (KeyError, TypeError):
                            print(f"Skipping malformed box in {filename}")
        if not bounding_boxes:
            print(f"Skipping {filename}: no valid bounding boxes.")
            continue

        is_train = idx < train_cutoff
        img_dest = train_img_folder if is_train else val_img_folder
        lbl_dest = train_lbl_folder if is_train else val_lbl_folder

        ### Get image filename ###
        image_filename = None
        for capture in data.get("captures", []):
            if "filename" in capture:
                image_filename = capture["filename"]
                break

        if not image_filename:
            print(f"Skipping {filename}: no image reference found.")
            continue

        ### Copy and resize image to 640x640 ###
        src_image_path = os.path.join(images_folder, image_filename)
        dst_image_path = os.path.join(img_dest, os.path.basename(image_filename))
        if os.path.exists(src_image_path):
            img = cv2.imread(src_image_path)
            if img is not None:
                resized_img = cv2.resize(img, (640, 640))
                cv2.imwrite(dst_image_path, resized_img)
            else:
                print(f"Warning: Could not read image {image_filename}")
                continue
        else:
            print(f"Warning: Image {image_filename} not found.")
            continue

        label_filename = os.path.basename(filename).replace(
            "frame_data.json", "camera.txt"
        )
        label_path = os.path.join(lbl_dest, label_filename)
        with open(label_path, "w") as file:
            file.write("\n".join(bounding_boxes))

    ### Copy test folder ###
    print(f"Copying test data from {test_path} to {output_folder}")
    dst_test_folder = os.path.join(output_folder, "test")
    if os.path.exists(dst_test_folder):
        shutil.rmtree(dst_test_folder)
    shutil.copytree(test_path, dst_test_folder)

    ### Write YOLO data.yaml ###
    def_path = os.path.join(input_folder, "annotation_definitions.json")
    if not os.path.exists(def_path):
        print(f"Error: Annotation definitions file '{def_path}' does not exist.")
        sys.exit(1)
    with open(def_path, "r") as file:
        defs = json.load(file)
    if defs and "annotationDefinitions" in defs:
        for ann in defs["annotationDefinitions"]:
            if ann["id"] == "bounding box":
                spec = ann["spec"]
                break
        class_names = [item["label_name"] for item in spec]
        nc = len(class_names)
    else:
        class_names = []
        nc = 0

    yaml_path = os.path.join(output_folder, "data.yaml")
    with open(yaml_path, "w") as yaml_file:
        yaml_file.write(
            f"train: ../train/images\n"
            f"val: ../valid/images\n"
            f"test: ../test/images\n\n"
            f"nc: {nc}\n"
            f"names: {json.dumps(class_names)}\n"
        )
    print(f"Created data.yaml at {yaml_path}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(
            "Usage: python convert_perception_to_yolo.py <dataset_name> <solo_number_or_range>"
        )
        sys.exit(1)

    dataset_name = sys.argv[1]
    if dataset_name not in ["pool", "lake"]:
        print("Error: dataset_name must be 'pool' or 'lake'.")
        sys.exit(1)

    test_path = pool_test_dir if dataset_name == "pool" else lake_test_dir
    solo_arg = sys.argv[2]

    # Handle range argument (e.g., 1-5)
    if "-" in solo_arg:
        try:
            start, end = map(int, solo_arg.split("-"))
            for solo_number in range(start, end + 1):
                print(f"\n--- Processing solo_{solo_number} ---")
                convert_percept_to_yolo(test_path, str(solo_number))
        except ValueError:
            print("Error: Invalid range format. Use X-Y where X and Y are integers.")
            sys.exit(1)
    else:
        # Single solo number
        convert_percept_to_yolo(test_path, solo_arg)
