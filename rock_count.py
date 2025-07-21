import os
import json
from collections import defaultdict
import sys
import re


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
                rock_count = None

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

                            if label_name == "rock":
                                rock_count = count
                        break  # only one ObjectCountMetric expected

                if all(count == 0 for count in object_counts):
                    empty_frame_count += 1

                # Check step number and rock count
                match = re.search(r"step(\d+)", filename)
                if match:
                    step_num = int(match.group(1))
                    if step_num % 4 == 0 and rock_count == 0:
                        print(
                            f"Step {step_num} (filename: {filename}) has 0 'rock' count."
                        )

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
