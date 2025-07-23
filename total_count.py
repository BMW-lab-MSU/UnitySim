"""
This script counts the number of objects detected in Unity simulation frames
collected with the perception package. Provide as argument the number of the solo (i.e. solo_3 = 3)
"""

import os
import json
import sys


def initialize_counts(directory):
    for filename in os.listdir(directory):
        if filename.startswith("step") and filename.endswith(".frame_data.json"):
            filepath = os.path.join(directory, filename)
            with open(filepath, "r") as f:
                data = json.load(f)

            for metric in data.get("metrics", []):
                if metric.get("@type") == "type.unity.com/unity.solo.ObjectCountMetric":
                    return {obj["labelName"]: 0 for obj in metric["values"]}
    raise ValueError("No valid ObjectCountMetric found to initialize counts.")


def count_instances_by_label(directory):
    total_counts = initialize_counts(directory)
    empty_frame_count = 0
    total_files = 0

    for filename in os.listdir(directory):
        if filename.startswith("step") and filename.endswith(".frame_data.json"):
            total_files += 1
            filepath = os.path.join(directory, filename)

            try:
                with open(filepath, "r") as f:
                    data = json.load(f)

                labels_in_frame = set()

                for capture in data.get("captures", []):
                    for annotation in capture.get("annotations", []):
                        if annotation.get("values"):
                            for instance in annotation["values"]:
                                label_name = instance.get("labelName")
                                if label_name in total_counts:
                                    labels_in_frame.add(label_name)

                for label in labels_in_frame:
                    total_counts[label] += 1

                if not labels_in_frame:
                    empty_frame_count += 1

            except Exception as e:
                print(f"Failed to process {filename}: {e}")

    return total_counts, empty_frame_count, total_files


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python count_instances.py <solo_number>")
        sys.exit(1)

    solo_number = sys.argv[1]
    base_path = r"C:\Users\sterl\AppData\LocalLow\DefaultCompany\ROBOSUB"
    if solo_number == "0":
        full_path = os.path.join(base_path, "solo", "sequence.0")
    else:
        full_path = os.path.join(base_path, f"solo_{solo_number}", "sequence.0")

    if not os.path.isdir(full_path):
        print(f"Directory does not exist: {full_path}")
        sys.exit(1)

    totals, empty_count, total_files = count_instances_by_label(full_path)

    print(f"\nInstance presence counts for solo_{solo_number}:\n")
    for label, count in sorted(totals.items()):
        print(f"{label}: {count}")

    print(f"\nTotal files processed: {total_files}")
    print(f"Files with no instances detected: {empty_count}")
