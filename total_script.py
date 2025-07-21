"""
This script counts the number of objects detected in Unity simulation frames
collected with the perception package. Provide as argument the number of the solo (i.e. solo_3 = 3
"""

import os
import json
from collections import defaultdict
import sys


def count_objects_in_directory(directory):
    total_counts = defaultdict(int)
    empty_frame_count = 0
    total_files = 0

    for filename in os.listdir(directory):
        if filename.startswith("step") and filename.endswith(".frame_data.json"):
            total_files += 1
            filepath = os.path.join(directory, filename)
            try:
                with open(filepath, "r") as f:
                    data = json.load(f)

                object_counts = []
                for metric in data.get("metrics", []):
                    if (
                        metric.get("@type")
                        == "type.unity.com/unity.solo.ObjectCountMetric"
                    ):
                        object_counts = [
                            obj.get("count", 0) for obj in metric.get("values", [])
                        ]
                        for obj in metric.get("values", []):
                            label_name = obj.get("labelName")
                            count = obj.get("count", 0)
                            total_counts[label_name] += count
                        break  # only one ObjectCountMetric expected

                if all(count == 0 for count in object_counts):
                    empty_frame_count += 1

            except Exception as e:
                print(f"Failed to process {filename}: {e}")

    return total_counts, empty_frame_count, total_files


# Example usage
if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python count_objects.py <solo_number>")
        sys.exit(1)

    solo_number = sys.argv[1]
    base_path = r"C:\Users\sterl\AppData\LocalLow\DefaultCompany\ROBOSUB"
    full_path = os.path.join(base_path, f"solo_{solo_number}", "sequence.0")

    if not os.path.isdir(full_path):
        print(f"Directory does not exist: {full_path}")
        sys.exit(1)

    totals, empty_count, total_files = count_objects_in_directory(full_path)

    print(f"\nObject counts for solo_{solo_number}:\n")
    for label, count in sorted(totals.items()):
        print(f"{label}: {count}")

    print(f"\nTotal files processed: {total_files}")
    print(f"Files with no objects detected: {empty_count}")
